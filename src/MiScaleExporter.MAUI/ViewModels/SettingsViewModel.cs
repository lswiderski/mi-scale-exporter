using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.MAUI.Utils;
using MiScaleExporter.Models;
using MiScaleExporter.Services;



namespace MiScaleExporter.MAUI.ViewModels
{
    public class SettingsViewModel : BaseViewModel, ISettingsViewModel
    {
        public SettingsViewModel()
        {
            this.Title = AppSnippets.Settings;
            // Initialize birthDate with a reasonable default to avoid binding issues
            this._birthDate = DateTime.Today.AddYears(-25);
            ResetCommand = new Command(() =>
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                    this.ApiAddress = string.Empty;
                    Preferences.Remove(PreferencesKeys.OneClickScanAndUpload);
                    Preferences.Remove(PreferencesKeys.UseExternalAPI);
                    Preferences.Remove(PreferencesKeys.ShowDebugInfo);
                    Preferences.Remove(PreferencesKeys.HideAds);
                    Preferences.Remove(PreferencesKeys.MuscleMassAsPercentage);
                    Preferences.Remove(PreferencesKeys.DisplayWeightInLbs);
                    Preferences.Remove(PreferencesKeys.UseChinaServer);
                    Preferences.Remove(PreferencesKeys.UserAge);
                    Preferences.Remove(PreferencesKeys.UserBirthDate);
                    Preferences.Remove(PreferencesKeys.UseBirthDateMode);
                    Preferences.Remove(PreferencesKeys.UseFatCalibration);
                    Preferences.Remove(PreferencesKeys.FatCalibrationPoints);
                    Preferences.Remove(PreferencesKeys.XiaomiUserId);
                    Preferences.Remove(PreferencesKeys.XiaomiPassToken);
                    Preferences.Remove(PreferencesKeys.XiaomiAccountRegion);
                    Preferences.Remove(PreferencesKeys.XiaomiScaleModel);
                    this.UseBirthDateMode = false;
                    this.ManualAge = "25";
                    this._useFatCalibration = false;
                    OnPropertyChanged(nameof(UseFatCalibration));
                    this.CalibrationPoints.Clear();
                    OnPropertyChanged(nameof(CalibrationSummary));
                    this.XiaomiUserId = string.Empty;
                    this.XiaomiPassToken = string.Empty;
                    this.XiaomiAccountRegion = string.Empty;
                    this.XiaomiScaleModel = string.Empty;
                }
            );
            GetBLEKeyCommand = new Command(async () => await Launcher.OpenAsync("https://lswiderski.github.io/mi-scale-exporter/#steps-to-connect-xiaomi-body-composition-scale-s400"));
            ResetTokensCommand = new Command(_clearTokens);
            AddCalibrationPointCommand = new Command(AddCalibrationPoint);
            RemoveCalibrationPointCommand = new Command<FatCalibrationPoint>(RemoveCalibrationPoint);
            _newPointDate = DateTime.Today;
        }

        public ICommand ResetCommand { get; }
        public ICommand GetBLEKeyCommand { get; }
        public ICommand ResetTokensCommand { get; }
        public ICommand AddCalibrationPointCommand { get; }
        public ICommand RemoveCalibrationPointCommand { get; }

        public List<string> RegionOptions { get; } = new() { "cn", "de", "ru", "sg", "us", "i2" };

        public List<PickerOption> ScaleModelOptions { get; } = new()
        {
            new PickerOption("yunmai.scales.ms104", "S400 - yunmai.scales.ms104"),
            new PickerOption("yunmai.scales.ms103", "S400 - yunmai.scales.ms103"),
            new PickerOption("yunmai.scales.ms107", "S400 - yunmai.scales.ms107"),
            new PickerOption("yunmai.scales.ms106", "S200 - yunmai.scales.ms106"),
            new PickerOption("yunmai.scales.ms116", "S800 - yunmai.scales.ms116")
        };


        public async Task LoadPreferencesAsync()
        {
            _isLoadingPreferences = true;
            try
            {
                this._apiAddress = Preferences.Get(PreferencesKeys.ApiServerAddressOverride, string.Empty);
                this._oneClickScanAndUpload = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false);
                this._useExternalAPI = Preferences.Get(PreferencesKeys.UseExternalAPI, false);
                this._showDebugInfo = Preferences.Get(PreferencesKeys.ShowDebugInfo, false);
                this._hideAds = Preferences.Get(PreferencesKeys.HideAds, false);
                this._muscleMassAsPercentage = Preferences.Get(PreferencesKeys.MuscleMassAsPercentage, false);
                this._displayWeightInLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
                this._useChinaServer = Preferences.Get(PreferencesKeys.UseChinaServer, false);

                // Load age or birthday mode
                this._useBirthDateMode = Preferences.Get(PreferencesKeys.UseBirthDateMode, false);
                
                if (_useBirthDateMode)
                {
                    // Load birthday
                    var birthDateTicks = Preferences.Get(PreferencesKeys.UserBirthDate, 0L);
                    this._birthDate = birthDateTicks > 0 ? new DateTime(birthDateTicks) : DateTime.Today.AddYears(-25);
                    this._manualAge = 0; // Not used in birthday mode
                }
                else
                {
                    // Load manual age
                    this._manualAge = Preferences.Get(PreferencesKeys.UserAge, 25);
                    this._birthDate = DateTime.Today.AddYears(-_manualAge); // For reference only
                }
                
                this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
                this._sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male);
                this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
                this._scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
                this._email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
                this._password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);
                this._bindkey = Preferences.Get(PreferencesKeys.S400Bindkey, string.Empty);

                // Load Xiaomi Settings
                this._xiaomiUserId = Preferences.Get(PreferencesKeys.XiaomiUserId, string.Empty);
                this._xiaomiPassToken = Preferences.Get(PreferencesKeys.XiaomiPassToken, string.Empty);
                this._xiaomiAccountRegion = Preferences.Get(PreferencesKeys.XiaomiAccountRegion, string.Empty);
                this._xiaomiScaleModel = Preferences.Get(PreferencesKeys.XiaomiScaleModel, string.Empty);

                this._useFatCalibration = Preferences.Get(PreferencesKeys.UseFatCalibration, false);
                this.CalibrationPoints = new ObservableCollection<FatCalibrationPoint>(
                    FatCalibration.LoadPoints().OrderBy(p => p.Date));
                OnPropertyChanged(nameof(CalibrationSummary));

                NotifyAllPropertiesChanged();
            }
            finally
            {
                _isLoadingPreferences = false;
            }
        }

        private bool ValidateProfile()
        {
            return !String.IsNullOrWhiteSpace(_address)
                                        && _height > 0 && _height < 220
                                        && Age > 0 && Age < 99;
        }

        private string _apiAddress;

        public string ApiAddress
        {
            get => _apiAddress;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                }
                else if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    {
                        Preferences.Set(PreferencesKeys.ApiServerAddressOverride, value);
                    }
                }
                SetProperty(ref _apiAddress, value);
            }
        }



        private bool _oneClickScanAndUpload;

        public bool OneClickScanAndUpload
        {
            get => _oneClickScanAndUpload;
            set
            {
                Preferences.Set(PreferencesKeys.OneClickScanAndUpload, value);
                SetProperty(ref _oneClickScanAndUpload, value);
            }
        }

        private bool _useExternalAPI;

        public bool UseExternalAPI
        {
            get => _useExternalAPI;
            set
            {
                Preferences.Set(PreferencesKeys.UseExternalAPI, value);
                SetProperty(ref _useExternalAPI, value);
            }
        }

        private bool _muscleMassAsPercentage;

        public bool MuscleMassAsPercentage
        {
            get => _muscleMassAsPercentage;
            set
            {
                Preferences.Set(PreferencesKeys.MuscleMassAsPercentage, value);
                SetProperty(ref _muscleMassAsPercentage, value);
            }
        }

        private bool _displayWeightInLbs;

        public bool DisplayWeightInLbs
        {
            get => _displayWeightInLbs;
            set
            {
                Preferences.Set(PreferencesKeys.DisplayWeightInLbs, value);
                SetProperty(ref _displayWeightInLbs, value);
            }
        }

        private bool _useChinaServer;

        public bool UseChinaServer
        {
            get => _useChinaServer;
            set
            {
                Preferences.Set(PreferencesKeys.UseChinaServer, value);
                SetProperty(ref _useChinaServer, value);
            }
        }

        private bool _showDebugInfo;

        public bool ShowDebugInfo
        {
            get => _showDebugInfo;
            set
            {
                Preferences.Set(PreferencesKeys.ShowDebugInfo, value);
                SetProperty(ref _showDebugInfo, value);
            }
        }

        private bool _hideAds;

        public bool HideAds
        {
            get => _hideAds;
            set
            {
                Preferences.Set(PreferencesKeys.HideAds, value);
                SetProperty(ref _hideAds, value);
            }
        }

        public void SexRadioSetToMale()
        {
            this.Sex = Sex.Male;
        }

        public void SexRadioSetToFemale()
        {
            this.Sex = Sex.Female;
        }

        public void ScaleTypeSetToBodyCompositionScale()
        {
            this.ScaleType = ScaleType.MiBodyCompositionScale;
        }

         public void ScaleTypeSetToMiscale()
        {
            this.ScaleType = ScaleType.MiSmartScale;
        }

        public void ScaleTypeSetToS400()
        {
            this.ScaleType = ScaleType.S400;
        }

        public void ScaleTypeSetToXiaomiHome()
        {
            this.ScaleType = ScaleType.XiaomiHome;
        }


        public void CheckPreferences()
        {

        }

        private string _address;

        public string Address
        {
            get => _address;
            set
            {
                SetProperty(ref _address, value);
                Preferences.Set(PreferencesKeys.MiScaleBluetoothAddress, value);
            }
        }

        private string _bindkey;

        public string Bindkey
        {
            get => _bindkey;
            set
            {
                SetProperty(ref _bindkey, value);
                Preferences.Set(PreferencesKeys.S400Bindkey, value);
            }
        }

        // ---- Xiaomi Settings --------------------------------------------------------

        private string _xiaomiUserId;

        public string XiaomiUserId
        {
            get => _xiaomiUserId;
            set
            {
                SetProperty(ref _xiaomiUserId, value);
                Preferences.Set(PreferencesKeys.XiaomiUserId, value ?? string.Empty);
            }
        }

        private string _xiaomiPassToken;

        public string XiaomiPassToken
        {
            get => _xiaomiPassToken;
            set
            {
                SetProperty(ref _xiaomiPassToken, value);
                Preferences.Set(PreferencesKeys.XiaomiPassToken, value ?? string.Empty);
            }
        }

        private string _xiaomiAccountRegion;

        public string XiaomiAccountRegion
        {
            get => _xiaomiAccountRegion;
            set
            {
                SetProperty(ref _xiaomiAccountRegion, value);
                Preferences.Set(PreferencesKeys.XiaomiAccountRegion, value ?? string.Empty);
            }
        }

        private string _xiaomiScaleModel;

        public string XiaomiScaleModel
        {
            get => _xiaomiScaleModel;
            set
            {
                SetProperty(ref _xiaomiScaleModel, value);
                Preferences.Set(PreferencesKeys.XiaomiScaleModel, value ?? string.Empty);
                OnPropertyChanged(nameof(SelectedScaleModelOption));
            }
        }

        public PickerOption SelectedScaleModelOption
        {
            get => ScaleModelOptions.FirstOrDefault(o => o.Value == _xiaomiScaleModel) ?? ScaleModelOptions.First();
            set
            {
                if (value != null && _xiaomiScaleModel != value.Value)
                {
                    XiaomiScaleModel = value.Value;
                }
            }
        }

        private int _age;

        public int Age
        {
            get
            {
                if (_useBirthDateMode)
                {
                    // Calculate age from birthday
                    var today = DateTime.Today;
                    var age = today.Year - _birthDate.Year;
                    if (_birthDate.Date > today.AddYears(-age))
                    {
                        age--;
                    }
                    return age;
                }
                else
                {
                    // Return manually entered age
                    return _manualAge;
                }
            }
        }

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
                    if (result == 0) return;
                    Preferences.Set(PreferencesKeys.UserAge, result);
                    OnPropertyChanged(nameof(Age));
                }
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
                    Preferences.Set(PreferencesKeys.UseBirthDateMode, value);
                    OnPropertyChanged(nameof(Age));
                }
            }
        }

        private DateTime _birthDate;
        private bool _isLoadingPreferences = false;

        public DateTime BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    // Only save to preferences if we're not in the middle of loading
                    if (!_isLoadingPreferences)
                    {
                        Preferences.Set(PreferencesKeys.UserBirthDate, value.Ticks);
                        OnPropertyChanged(nameof(Age));
                    }
                }
            }
        }

        private int _height;

        public string Height
        {
            get => _height.ToString();
            set
            {
                if (value is null) return;
                if (int.TryParse(value, out var result))
                {
                    SetProperty(ref _height, result);
                    if (result == 0) return;
                    Preferences.Set(PreferencesKeys.UserHeight, result);
                }
            }
        }

        public bool IsMaleSelected
        {
            get => _sex == Sex.Male;
        }

        public bool IsFemaleSelected
        {
            get => _sex == Sex.Female;
        }

        private Sex _sex;

        public Sex Sex
        {
            get => _sex;
            set
            {
                if (_sex == value) return;
                SetProperty(ref _sex, value);
                Preferences.Set(PreferencesKeys.UserSex, (byte)value);
            }
        }



        public bool IsMiBodyCompositionScaleSelected
        {
            get => _scaleType == ScaleType.MiBodyCompositionScale;
        }

        public bool IsMiSmartScaleSelected
        {
            get => _scaleType == ScaleType.MiSmartScale;
        }

        public bool IsS400Selected
        {
            get => _scaleType == ScaleType.S400;
        }

        public bool IsXiaomiHomeSelected
        {
            get => _scaleType == ScaleType.XiaomiHome;
        }

        public bool IsS400OrXiaomiHomeSelected
        {
            get => _scaleType == ScaleType.S400 || _scaleType == ScaleType.XiaomiHome;
        }

        private ScaleType _scaleType;

        public ScaleType ScaleType
        {
            get => _scaleType;
            set
            {
                if (_scaleType != value)
                {
                    SetProperty(ref _scaleType, value);
                    Preferences.Set(PreferencesKeys.ScaleType, (byte)value);
                    // Notify dependent properties
                    OnPropertyChanged(nameof(IsMiBodyCompositionScaleSelected));
                    OnPropertyChanged(nameof(IsMiSmartScaleSelected));
                    OnPropertyChanged(nameof(IsS400Selected));
                    OnPropertyChanged(nameof(IsXiaomiHomeSelected));
                    OnPropertyChanged(nameof(IsS400OrXiaomiHomeSelected));
                }

            }
        }

        private string _email;

        public string Email
        {
            get => _email;
            set
            {if(_email != value)
                {
                    SetProperty(ref _email, value);
                    Preferences.Set(PreferencesKeys.GarminUserEmail, value);
                    this._clearTokens();
                }
                
            }
        }

        private string _password;

        public string Password
        {
            get => _password;
            set
            {
                if(_password != value)
                {
                    SetProperty(ref _password, value);
                    SecureStorage.SetAsync(PreferencesKeys.GarminUserPassword, value);
                    this._clearTokens();
                }
               
            }
        }

        private void _clearTokens()
        {
            SecureStorage.SetAsync(PreferencesKeys.GarminUserAccessToken, string.Empty);
            SecureStorage.SetAsync(PreferencesKeys.GarminUserTokenSecret, string.Empty);
        }

        // ---- Body fat calibration --------------------------------------------------------

        private bool _useFatCalibration;

        public bool UseFatCalibration
        {
            get => _useFatCalibration;
            set
            {
                Preferences.Set(PreferencesKeys.UseFatCalibration, value);
                SetProperty(ref _useFatCalibration, value);
                OnPropertyChanged(nameof(CalibrationSummary));
            }
        }

        private ObservableCollection<FatCalibrationPoint> _calibrationPoints = new();

        public ObservableCollection<FatCalibrationPoint> CalibrationPoints
        {
            get => _calibrationPoints;
            private set => SetProperty(ref _calibrationPoints, value);
        }

        private DateTime _newPointDate;

        public DateTime NewPointDate
        {
            get => _newPointDate;
            set => SetProperty(ref _newPointDate, value);
        }

        private string _newPointScaleFat;

        public string NewPointScaleFat
        {
            get => _newPointScaleFat;
            set => SetProperty(ref _newPointScaleFat, value);
        }

        private string _newPointTrueFat;

        public string NewPointTrueFat
        {
            get => _newPointTrueFat;
            set => SetProperty(ref _newPointTrueFat, value);
        }

        /// <summary>Human-readable description of the current fit, shown under the table.</summary>
        public string CalibrationSummary
        {
            get
            {
                var count = _calibrationPoints?.Count ?? 0;
                if (count == 0)
                {
                    return "No calibration points yet. Add at least one to enable correction.";
                }

                var fit = FatCalibration.ComputeFit(_calibrationPoints.ToList());
                var formula = $"corrected = {fit.A:0.###} × scale {(fit.B >= 0 ? "+" : "−")} {Math.Abs(fit.B):0.##}";
                var quality = double.IsNaN(fit.R2)
                    ? (count == 1 ? "single-point ratio" : "fit quality unavailable")
                    : $"R² = {fit.R2:0.000}";
                var status = _useFatCalibration ? "active" : "disabled";
                return $"{count} point{(count == 1 ? "" : "s")} · {formula} · {quality} · {status}";
            }
        }

        private void AddCalibrationPoint()
        {
            var scaleFat = DoubleValueParser.ParseValueFromUsersCulture(_newPointScaleFat);
            var trueFat = DoubleValueParser.ParseValueFromUsersCulture(_newPointTrueFat);
            if (scaleFat is not > 0 || trueFat is not > 0)
            {
                return;
            }

            _calibrationPoints.Add(new FatCalibrationPoint
            {
                Date = _newPointDate == default ? DateTime.Today : _newPointDate,
                ScaleFat = scaleFat.Value,
                TrueFat = trueFat.Value,
            });

            CalibrationPoints = new ObservableCollection<FatCalibrationPoint>(
                _calibrationPoints.OrderBy(p => p.Date));

            FatCalibration.SavePoints(_calibrationPoints);

            NewPointScaleFat = string.Empty;
            NewPointTrueFat = string.Empty;
            OnPropertyChanged(nameof(CalibrationSummary));
        }

        private void RemoveCalibrationPoint(FatCalibrationPoint point)
        {
            if (point == null || !_calibrationPoints.Contains(point))
            {
                return;
            }

            _calibrationPoints.Remove(point);
            FatCalibration.SavePoints(_calibrationPoints);
            OnPropertyChanged(nameof(CalibrationSummary));
        }

    }
}