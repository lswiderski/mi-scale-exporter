using MiScaleExporter.MAUI.Resources.Localization;
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

        private IAdapter _adapter;
        private TaskCompletionSource<BodyComposition> _completionSource;

        private BodyComposition _lastSuccessfulBodyComposition;
        private byte[] _scannedData;
        private string _scaleBlutetoothAddress;
        private DateTime? _lastSuccessfulMeasure;
        private DateTime? _weightOnlyStabilizedAtUtc;
        private bool _scaleFoundReported;
        private bool _readingReported;
        private int _minWeight = 10; // in kilograms
        private const double KgToLbsConversion = 2.20462;

        private User _user;

        private BodyComposition _receivedBodyComposition;

        public BodyComposition BodyComposition
        {
            get { return _receivedBodyComposition; }
            set { _receivedBodyComposition = value; }
        }

        public Scale(ILogService logService, IDataInterpreter dataInterpreter)
        {
            _logService = logService;

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
                    && (DateTime.UtcNow - _weightOnlyStabilizedAtUtc.Value) >= TimeSpan.FromSeconds(5);
            }
        }

        public async Task<BodyComposition> GetBodyCompositonAsync(string scaleAddress, User user)
        {
            this.BodyComposition = null;
            _lastSuccessfulBodyComposition = null;
            _receivedBodyComposition = null;
            _weightOnlyStabilizedAtUtc = null;
            _scaleFoundReported = false;
            _readingReported = false;

            _user = user;
            _scaleBlutetoothAddress = scaleAddress;
            _completionSource = new TaskCompletionSource<BodyComposition>();
            _adapter.DeviceAdvertised += DeviceAdvertided;

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

        private void DeviceAdvertided(object s, DeviceEventArgs a)
        {
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

            if (address.ToLowerInvariant() == _scaleBlutetoothAddress?.ToLowerInvariant())
            {
                if (!_scaleFoundReported)
                {
                    _scaleFoundReported = true;
                    ReportPhase(ScanPhase.ScaleFound);
                }

                try
                {
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

                    if (_weightOnlyStabilizedAtUtc != null
                        && this.BodyComposition != null
                        && !this.BodyComposition.HasImpedance
                        && !ImpedanceWaitElapsed)
                    {
                        ReportPhase(ScanPhase.Stabilizing);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex.Message);

                    if (_scannedData != null)
                    {
                        _logService.LogInfo(string.Join("; ", _scannedData));
                    }
                }
                finally
                {

                    if (this.BodyComposition != null && (this.BodyComposition.HasImpedance || ImpedanceWaitElapsed))
                    {
                        _lastSuccessfulMeasure = this.BodyComposition.Date;
                        this.BodyComposition.IsValid = true;
                        ReportPhase(ScanPhase.Success);
                        _completionSource.TrySetResult(this.BodyComposition);
                        _ = StopAsync();
                    }
                }
            }
        }

        private void ProcessReceivedData()
        {
            if (this.BodyComposition == null)
            {
                return;
            }

            _lastSuccessfulBodyComposition = this.BodyComposition;

            if (!this.BodyComposition.IsStabilized)
            {
                this.BodyComposition = null;
                return;
            }
            else
            {

                if (_lastSuccessfulMeasure != null && _lastSuccessfulMeasure >= this.BodyComposition.Date)
                {
                    this.BodyComposition = null;

                    return;
                }
                if (!this.BodyComposition.HasImpedance && _weightOnlyStabilizedAtUtc == null)
                {
                    _weightOnlyStabilizedAtUtc = DateTime.UtcNow;
                }
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
                foundScaleText = bodyCompositionCandidate != null ? "Connected to scale: Yes" : "Connected to scale: No";
                if (bodyCompositionCandidate != null)
                {
                    debugDataText = (bodyCompositionCandidate.IsStabilized ? "Stabilized: Yes" : "Stabilized: No") + " " + (bodyCompositionCandidate.HasImpedance ? "Impedance: Yes" : "Impedance: No");
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
            if (device != null)
            {
                var data = device.AdvertisementRecords
                    .Where(x => x.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ServiceData) //0x16
                    .Select(x => x.Data)
                    .FirstOrDefault();
                _scannedData = data;

                var bc = this._dataInterpreter.ComputeData(data, _user, _scaleBlutetoothAddress);
                if (bc is not null)
                {
                    bc.ReceivedRawData = _scannedData;
                    if (!bc.RawDataLog.Contains(_scannedData))
                    {
                        bc.RawDataLog.Add(_scannedData);
                    }
                }

                return bc;
            }

            return null;
        }

        private void CalculateBMIIfEmpty()
        {
            if (this.BodyComposition is not null && this.BodyComposition.BMI == 0 && _user.Height != 0)
            {
                var heightInMeters = (double)_user.Height / 100;
                this.BodyComposition.BMI = Math.Round(this.BodyComposition.Weight / (heightInMeters * heightInMeters), 2);
            }
        }

        public async Task CancelSearchAsync()
        {
            try
            {
                if (this.BodyComposition != null)
                {
                    this.BodyComposition.IsValid = false;
                }
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

            await StopAsync();
        }

        public void StopSearch()
        {
            StopAsync().Wait();

            if (this.BodyComposition is not null)
            {
                this.BodyComposition.IsValid = true;
            }
            else if (_lastSuccessfulBodyComposition is not null)
            {
                this.BodyComposition = _lastSuccessfulBodyComposition;
                this.BodyComposition.IsValid = true;
            }
            CalculateBMIIfEmpty();
        }

        private void TimeOuted(object s, EventArgs e)
        {
            var hasResult = this.BodyComposition != null || _lastSuccessfulBodyComposition != null;
            ReportPhase(hasResult ? ScanPhase.Success : ScanPhase.Failed);
            _completionSource.TrySetResult(this.BodyComposition);
            _ = StopAsync();
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
