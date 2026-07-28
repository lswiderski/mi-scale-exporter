using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Models;
using MiScaleExporter.Services;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class OnboardingViewModel : BaseViewModel
    {
        private static readonly Color SuccessColor = Color.FromArgb("#16A34A");
        private static readonly Color ErrorColor = Color.FromArgb("#DC2626");
        private static readonly Color NeutralColor = Color.FromArgb("#6B7280");

        private readonly IGarminAuthService _authService;

        public OnboardingViewModel(IGarminAuthService authService)
        {
            _authService = authService;
            this.Title = AppSnippets.MiScaleExporter;

            // Prefill from existing preferences so re-running the wizard keeps current values.
            this._useBirthDateMode = Preferences.Get(PreferencesKeys.UseBirthDateMode, false);
            this._manualAge = Preferences.Get(PreferencesKeys.UserAge, 25);
            var birthDateTicks = Preferences.Get(PreferencesKeys.UserBirthDate, 0L);
            this._birthDate = birthDateTicks > 0 ? new DateTime(birthDateTicks) : DateTime.Today.AddYears(-25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170).ToString();
            this._sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male);
            this._scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
            this._bindKey = Preferences.Get(PreferencesKeys.S400Bindkey, string.Empty);
            this._email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
            // Auto-upload defaults to ON for first-run users.
            this._autoUpload = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, true);

            FinishCommand = new Command(Finish);
            SkipCommand = new Command(Skip);
            HowToFindAddressCommand = new Command(async () =>
                await Launcher.OpenAsync("https://lswiderski.github.io/mi-scale-exporter/"));
            ConnectGarminCommand = new Command(async () => await ConnectGarminAsync());
            VerifyMfaCommand = new Command(async () => await VerifyMfaAsync());
        }

        public ICommand FinishCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand HowToFindAddressCommand { get; }
        public ICommand ConnectGarminCommand { get; }
        public ICommand VerifyMfaCommand { get; }

        private bool _autoUpload;
        public bool AutoUpload
        {
            get => _autoUpload;
            set
            {
                Preferences.Set(PreferencesKeys.OneClickScanAndUpload, value);
                SetProperty(ref _autoUpload, value);
            }
        }

        private string _garminStatusText;
        public string GarminStatusText
        {
            get => _garminStatusText;
            set
            {
                if (SetProperty(ref _garminStatusText, value))
                {
                    OnPropertyChanged(nameof(HasGarminStatus));
                }
            }
        }

        public bool HasGarminStatus => !string.IsNullOrEmpty(_garminStatusText);

        private Color _garminStatusColor = NeutralColor;
        public Color GarminStatusColor
        {
            get => _garminStatusColor;
            set => SetProperty(ref _garminStatusColor, value);
        }

        private bool _showMfaInput;
        public bool ShowMfaInput
        {
            get => _showMfaInput;
            set => SetProperty(ref _showMfaInput, value);
        }

        private string _mfaCode;
        public string MfaCode
        {
            get => _mfaCode;
            set => SetProperty(ref _mfaCode, value);
        }

        private async Task ConnectGarminAsync()
        {
            if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            {
                ShowMfaInput = false;
                GarminStatusColor = ErrorColor;
                GarminStatusText = AppSnippets.GarminConnectFailed;
                return;
            }

            ShowMfaInput = false;
            GarminStatusColor = NeutralColor;
            GarminStatusText = AppSnippets.GarminConnecting;

            var result = await _authService.AuthenticateAsync(_email.Trim(), _password);
            ApplyAuthResult(result);
        }

        private async Task VerifyMfaAsync()
        {
            GarminStatusColor = NeutralColor;
            GarminStatusText = AppSnippets.GarminConnecting;

            var result = await _authService.CompleteMfaAsync(_mfaCode);
            ApplyAuthResult(result);
        }

        private void ApplyAuthResult(GarminAuthResult result)
        {
            switch (result?.Status)
            {
                case GarminAuthStatus.Success:
                    ShowMfaInput = false;
                    GarminStatusColor = SuccessColor;
                    GarminStatusText = AppSnippets.GarminConnected;
                    // Default-on intent only when the user has never set the preference explicitly.
                    if (!Preferences.ContainsKey(PreferencesKeys.OneClickScanAndUpload))
                    {
                        AutoUpload = true;
                    }
                    break;
                case GarminAuthStatus.MfaRequired:
                    ShowMfaInput = true;
                    GarminStatusColor = NeutralColor;
                    GarminStatusText = AppSnippets.GarminEnterMfa;
                    break;
                default:
                    GarminStatusColor = ErrorColor;
                    GarminStatusText = string.IsNullOrWhiteSpace(result?.Message)
                        ? AppSnippets.GarminConnectFailed
                        : result.Message;
                    break;
            }
        }

        private bool _useBirthDateMode;
        public bool UseBirthDateMode
        {
            get => _useBirthDateMode;
            set
            {
                if (SetProperty(ref _useBirthDateMode, value))
                {
                    OnPropertyChanged(nameof(Age));
                    OnPropertyChanged(nameof(IsManualAgeSelected));
                    OnPropertyChanged(nameof(IsBirthDateSelected));
                }
            }
        }

        public bool IsManualAgeSelected => !_useBirthDateMode;
        public bool IsBirthDateSelected => _useBirthDateMode;

        public void AgeModeSetToManual() => this.UseBirthDateMode = false;
        public void AgeModeSetToBirthDate() => this.UseBirthDateMode = true;

        private int _manualAge;
        public string ManualAge
        {
            get => _manualAge.ToString();
            set
            {
                if (value is null) return;
                if (int.TryParse(value, out var result))
                {
                    SetProperty(ref _manualAge, result);
                    OnPropertyChanged(nameof(Age));
                }
            }
        }

        private DateTime _birthDate;
        public DateTime MaxBirthDate => DateTime.Today;
        public DateTime BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    OnPropertyChanged(nameof(Age));
                }
            }
        }

        public int Age
        {
            get
            {
                if (_useBirthDateMode)
                {
                    var today = DateTime.Today;
                    var age = today.Year - _birthDate.Year;
                    if (_birthDate.Date > today.AddYears(-age))
                    {
                        age--;
                    }
                    return age;
                }

                return _manualAge;
            }
        }

        private string _height;
        public string Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private Sex _sex;
        public Sex Sex
        {
            get => _sex;
            set
            {
                if (SetProperty(ref _sex, value))
                {
                    OnPropertyChanged(nameof(IsMaleSelected));
                    OnPropertyChanged(nameof(IsFemaleSelected));
                }
            }
        }

        public bool IsMaleSelected => _sex == Sex.Male;
        public bool IsFemaleSelected => _sex == Sex.Female;

        public void SexRadioSetToMale() => this.Sex = Sex.Male;
        public void SexRadioSetToFemale() => this.Sex = Sex.Female;

        private ScaleType _scaleType;
        public ScaleType ScaleType
        {
            get => _scaleType;
            set
            {
                if (SetProperty(ref _scaleType, value))
                {
                    OnPropertyChanged(nameof(IsMiBodyCompositionScaleSelected));
                    OnPropertyChanged(nameof(IsMiSmartScaleSelected));
                    OnPropertyChanged(nameof(IsS400Selected));
                    OnPropertyChanged(nameof(IsS400));
                }
            }
        }

        public bool IsMiBodyCompositionScaleSelected => _scaleType == ScaleType.MiBodyCompositionScale;
        public bool IsMiSmartScaleSelected => _scaleType == ScaleType.MiSmartScale;
        public bool IsS400Selected => _scaleType == ScaleType.S400;
        public bool IsS400 => _scaleType == ScaleType.S400;

        public void ScaleTypeSetToBodyCompositionScale() => this.ScaleType = ScaleType.MiBodyCompositionScale;
        public void ScaleTypeSetToMiscale() => this.ScaleType = ScaleType.MiSmartScale;
        public void ScaleTypeSetToS400() => this.ScaleType = ScaleType.S400;

        private string _address;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _bindKey;
        public string BindKey
        {
            get => _bindKey;
            set => SetProperty(ref _bindKey, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private void Finish()
        {
            Preferences.Set(PreferencesKeys.UseBirthDateMode, _useBirthDateMode);
            if (_useBirthDateMode)
            {
                Preferences.Set(PreferencesKeys.UserBirthDate, _birthDate.Ticks);
                var birthAge = Age;
                if (birthAge <= 0)
                {
                    birthAge = 25;
                }
                Preferences.Set(PreferencesKeys.UserAge, birthAge);
            }
            else
            {
                var age = _manualAge > 0 ? _manualAge : 25;
                Preferences.Set(PreferencesKeys.UserAge, age);
            }

            var height = 170;
            if (int.TryParse(_height, out var parsedHeight) && parsedHeight > 0)
            {
                height = parsedHeight;
            }
            Preferences.Set(PreferencesKeys.UserHeight, height);

            Preferences.Set(PreferencesKeys.UserSex, (byte)_sex);
            Preferences.Set(PreferencesKeys.ScaleType, (byte)_scaleType);

            if (!string.IsNullOrWhiteSpace(_address))
            {
                Preferences.Set(PreferencesKeys.MiScaleBluetoothAddress, _address.Trim());
            }
            if (!string.IsNullOrWhiteSpace(_bindKey))
            {
                Preferences.Set(PreferencesKeys.S400Bindkey, _bindKey.Trim());
            }
            if (!string.IsNullOrWhiteSpace(_email))
            {
                Preferences.Set(PreferencesKeys.GarminUserEmail, _email.Trim());
            }
            if (!string.IsNullOrWhiteSpace(_password))
            {
                SecureStorage.SetAsync(PreferencesKeys.GarminUserPassword, _password);
            }

            App.CompleteOnboardingAndGoHome();
        }

        private void Skip()
        {
            App.CompleteOnboardingAndGoHome();
        }
    }
}
