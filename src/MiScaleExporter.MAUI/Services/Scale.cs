using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.History;
using MiScaleExporter.Core.S400;
using MiScaleExporter.Models;
using Microsoft.Maui.ApplicationModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Reflection;

namespace MiScaleExporter.Services
{
    public class Scale : IScale
    {
        private ILogService _logService;
        private IDataInterpreter _dataInterpreter;
        private readonly IMeasurementHistoryStore _historyStore;
        private readonly SemaphoreSlim _advertisementGate = new(1, 1);
        private readonly SemaphoreSlim _scanSessionGate = new(1, 1);

        private IAdapter _adapter;
        private TaskCompletionSource<BodyComposition> _completionSource;

        private BodyComposition _lastSuccessfulBodyComposition;
        private byte[] _scannedData;
        private string _scaleBlutetoothAddress;
        private DateTime? _lastSuccessfulMeasure;
        private DateTime? _weightOnlyStabilizedAtUtc;
        private bool _scaleFoundReported;
        private bool _readingReported;
        private bool _persistMeasurementHistory;
        private int _scanGeneration;
        private int _matchingAdvertisementCount;
        private int _s400ServiceDataCount;
        private int _s400DecodedPacketCount;
        private int _s400DecodeErrorCount;
        private int _minWeight = 10; // in kilograms
        private const double KgToLbsConversion = 2.20462;
        private static readonly TimeSpan ImpedanceGracePeriod = TimeSpan.FromSeconds(5);

        private User _user;

        private BodyComposition _receivedBodyComposition;

        public BodyComposition BodyComposition
        {
            get { return _receivedBodyComposition; }
            set { _receivedBodyComposition = value; }
        }

        public Scale(
            ILogService logService,
            IDataInterpreter dataInterpreter,
            IMeasurementHistoryStore historyStore)
        {
            _logService = logService;
            _historyStore = historyStore;

            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 50000;
            _adapter.ScanTimeoutElapsed += TimeOuted;
            _dataInterpreter = dataInterpreter;
        }

        private bool ImpedanceWaitElapsed
        {
            get
            {
                return _weightOnlyStabilizedAtUtc != null
                    && (DateTime.UtcNow - _weightOnlyStabilizedAtUtc.Value) >= ImpedanceGracePeriod;
            }
        }

        public async Task<BodyComposition> GetBodyCompositonAsync(
            string scaleAddress,
            User user,
            bool persistMeasurementHistory = true)
        {
            if (!await _scanSessionGate.WaitAsync(0))
            {
                _logService.LogError("A scale scan is already running.");
                return null;
            }

            try
            {
                await _advertisementGate.WaitAsync();
                try
                {
                    Interlocked.Increment(ref _scanGeneration);
                    _dataInterpreter.ResetSession();
                    this.BodyComposition = null;
                    _lastSuccessfulBodyComposition = null;
                    _receivedBodyComposition = null;
                    _weightOnlyStabilizedAtUtc = null;
                    _scaleFoundReported = false;
                    _readingReported = false;
                    _matchingAdvertisementCount = 0;
                    _s400ServiceDataCount = 0;
                    _s400DecodedPacketCount = 0;
                    _s400DecodeErrorCount = 0;

                    _user = user;
                    _persistMeasurementHistory = persistMeasurementHistory;
                    _scaleBlutetoothAddress = scaleAddress;
                    _adapter.ScanMode = user.ScaleType == ScaleType.S400
                        ? ScanMode.LowLatency
                        : ScanMode.LowPower;
                    _adapter.ScanMatchMode = user.ScaleType == ScaleType.S400
                        ? ScanMatchMode.AGRESSIVE
                        : ScanMatchMode.STICKY;
                    _completionSource = new TaskCompletionSource<BodyComposition>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    _adapter.DeviceAdvertised -= DeviceAdvertided;
                    _adapter.DeviceAdvertised += DeviceAdvertided;
                }
                finally
                {
                    _advertisementGate.Release();
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ScaleMeasurement.Instance.Weight = null;
                    ScaleMeasurement.Instance.FoundScale = null;
                    ScaleMeasurement.Instance.DebugData = null;
                    ScaleMeasurement.Instance.RawData = null;
                    ScaleMeasurement.Instance.Phase = ScanPhase.Idle;
                    ScaleMeasurement.Instance.PhaseLabel = string.Empty;
                    ScaleMeasurement.Instance.Progress = 0;
                });

                ReportPhase(ScanPhase.Searching);
                await _adapter.StartScanningForDevicesAsync();
                return await _completionSource.Task;
            }
            finally
            {
                await StopAsync();
                _scanSessionGate.Release();
            }
        }

        private async void DeviceAdvertided(object s, DeviceEventArgs a)
        {
            var generation = Volatile.Read(ref _scanGeneration);
            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj?.GetType().GetProperty("Address");
            if (propInfo == null)
            {
                _logService.LogError("Native device has no Address property; skipping advert.");
                return;
            }
            var addressValue = propInfo.GetValue(obj, null);
            if (addressValue == null)
            {
                return;
            }
            string address = (string)addressValue;

            if (!string.Equals(address, _scaleBlutetoothAddress, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Interlocked.Increment(ref _matchingAdvertisementCount);
            await _advertisementGate.WaitAsync();
            try
            {
                if (generation != Volatile.Read(ref _scanGeneration)
                    || _completionSource?.Task.IsCompleted != false)
                {
                    return;
                }

                if (!_scaleFoundReported)
                {
                    _scaleFoundReported = true;
                    ReportPhase(ScanPhase.ScaleFound);
                }

                var device = a.Device;
                var bodyCompositionCandidate = GetScanData(device);
                if (bodyCompositionCandidate is not null && bodyCompositionCandidate.Weight > _minWeight)
                {
                    this.BodyComposition = bodyCompositionCandidate;
                    if (!_readingReported)
                    {
                        _readingReported = true;
                        ReportPhase(ScanPhase.Reading);
                    }
                }

                this.ProcessReceivedData();
                this.SetPreviews(bodyCompositionCandidate);

                if (this.BodyComposition?.MeasurementQuality == S400MeasurementQuality.Incomplete
                    || (_weightOnlyStabilizedAtUtc != null
                        && this.BodyComposition != null
                        && !this.BodyComposition.HasImpedance
                        && !ImpedanceWaitElapsed))
                {
                    ReportPhase(ScanPhase.Stabilizing);
                }

                var completedResult = this.BodyComposition;
                if (CanCompleteMeasurement(completedResult))
                {
                    await CompleteMeasurementAsync(completedResult, generation);
                }
            }
            catch (Exception ex)
            {
                if (_user?.ScaleType == ScaleType.S400)
                {
                    Interlocked.Increment(ref _s400DecodeErrorCount);
                    SetPreviews(null);
                }
                _logService.LogError(ex.Message);

                if (_scannedData != null)
                {
                    _logService.LogInfo(string.Join("; ", _scannedData));
                }
            }
            finally
            {
                _advertisementGate.Release();
            }
        }

        private void ProcessReceivedData()
        {
            if (this.BodyComposition == null)
            {
                return;
            }

            if (!this.BodyComposition.IsStabilized)
            {
                this.BodyComposition = null;
                return;
            }

            if (_lastSuccessfulMeasure != null && _lastSuccessfulMeasure >= this.BodyComposition.Date)
            {
                this.BodyComposition = null;
                return;
            }

            _lastSuccessfulBodyComposition = this.BodyComposition;
            if (_user?.ScaleType == ScaleType.S400)
            {
                return;
            }

            if (!this.BodyComposition.HasImpedance && _weightOnlyStabilizedAtUtc == null)
            {
                _weightOnlyStabilizedAtUtc = DateTime.UtcNow;
            }
        }

        public static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private void SetPreviews(BodyComposition bodyCompositionCandidate)
        {
            bool showDebug = Preferences.Get(PreferencesKeys.ShowDebugInfo, false);
            string foundScaleText = null;
            string debugDataText = null;
            string rawDataText = null;

            if (showDebug)
            {
                foundScaleText = _matchingAdvertisementCount > 0
                    ? "Connected to scale: Yes"
                    : "Connected to scale: No";
                if (_user?.ScaleType == ScaleType.S400)
                {
                    var quality = (bodyCompositionCandidate ?? this.BodyComposition)
                        ?.MeasurementQuality?.ToString() ?? "Pending";
                    debugDataText = $"Adverts: {_matchingAdvertisementCount}; FE95: {_s400ServiceDataCount}; decoded: {_s400DecodedPacketCount}; errors: {_s400DecodeErrorCount}; state: {quality}";
                }
                else if (bodyCompositionCandidate != null)
                {
                    debugDataText = (bodyCompositionCandidate.IsStabilized ? "Stabilized: Yes" : "Stabilized: No") + " " + (bodyCompositionCandidate.HasImpedance ? "Impedance: Yes" : "Impedance: No");
                }

                if (bodyCompositionCandidate != null)
                {
                    if (bodyCompositionCandidate.RawDataLog != null && bodyCompositionCandidate.RawDataLog.Count > 0)
                    {
                        rawDataText = string.Join("|", bodyCompositionCandidate.RawDataLog.Select(BytesToHex));
                    }
                    else if (bodyCompositionCandidate.ReceivedRawData != null && bodyCompositionCandidate.ReceivedRawData.Length > 0)
                    {
                        rawDataText = BytesToHex(bodyCompositionCandidate.ReceivedRawData);
                    }
                }
            }

            string weightText = null;
            if (this.BodyComposition != null)
            {
                weightText = GetWeightScanningLabel(this.BodyComposition.Weight, Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (showDebug)
                {
                    ScaleMeasurement.Instance.FoundScale = foundScaleText;
                    if (debugDataText != null)
                    {
                        ScaleMeasurement.Instance.DebugData = debugDataText;
                    }
                    if (rawDataText != null)
                    {
                        ScaleMeasurement.Instance.RawData = rawDataText;
                    }
                }
                if (weightText != null)
                {
                    ScaleMeasurement.Instance.Weight = weightText;
                }
            });
        }

        private string GetWeightScanningLabel(double valueInKg, bool convertToLbs)
        {
            return $"{(convertToLbs ? valueInKg * KgToLbsConversion : valueInKg).ToString("0.##")}{(convertToLbs ? "lbs" : "kg")}";
        }

        private BodyComposition GetScanData(IDevice device)
        {
            if (device == null)
            {
                return null;
            }

            var serviceData = device.AdvertisementRecords
                .Where(record => record.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ServiceData)
                .Select(record => record.Data)
                .Where(data => data != null)
                .ToArray();
            if (_user.ScaleType != ScaleType.S400)
            {
                return AttachRawData(serviceData.FirstOrDefault());
            }

            BodyComposition bestCandidate = null;
            var s400ServiceData = serviceData.Where(IsS400ServiceData).ToArray();
            _s400ServiceDataCount += s400ServiceData.Length;
            foreach (var data in s400ServiceData)
            {
                var candidate = AttachRawData(data);
                if (candidate is null)
                {
                    continue;
                }

                bestCandidate = candidate;
                if (candidate.IsStabilized
                    && candidate.MeasurementQuality != S400MeasurementQuality.Incomplete)
                {
                    break;
                }
            }
            return bestCandidate;
        }

        private BodyComposition AttachRawData(byte[] data)
        {
            _scannedData = data;
            var composition = _dataInterpreter.ComputeData(data, _user, _scaleBlutetoothAddress);
            if (_user?.ScaleType == ScaleType.S400)
            {
                _s400DecodedPacketCount++;
            }
            if (composition is null)
            {
                return null;
            }

            composition.ReceivedRawData = data;
            if (data != null
                && !composition.RawDataLog.Any(existing => existing.AsSpan().SequenceEqual(data)))
            {
                composition.RawDataLog.Add(data.ToArray());
            }
            return composition;
        }

        private static bool IsS400ServiceData(byte[] data) =>
            data?.Length == 24
                && S400AdvertisementDecoder.IsSupportedProductId((ushort)(data[2] | data[3] << 8))
            || data?.Length == 26
                && data[0] == 0x95
                && data[1] == 0xFE
                && S400AdvertisementDecoder.IsSupportedProductId((ushort)(data[4] | data[5] << 8));

        private void CalculateBMIIfEmpty()
        {
            if (this.BodyComposition is not null && this.BodyComposition.BMI == 0 && _user.Height != 0)
            {
                var heightInMeters = (double)_user.Height / 100;
                this.BodyComposition.BMI = Math.Round(this.BodyComposition.Weight / (heightInMeters * heightInMeters), 2);
            }
        }

        private async Task<bool> PersistS400MeasurementAsync(BodyComposition composition)
        {
            if (composition?.MeasurementQuality is not { } quality
                || !S400MeasurementCompletionPolicy.CanPersistScan(
                    quality,
                    _persistMeasurementHistory)
                || string.IsNullOrWhiteSpace(composition.MeasurementId))
            {
                return true;
            }

            try
            {
                BodyCompositionEstimate estimate = null;
                if (composition.MeasurementQuality == S400MeasurementQuality.DualFrequencyComplete)
                {
                    estimate = new BodyCompositionEstimate(
                        composition.AlgorithmVersion ?? string.Empty,
                        composition.Weight,
                        composition.BMI,
                        composition.Fat,
                        composition.WaterPercentage,
                        composition.MuscleMass,
                        composition.BoneMass,
                        composition.ProteinPercentage,
                        composition.VisceralFat,
                        composition.BMR,
                        composition.MetabolicAge,
                        composition.IdealWeight,
                        composition.BodyType,
                        composition.IsCalibrated,
                        composition.CalibrationVersion ?? "identity",
                        composition.CalibrationPointCount,
                        composition.CalibrationRSquared,
                        composition.AppliedFatCorrection,
                        composition.BaselineFatPercentage > 0
                            ? composition.BaselineFatPercentage
                            : composition.Fat - composition.AppliedFatCorrection);
                }

                var measuredAt = composition.MeasuredAt
                    ?? (composition.Date.Kind == DateTimeKind.Utc
                        ? new DateTimeOffset(composition.Date)
                        : new DateTimeOffset(composition.Date.ToUniversalTime()));
                var record = new MeasurementHistoryRecord(
                    composition.MeasurementId,
                    measuredAt,
                    composition.MeasurementQuality.Value,
                    composition.ScaleProfileId ?? 0,
                    composition.Weight,
                    composition.HeartRate,
                    composition.Impedance50Khz,
                    composition.Impedance250Khz,
                    composition.RawDataLog.Select(BytesToHex).ToArray(),
                    composition.AlgorithmVersion ?? string.Empty,
                    composition.CalibrationVersion ?? "identity",
                    estimate,
                    MeasurementUploadState.Pending,
                    null,
                    null);
                await _historyStore.UpsertAsync(record);
                return true;
            }
            catch (Exception exception)
            {
                _logService.LogError($"Could not save S400 measurement history: {exception.Message}");
                return false;
            }
        }

        private async Task CompleteMeasurementAsync(BodyComposition result, int generation)
        {
            if (!CanPublishMeasurement(result, generation))
            {
                return;
            }

            result.IsValid = true;
            var persisted = await PersistS400MeasurementAsync(result);
            if (!CanPublishMeasurement(result, generation))
            {
                result.IsValid = false;
                return;
            }

            if (!persisted)
            {
                _logService.LogError("S400 measurement completed but could not be saved locally.");
            }

            _lastSuccessfulMeasure = result.Date;
            _lastSuccessfulBodyComposition = result;
            this.BodyComposition = result;
            ReportPhase(ScanPhase.Success);
            _completionSource.TrySetResult(result);
            _ = StopAsync();
        }

        public async Task CancelSearchAsync()
        {
            await _advertisementGate.WaitAsync();
            try
            {
                if (_completionSource?.Task.IsCompleted != false)
                {
                    return;
                }
                Interlocked.Increment(ref _scanGeneration);
                if (this.BodyComposition != null)
                {
                    this.BodyComposition.IsValid = false;
                }
                _dataInterpreter.ResetSession();
                if (!_completionSource.Task.IsCompleted)
                {
                    ReportPhase(ScanPhase.Cancelled);
                    _completionSource.TrySetResult(this.BodyComposition);
                }

            }
            catch (Exception ex)
            {
                _logService.LogError(ex.Message);
            }
            finally
            {
                _advertisementGate.Release();
            }

            await StopAsync();
        }

        public async Task StopSearchAsync()
        {
            await _advertisementGate.WaitAsync();
            try
            {
                if (_completionSource?.Task.IsCompleted == false)
                {
                    Interlocked.Increment(ref _scanGeneration);
                    var result = this.BodyComposition?.IsStabilized == true
                        ? this.BodyComposition
                        : _lastSuccessfulBodyComposition?.IsStabilized == true
                            ? _lastSuccessfulBodyComposition
                            : null;
                    var canFinalize = _user?.ScaleType == ScaleType.S400
                        ? CanCompleteMeasurement(result)
                        : result is not null;
                    if (canFinalize)
                    {
                        result.IsValid = true;
                        _lastSuccessfulBodyComposition = result;
                        this.BodyComposition = result;
                        ReportPhase(ScanPhase.Success);
                        _completionSource.TrySetResult(result);
                    }
                    else
                    {
                        if (this.BodyComposition != null)
                        {
                            this.BodyComposition.IsValid = false;
                        }
                        _completionSource.TrySetResult(null);
                    }
                    _dataInterpreter.ResetSession();
                }
            }
            finally
            {
                _advertisementGate.Release();
            }

            await StopAsync();

            if (this.BodyComposition is null && _lastSuccessfulBodyComposition?.IsValid == true)
            {
                this.BodyComposition = _lastSuccessfulBodyComposition;
            }
            CalculateBMIIfEmpty();
        }

        private async void TimeOuted(object s, EventArgs e)
        {
            var generation = Volatile.Read(ref _scanGeneration);
            await _advertisementGate.WaitAsync();
            try
            {
                if (generation != Volatile.Read(ref _scanGeneration)
                    || _completionSource?.Task.IsCompleted != false)
                {
                    return;
                }

                var result = this.BodyComposition?.IsStabilized == true
                    ? this.BodyComposition
                    : _lastSuccessfulBodyComposition?.IsStabilized == true
                        ? _lastSuccessfulBodyComposition
                        : null;
                if (CanCompleteMeasurement(result))
                {
                    await CompleteMeasurementAsync(result, generation);
                }
                else
                {
                    ReportPhase(ScanPhase.Failed);
                    _completionSource.TrySetResult(null);
                    _ = StopAsync();
                }
            }
            finally
            {
                _advertisementGate.Release();
            }
        }

        private async Task StopAsync()
        {
            try
            {
                await _adapter.StopScanningForDevicesAsync();
                _adapter.DeviceAdvertised -= DeviceAdvertided;
            }
            catch (Exception ex)
            {
                _logService?.LogError("StopAsync: " + ex.Message);
            }
        }

        private bool CanCompleteMeasurement(BodyComposition composition)
        {
            if (composition is null)
            {
                return false;
            }

            if (_user?.ScaleType != ScaleType.S400)
            {
                return composition.HasImpedance || ImpedanceWaitElapsed;
            }

            return composition.MeasurementQuality is { } quality
                && S400MeasurementCompletionPolicy.CanCompleteScan(quality);
        }

        private bool CanPublishMeasurement(BodyComposition composition, int generation)
        {
            var currentGeneration = Volatile.Read(ref _scanGeneration);
            var completionPending = _completionSource?.Task.IsCompleted == false;
            if (_user?.ScaleType != ScaleType.S400)
            {
                return generation == currentGeneration
                    && completionPending
                    && CanCompleteMeasurement(composition);
            }

            return composition?.MeasurementQuality is { } quality
                && S400MeasurementCompletionPolicy.CanPublishScan(
                    quality,
                    generation,
                    currentGeneration,
                    completionPending);
        }

        private void ReportPhase(ScanPhase phase)
        {
            string label = PhaseToLabel(phase);
            double progress = PhaseToProgress(phase);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScaleMeasurement.Instance.Phase = phase;
                ScaleMeasurement.Instance.PhaseLabel = label;
                ScaleMeasurement.Instance.Progress = progress;
            });
        }

        private static string PhaseToLabel(ScanPhase phase)
        {
            switch (phase)
            {
                case ScanPhase.Searching: return AppSnippets.PhaseSearching;
                case ScanPhase.ScaleFound: return AppSnippets.PhaseScaleFound;
                case ScanPhase.Reading: return AppSnippets.PhaseReading;
                case ScanPhase.Stabilizing: return AppSnippets.PhaseStabilizing;
                case ScanPhase.Success: return AppSnippets.PhaseSuccess;
                case ScanPhase.Failed: return AppSnippets.PhaseFailed;
                case ScanPhase.Cancelled: return AppSnippets.PhaseCancelled;
                default: return string.Empty;
            }
        }

        private static double PhaseToProgress(ScanPhase phase)
        {
            switch (phase)
            {
                case ScanPhase.Searching: return 0.15;
                case ScanPhase.ScaleFound: return 0.4;
                case ScanPhase.Reading: return 0.6;
                case ScanPhase.Stabilizing: return 0.8;
                case ScanPhase.Success: return 1.0;
                case ScanPhase.Failed:
                case ScanPhase.Cancelled:
                case ScanPhase.Idle:
                default: return 0.0;
            }
        }
    }
}
