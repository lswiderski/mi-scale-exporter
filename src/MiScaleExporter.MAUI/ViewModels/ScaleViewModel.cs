using MiScaleExporter.Models;
using MiScaleExporter.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
 
 
 
using MiScaleExporter.Permission;
using MiScaleExporter.MAUI;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private readonly IScaleService _scaleService;
        private readonly ILogService _logService;

        private string _address;
        private int _age;
        private int _height;
        private Sex _sex;
        private ScaleType _scaleType;

        public ScaleViewModel(IScaleService scaleService, ILogService logService)
        {
            _scaleService = scaleService;
            _logService = logService;

            Title = "Mi Scale Data";
            CancelCommand = new Command(OnCancel);
            ScanCommand = new Command(OnScan, ValidateScan);
        }

        public async Task CheckPreferencesAsync()
        {
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
            this.IsBusyForm = true;
            var bc = await _scaleService.GetBodyCompositonAsync(scale,
                new User { Sex = _sex, Age = _age, Height = _height, ScaleType = _scaleType });

            this.IsBusyForm = false;
            if (bc is null || !bc.IsValid)
            {
                var msg = "Data could not be obtained. try again";
                await Application.Current.MainPage.DisplayAlert("Problem", msg,
                    "OK");
                _logService.LogError(msg);
                ScanningLabel = "Not found";
            }
            else
            {
                App.BodyComposition = bc;
                await Shell.Current.GoToAsync($"//FormPage?autoUpload={Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false)}");
            }
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

        private bool ValidateScan()
        {
            return !String.IsNullOrWhiteSpace(_address)
                                        && _height > 0 && _height < 220
                                        && _age > 0 && _age < 99;
        }

        public Command ScanCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await _scaleService.CancelSearchAsync();
            this.IsBusyForm = false;
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

    }
}