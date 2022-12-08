using MiScaleExporter.Models;
using MiScaleExporter.Services;
using MiScaleExporter.Permission;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Reflection;


namespace MiScaleExporter.MAUI.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private IAdapter _adapter;
        private IDevice _scaleDevice;
        private TaskCompletionSource<BodyComposition> _completionSource;


        private readonly IScaleService _scaleService;
        private readonly ILogService _logService;

        private string _address;
        private int _age;
        private int _height;
        private Sex _sex;
        private ScaleType _scaleType;

        private BodyComposition bodyComposition;
        private BodyComposition lastSuccessfulBodyComposition;
        private byte[] _scannedData;
        private Scale _scale;
        private DateTime? lastSuccessfulMeasure;
        private static bool _impedanceWaitFinished = false;
        private bool _impedanceWaitStarted = false;

        public ScaleViewModel(IScaleService scaleService, ILogService logService)
        {
            _scaleService = scaleService;
            _logService = logService;

            Title = "Mi Scale Data";
            CancelCommand = new Command(OnCancel);
            StopCommand = new Command(OnStop);

            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 50000;
            _adapter.ScanTimeoutElapsed += TimeOuted;
        }

        public async Task CheckPreferencesAsync()
        {
            App.BodyComposition = null;
            await this.LoadPreferencesAsync();
            if (!string.IsNullOrWhiteSpace(_address))
            {
                OnScan();
            }
            else
            {
                await Shell.Current.GoToAsync($"//Settings");
            }
        }

        public async Task LoadPreferencesAsync()
        {
            this._age = Preferences.Get(PreferencesKeys.UserAge, 25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
            this._sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
            this._scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
        }

        private async Task<Models.BodyComposition> GetBodyCompositonAsync(Scale scale, Models.User user)
        {
            bodyComposition = null;
            lastSuccessfulBodyComposition = null;
            _impedanceWaitFinished = false;
            _impedanceWaitStarted = false;
            _scale = scale;
            _completionSource = new TaskCompletionSource<BodyComposition>();
            _adapter.DeviceAdvertised += DeviceAdvertided;
            await _adapter.StartScanningForDevicesAsync();
            return await _completionSource.Task;
        }

        private void TimeOuted(object s, EventArgs e)
        {
            StopAsync().Wait();
            _completionSource.SetResult(bodyComposition);
        }

        public async Task CancelSearchAsync()
        {
            try
            {
                if (bodyComposition != null)
                {
                    bodyComposition.IsValid = false;
                }
                if (!_completionSource.Task.IsCompleted)
                {
                    _completionSource.SetResult(bodyComposition);
                }

            }
            catch (Exception ex)
            {
                _logService.LogError(ex.Message);
            }

            await StopAsync();
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceAdvertised -= DeviceAdvertided;
        }

        private void DeviceAdvertided(object s, DeviceEventArgs a)
        {
            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj.GetType().GetProperty("Address");
            string address = (string)propInfo.GetValue(obj, null);

            if (address.ToLowerInvariant() == _scale.Address?.ToLowerInvariant())
            {

                try
                {
                    _scaleDevice = a.Device;
                    bodyComposition = GetScanData();
                    FoundScaleLabel = bodyComposition != null ? "Connected to scale: Yes" : "Connected to scale: No";
                    WeightLabel = bodyComposition.Weight.ToString("0.##");
                    StabilizedLabel = bodyComposition.IsStabilized ? "Stabilized: Yes" : "Stabilized: No";
                    ImpedanceLabel = bodyComposition.HasImpedance ? "Impedance: Yes" : "Impedance: No";
                    DataLabel = string.Join("|", bodyComposition.ReceivedRawData);

                    if (bodyComposition != null)
                    {
                        lastSuccessfulBodyComposition = bodyComposition;
                    }

                    if (!bodyComposition.IsStabilized)
                    {
                        bodyComposition = null;
                        return;
                    }
                    else
                    {

                        if (lastSuccessfulMeasure != null && lastSuccessfulMeasure >= bodyComposition.Date)
                        {
                            bodyComposition = null;

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
                catch (Exception ex)
                {
                    _logService.LogError(ex.Message);

                    if (_scannedData != null)
                    {
                        _logService.LogInfo(String.Join("; ", _scannedData));
                    }
                }
                finally
                {

                    if (bodyComposition != null && (bodyComposition.HasImpedance || _impedanceWaitFinished))
                    {
                        StopAsync().Wait();
                        lastSuccessfulMeasure = bodyComposition.Date;
                        bodyComposition.IsValid = true;
                        _completionSource.SetResult(bodyComposition);
                    }
                }
            }

        }

        private BodyComposition GetScanData()
        {
            if (_scaleDevice != null)
            {
                var data = _scaleDevice.AdvertisementRecords
                    .Where(x => x.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ServiceData) //0x16
                    .Select(x => x.Data)
                    .FirstOrDefault();
                _scannedData = data;


                var bc = _scaleService.ComputeData(data, new User { Sex = _sex, Age = _age, Height = _height, ScaleType = _scaleType });
                if (bc is not null)
                {
                    bc.ReceivedRawData = _scannedData;
                }

                return bc;
            }

            return null;
        }

        private async void OnStop()
        {
            StopAsync().Wait();

            if(bodyComposition is not null)
            {
                bodyComposition.IsValid = true;
            }
            else if(lastSuccessfulBodyComposition is not null)
            {
                bodyComposition = lastSuccessfulBodyComposition;
                bodyComposition.IsValid = true;
            }
            FinishMeasure();

        }

        private async void OnScan()
        {

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                double version = 0;
                double.TryParse(DeviceInfo.VersionString, out version);

                if (version >= 12)
                {
                    if (await GetBluetoothPermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert("Problem", "Permission to use Bluetooth is required to scan.",
                           "OK");
                        return;
                    }
                }
                else
                {
                    if (await GetLocationPermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert("Problem", "Permission to use Bluetooth is required to scan.",
                            "OK");
                        return;
                    }
                }

            }


            Scale scale = new Scale()
            {
                Address = _address,
            };
            ScanningLabel = string.Empty;
            WeightLabel = "0";
            this.IsBusyForm = true;
            await this.GetBodyCompositonAsync(scale,
                new User { Sex = _sex, Age = _age, Height = _height, ScaleType = _scaleType });
            OnStop();
        }

        private async void FinishMeasure()
        {
            this.IsBusyForm = false;
            if (bodyComposition is null || !bodyComposition.IsValid)
            {
                var msg = "Data could not be obtained. try again";
                await Application.Current.MainPage.DisplayAlert("Problem", msg,
                    "OK");
                _logService.LogError(msg);
                ScanningLabel = "Not found";
            }
            else
            {
                App.BodyComposition = bodyComposition;
                await Shell.Current.GoToAsync($"//FormPage?autoUpload={Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false)}");
            }
        }

        private async void OnCancel()
        {
            await this.CancelSearchAsync();
            this.IsBusyForm = false;
        }

        private async Task<PermissionStatus> GetLocationPermissionStatusAsync()
        {
            var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationPermissionStatus != PermissionStatus.Granted)
            {
                locationPermissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return locationPermissionStatus;
        }

        private async Task<PermissionStatus> GetBluetoothPermissionStatusAsync()
        {
            var bluetoothPermission = DependencyService.Get<IBluetoothConnectPermission>();
            var status = await bluetoothPermission.CheckStatusAsync();
            if (status != PermissionStatus.Granted)
            {
                status = await bluetoothPermission.RequestAsync();
            }
            return status;
        }

        public Command CancelCommand { get; }
        public Command StopCommand { get; }

        private string _weight;

        public string WeightLabel
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        private string _scanningLabel;

        public string ScanningLabel
        {
            get => _scanningLabel;
            set => SetProperty(ref _scanningLabel, value);
        }

        private bool _isBusyForm;

        public bool IsBusyForm
        {
            get => _isBusyForm;
            set => SetProperty(ref _isBusyForm, value);
        }

        private string _dataLabel;

        public string DataLabel
        {
            get => Preferences.Get(PreferencesKeys.ShowDebugInfo, false) ? _dataLabel : string.Empty;
            set => SetProperty(ref _dataLabel, value);
        }

        private string _stabilizedLabel;

        public string StabilizedLabel
        {
            get => Preferences.Get(PreferencesKeys.ShowDebugInfo, false) ? _stabilizedLabel : string.Empty;
            set => SetProperty(ref _stabilizedLabel, value);
        }

        private string _impedanceLabel;

        public string ImpedanceLabel
        {
            get => Preferences.Get(PreferencesKeys.ShowDebugInfo, false) ? _impedanceLabel : string.Empty;
            set => SetProperty(ref _impedanceLabel, value);
        }

        private string _foundScaleLabel;

        public string FoundScaleLabel
        {
            get => Preferences.Get(PreferencesKeys.ShowDebugInfo, false) ? _foundScaleLabel : string.Empty;
            set => SetProperty(ref _foundScaleLabel, value);
        }


    }
}