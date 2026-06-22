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

        private string _address;
        private int _age;
        private int _height;
        private Models.Sex _sex;
        private ScaleType _scaleType;

        private string _bindkey;

        public ScaleViewModel(IScale scale, ILogService logService)
        {
            _scale = scale;
            _logService = logService;

            Title = AppSnippets.MiScaleData;
            CancelCommand = new Command(OnCancel);
            StopCommand = new Command(OnStop);
            GuestScanCommand = new Command(async () => await StartGuestScan());
        }

        public async Task CheckPreferencesAsync()
        {
            App.IsGuestMeasurement = false;
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
                   // await App.Current.MainPage.Navigation.PopAsync();
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
                        await Application.Current.MainPage.DisplayAlert(AppSnippets.Problem, AppSnippets.PermissionBluetoothRequired,
                           AppSnippets.OK);
                        return false;
                    }

                    if (await GetLocationWhenInUsePermissionStatusAsync() != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert(AppSnippets.Problem, AppSnippets.PermissionLocationRequired,
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
                            await Application.Current.MainPage.DisplayAlert(AppSnippets.Problem, AppSnippets.PermissionLocationRequired,
                            AppSnippets.OK);
                            return false;
                        }
                    }
                }

            }

            // Check if Bluetooth is enabled
            if (CrossBluetoothLE.Current.State != BluetoothState.On)
            {
                await Application.Current.MainPage.DisplayAlert(
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
                // When the user toggles Guest mode mid-scan, the setter cancels the in-flight
                // scan which resolves OnStop with an invalid BC. Suppress the misleading
                // "Data could not be obtained" alert in that case.
                if (_suppressNextStopAlert)
                {
                    _suppressNextStopAlert = false;
                    return;
                }
                var msg = AppSnippets.DataCouldNotBeObtained;
                await Application.Current.MainPage.DisplayAlert(AppSnippets.Problem, msg,
                    AppSnippets.OK);
                _logService.LogError(msg);
                ScanningLabel = AppSnippets.NotFound;
            }
            else
            {
                App.BodyComposition = this._scale.BodyComposition;

                if (App.IsGuestMeasurement)
                {
                    // Guest mode never uploads, never hits FormPage, never writes prefs/cache.
                    await Shell.Current.GoToAsync("ResultPage?guest=true");
                    return;
                }

                var oneClick = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, true);
                if (oneClick)
                {
                    // One-click flow now also lands on ResultPage (after auto-upload), so the
                    // PreviousMeasurementJson cache is written by ResultPage for BOTH flows.
                    // Do not write it here — ResultPage performs read-old-THEN-write-new ordering
                    // and a duplicate write here would make the next delta compare against itself.
                    await Shell.Current.GoToAsync("ResultPage?autoupload=true");
                }
                else
                {
                    // Manual flow: show the new capability-driven Result screen.
                    // NOTE: ResultPage performs read-old-THEN-write-new ordering for the cache;
                    // writing here for the manual branch would make the delta compare against itself.
                    await Shell.Current.GoToAsync("ResultPage");
                }
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
        public Command GuestScanCommand { get; }

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

        private bool _isGuestMode;
        private bool _suppressNextStopAlert;
        public bool IsGuestMode
        {
            get => _isGuestMode;
            set
            {
                if (SetProperty(ref _isGuestMode, value) && value)
                {
                    // Entering guest mode: cancel any in-flight owner auto-scan so the user
                    // can fill in temporary inputs without a scan racing underneath, and
                    // suppress the failure alert that the cancelled scan's OnStop would show.
                    _suppressNextStopAlert = true;
                    _ = this._scale.CancelSearchAsync();
                    this.IsBusyForm = false;
                }
            }
        }

        private string _guestAge = "30";
        public string GuestAge
        {
            get => _guestAge;
            set => SetProperty(ref _guestAge, value);
        }

        private string _guestHeight = "170";
        public string GuestHeight
        {
            get => _guestHeight;
            set => SetProperty(ref _guestHeight, value);
        }

        private Models.Sex _guestSex = Models.Sex.Male;
        public Models.Sex GuestSex
        {
            get => _guestSex;
            set
            {
                if (SetProperty(ref _guestSex, value))
                {
                    OnPropertyChanged(nameof(GuestSexIndex));
                }
            }
        }

        public int GuestSexIndex
        {
            get => _guestSex == Models.Sex.Female ? 1 : 0;
            set
            {
                var sex = value == 1 ? Models.Sex.Female : Models.Sex.Male;
                if (_guestSex != sex)
                {
                    _guestSex = sex;
                    OnPropertyChanged(nameof(GuestSex));
                    OnPropertyChanged(nameof(GuestSexIndex));
                }
            }
        }

        private async Task StartGuestScan()
        {
            // Cancel any owner scan FIRST, so a late OnStop from the owner scan can't see the
            // guest flag and route owner data through the guest branch.
            await this._scale.CancelSearchAsync();
            App.IsGuestMeasurement = true;

            // Same permission/Bluetooth gate the owner path uses.
            var ok = await CheckPermissions();
            if (!ok)
            {
                return;
            }

            // Read ONLY device config from prefs — never owner age/height/sex.
            var address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
            var scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
            var bindkey = Preferences.Get(PreferencesKeys.S400Bindkey, string.Empty);

            if (string.IsNullOrWhiteSpace(address))
            {
                await Shell.Current.GoToAsync($"//Settings");
                return;
            }

            if (!int.TryParse(GuestAge, out var age) || age <= 0 || age >= 120) age = 30;
            if (!int.TryParse(GuestHeight, out var height) || height <= 0 || height >= 250) height = 170;

            var guestUser = new User
            {
                Sex = GuestSex,
                Age = age,
                Height = height,
                ScaleType = scaleType,
                BindKey = bindkey,
            };

            // Snapshot the guest user so ResultPage's status bands are scored against the
            // guest's demographics instead of the owner's prefs.
            App.LastGuestUser = guestUser;

            ScanningLabel = string.Empty;
            ScaleMeasurement.Instance.Weight = "0";
            this.IsBusyForm = true;
            await this._scale.GetBodyCompositonAsync(address, guestUser);
            this.OnStop();
        }
    }
}
