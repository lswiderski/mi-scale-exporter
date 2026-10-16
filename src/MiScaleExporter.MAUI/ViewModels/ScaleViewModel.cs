using MiScaleExporter.Models;
using MiScaleExporter.Services;
using MiScaleExporter.Permission;
using MiScaleExporter.MAUI.Resources.Localization;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private readonly IScale _scale;
        private readonly ILogService _logService;
        private readonly IHealthConnectService _healthConnectService;

        private string _address;
        private int _age;
        private int _height;
        private Models.Sex _sex;
        private ScaleType _scaleType;

        private string _bindkey;

        public ScaleViewModel(IScale scale, ILogService logService, IHealthConnectService healthConnectService)
        {
            _scale = scale;
            _logService = logService;
            _healthConnectService = healthConnectService;

            Title = AppSnippets.MiScaleData;
            CancelCommand = new Command(OnCancel);
            StopCommand = new Command(OnStop);
        }

        public async Task CheckPreferencesAsync()
        {
            ScaleMeasurement.Instance.Weight = "";
            App.BodyComposition = null;
            var hasPermissions = await CheckPermissions();

            if(hasPermissions)
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
           
        }

        public async Task LoadPreferencesAsync()
        {
            this._age = Preferences.Get(PreferencesKeys.UserAge, 25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
            this._sex = (Models.Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Models.Sex.Male);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
            this._scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
            this._bindkey = Preferences.Get(PreferencesKeys.S400Bindkey, string.Empty);
        }

        private async void OnScan()
        {
            await StartScan();
        }

        private async Task<bool> CheckPermissions()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.Version.Major >= 12)
                {
                    if (await GetBluetoothPermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        await Dialog.AlertAsync(AppSnippets.Problem, AppSnippets.PermissionBluetoothRequired,
                           AppSnippets.OK);
                        return false;
                    }

                    if (await GetLocationWhenInUsePermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        await Dialog.AlertAsync(AppSnippets.Problem, AppSnippets.PermissionLocationRequired,
                             AppSnippets.OK);
                        return false;
                    }
                }
                else
                {
                    if (await GetLocationWhenInUsePermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        if(await GetLocationAlwaysPermissionStatusAsync() != PermissionStatus.Granted)
                        {
                            await Dialog.AlertAsync(AppSnippets.Problem, AppSnippets.PermissionLocationRequired,
                            AppSnippets.OK);
                            return false;
                        }
                    }
                }

            }

            // Check if Bluetooth is enabled
            if (CrossBluetoothLE.Current.State != BluetoothState.On)
            {
                await Dialog.AlertAsync(
                    AppSnippets.Problem,
                    AppSnippets.BluetoothDisabled,
                    AppSnippets.OK);
                return false;
            }

            return true;

        }

        private async Task StartScan()
        {
            ScanningLabel = string.Empty;
            ScaleMeasurement.Instance.Weight = "0";
            this.IsBusyForm = true;
            await this._scale.GetBodyCompositonAsync(_address,
                new User { Sex = _sex, Age = _age, Height = _height, ScaleType = _scaleType, BindKey = _bindkey });
            this.OnStop();
        }

        private async void OnStop()
        {
            this._scale.StopSearch();
            this.IsBusyForm = false;
            if (this._scale.BodyComposition is null || !this._scale.BodyComposition.IsValid)
            {
                var msg = AppSnippets.DataCouldNotBeObtained;
                await Dialog.AlertAsync(AppSnippets.Problem, msg,
                    AppSnippets.OK);
                _logService.LogError(msg);
                ScanningLabel = AppSnippets.NotFound;
            }
            else
            {
                App.BodyComposition = this._scale.BodyComposition;
                _ = ExportToHealthConnectAsync();
                var oneClickEnabled = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false);
                if (!oneClickEnabled)
                {
                    await Shell.Current.GoToAsync("//FormPage?autoUpload=false");
                    return;
                }

                var target = (OneClickUploadTarget)Preferences.Get(PreferencesKeys.OneClickScanAndUploadTarget, (byte)OneClickUploadTarget.Garmin);
                switch (target)
                {
                    case OneClickUploadTarget.Intervals:
                        await Shell.Current.GoToAsync("//IntervalsFormPage?autoUpload=true");
                        break;
                    case OneClickUploadTarget.Both:
                        await Shell.Current.GoToAsync("//IntervalsFormPage?autoUpload=true&continueTo=garmin");
                        break;
                    case OneClickUploadTarget.Garmin:
                    default:
                        await Shell.Current.GoToAsync("//FormPage?autoUpload=true");
                        break;
                }
            }
        }

        private async Task ExportToHealthConnectAsync()
        {
            try
            {
                if (!Preferences.Get(PreferencesKeys.ExportToHealthConnect, false))
                {
                    return;
                }

                var bodyComposition = App.BodyComposition;
                if (bodyComposition is null || !bodyComposition.IsValid)
                {
                    return;
                }

                if (!_healthConnectService.IsSupported)
                {
                    return;
                }

                var result = await _healthConnectService.WriteAsync(bodyComposition);
                if (!result.IsSuccess && !result.IsUnavailable)
                {
                    _logService.LogError($"Health Connect export failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Health Connect export failed: {ex.Message}");
            }
        }

        private async void OnCancel()
        {
            await this._scale.CancelSearchAsync();
            this.IsBusyForm = false;
        }

        private async Task<PermissionStatus> GetLocationWhenInUsePermissionStatusAsync()
        {
            var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationPermissionStatus != PermissionStatus.Granted)
            {
                locationPermissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return locationPermissionStatus;
        }

        private async Task<PermissionStatus> GetLocationAlwaysPermissionStatusAsync()
        {
            var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (locationPermissionStatus != PermissionStatus.Granted)
            {
                locationPermissionStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
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