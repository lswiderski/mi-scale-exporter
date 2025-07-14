using MiScaleExporter.Models;
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
        private static bool _impedanceWaitFinished = false;
        private bool _impedanceWaitStarted = false;
        private int _minWeight = 10; // in kilograms

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

        public async Task<BodyComposition> GetBodyCompositonAsync(string scaleAddress, User user)
        {
            this.BodyComposition = null;
            _lastSuccessfulBodyComposition = null;
            _receivedBodyComposition = null;
            _impedanceWaitFinished = false;
            _impedanceWaitStarted = false;

            _user = user;
            _scaleBlutetoothAddress = scaleAddress;
            _completionSource = new TaskCompletionSource<BodyComposition>();
            _adapter.DeviceAdvertised += DeviceAdvertided;

            await _adapter.StartScanningForDevicesAsync();
            return await _completionSource.Task;
        }

        private void DeviceAdvertided(object s, DeviceEventArgs a)
        {
            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj.GetType().GetProperty("Address");
            string address = (string)propInfo.GetValue(obj, null);

            if (address.ToLowerInvariant() == _scaleBlutetoothAddress?.ToLowerInvariant())
            {

                try
                {
                    var device = a.Device;
                    var bodyCompositionCandidate = GetScanData(device);
                    if(bodyCompositionCandidate is not null && bodyCompositionCandidate.Weight > _minWeight)
                    {
                        this.BodyComposition = bodyCompositionCandidate;
                    }
                   
                    this.ProcessReceivedData();
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

                    if (this.BodyComposition != null && (this.BodyComposition.HasImpedance || _impedanceWaitFinished))
                    {
                        StopAsync().Wait();
                        _lastSuccessfulMeasure = this.BodyComposition.Date;
                        this.BodyComposition.IsValid = true;
                        _completionSource.SetResult(this.BodyComposition);
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

            this.SetPreviews();

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
                if (!_impedanceWaitStarted)
                {
                    _impedanceWaitStarted = true;
                    Task.Factory.StartNew(async () =>
                    {
                        var seconds = 5;
                        await Task.Delay(TimeSpan.FromSeconds(seconds));
                        _impedanceWaitStarted = false;
                        _impedanceWaitFinished = true;

                    });
                }
            }
        }

        private void SetPreviews()
        {
            if (Preferences.Get(PreferencesKeys.ShowDebugInfo, false))
            {
                ScaleMeasurement.Instance.FoundScale = this.BodyComposition != null ? "Connected to scale: Yes" : "Connected to scale: No"; ;
                ScaleMeasurement.Instance.DebugData = (this.BodyComposition.IsStabilized ? "Stabilized: Yes" : "Stabilized: No") + " " + (this.BodyComposition.HasImpedance ? "Impedance: Yes" : "Impedance: No");
                ScaleMeasurement.Instance.RawData = string.Join("|", this.BodyComposition.ReceivedRawData);
            }

            ScaleMeasurement.Instance.Weight = this.BodyComposition.Weight.ToString("0.##") + "kg";
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
                    _completionSource.SetResult(this.BodyComposition);
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
            StopAsync().Wait();
            _completionSource.SetResult(this.BodyComposition);
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceAdvertised -= DeviceAdvertided;
        }
    }
}