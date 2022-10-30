using System;
using System.Windows.Input;
using MiScaleExporter.Models;



namespace MiScaleExporter.MAUI.ViewModels
{
    public class SettingsViewModel : BaseViewModel, ISettingsViewModel
    {
        public SettingsViewModel()
        {
            this.Title = "Settings";
            ResetCommand = new Command(() =>
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                    this.ApiAddress = string.Empty;
                    Preferences.Remove(PreferencesKeys.ShowReceivedByteArray);
                    Preferences.Remove(PreferencesKeys.SaveToStorage);
                    Preferences.Remove(PreferencesKeys.OneClickScanAndUpload);
                    Preferences.Remove(PreferencesKeys.ShowUnstabilizedData);
                    Preferences.Remove(PreferencesKeys.DoNotWaitForImpedance);
                    Preferences.Remove(PreferencesKeys.UseExternalAPI);
                }
            );
        }

        public ICommand ResetCommand { get; }


        public async Task LoadPreferencesAsync()
        {
            this._apiAddress = Preferences.Get(PreferencesKeys.ApiServerAddressOverride, string.Empty);
            this._showReceivedByteArray = Preferences.Get(PreferencesKeys.ShowReceivedByteArray, false);
            this._saveToStorage = Preferences.Get(PreferencesKeys.SaveToStorage, false);
            this._oneClickScanAndUpload = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false);
            this._showUnstabilizedData = Preferences.Get(PreferencesKeys.ShowUnstabilizedData, false);
            this._doNotWaitForImpedance = Preferences.Get(PreferencesKeys.DoNotWaitForImpedance, false);
            this._useExternalAPI = Preferences.Get(PreferencesKeys.UseExternalAPI, false);

            this._age = Preferences.Get(PreferencesKeys.UserAge, 25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
            this._sex = (Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Sex.Male);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
            this._scaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
            this._email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
            this._password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);
            NotifyAllPropertiesChanged();
        }

        private bool ValidateProfile()
        {
            return !String.IsNullOrWhiteSpace(_address)
                                        && _height > 0 && _height < 220
                                        && _age > 0 && _age < 99;
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

        private bool _saveToStorage;

        public bool SaveToStorage
        {
            get => _saveToStorage;
            set
            {
                Preferences.Set(PreferencesKeys.SaveToStorage, value);
                SetProperty(ref _saveToStorage, value);
            }
        }

        private bool _showReceivedByteArray;

        public bool ShowReceivedByteArray
        {
            get => _showReceivedByteArray;
            set
            {
                Preferences.Set(PreferencesKeys.ShowReceivedByteArray, value);
                SetProperty(ref _showReceivedByteArray, value);
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

        private bool _showUnstabilizedData;

        public bool ShowUnstabilizedData
        {
            get => _showUnstabilizedData;
            set
            {
                Preferences.Set(PreferencesKeys.ShowUnstabilizedData, value);
                SetProperty(ref _showUnstabilizedData, value);
            }
        }

        private bool _doNotWaitForImpedance;

        public bool DoNotWaitForImpedance
        {
            get => _doNotWaitForImpedance;
            set
            {
                Preferences.Set(PreferencesKeys.DoNotWaitForImpedance, value);
                SetProperty(ref _doNotWaitForImpedance, value);
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



        public void SexRadioButtonChanged(object s, CheckedChangedEventArgs e)
        {
            var radio = s as RadioButton;
            this.Sex = radio.Value as string == "1" ? Models.Sex.Male : Models.Sex.Female;
        }

        public void ScaleTypeRadioButton_Changed(object s, CheckedChangedEventArgs e)
        {
            var radio = s as RadioButton;
            this.ScaleType = radio.Value as string == "1" ? Models.ScaleType.MiSmartScale : Models.ScaleType.MiBodyCompositionScale;
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

        private int _age;

        public string Age
        {
            get => _age.ToString();
            set
            {
                if (value is null) return;
                if (int.TryParse(value, out var result))
                {
                    SetProperty(ref _age, result);
                    if (result == 0) return;
                    Preferences.Set(PreferencesKeys.UserAge, result);
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
                }
               
            }
        }

    }
}