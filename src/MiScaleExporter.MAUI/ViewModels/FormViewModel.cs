using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using MiScaleExporter.MAUI;
using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.MAUI.Utils;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using System.Globalization;
using System.Threading;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class FormViewModel : BaseViewModel, IFormViewModel
    {
        private readonly IGarminService _garminService;
        private readonly IFileSaver _fileSaver;
        private const double KgToLbsConversion = 2.20462;

        public FormViewModel(IGarminService garminService, IFileSaver fileSaver)
        {
            _garminService = garminService;
            Title = AppSnippets.GarminBodyCompositionForm;
            Date = DateTime.Now;
            Time = DateTime.Now.TimeOfDay;
            _muscleMassAsKg = true;
            _showWeightInKg = true;
            UploadCommand = new Command(OnUpload, ValidateSave);
            GenerateFitFileCommand = new Command(OnGenerateFitFileAsync);
            CancelMFACommand = new Command(OnCancelMFA);
            this.PropertyChanged +=
                (_, __) => UploadCommand.ChangeCanExecute();

            this._fileSaver = fileSaver;
        }

        public async Task LoadPreferencesAsync()
        {
            this._email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
            this._password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);

            this._accessToken = await SecureStorage.GetAsync(PreferencesKeys.GarminUserAccessToken);
            this._tokenSecret = await SecureStorage.GetAsync(PreferencesKeys.GarminUserTokenSecret);
            this._saveTokens = !string.IsNullOrWhiteSpace(_email) && !string.IsNullOrWhiteSpace(_password);

            this.ShowEmail = string.IsNullOrWhiteSpace(_email);
            this.ShowPassword = string.IsNullOrWhiteSpace(_password);

            this._displayWeightInLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
            this.ShowWeightInKg = !_displayWeightInLbs;
            this.ShowWeightInLbs = _displayWeightInLbs;

            this.MuscleMassAsPercentage = Preferences.Get(PreferencesKeys.MuscleMassAsPercentage, false);
            this.MuscleMassAsKg = (!MuscleMassAsPercentage && ShowWeightInKg);
            this.MuscleMassAsLbs = (!MuscleMassAsPercentage && !ShowWeightInKg);


            this.Date = DateTime.Now;
            this.Time = DateTime.Now.TimeOfDay;
        }
        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(_email)
                   && !String.IsNullOrWhiteSpace(_password);
        }

        public void AutoUpload()
        {
            // Guest data must never auto-upload, even if a stale BC is sitting under the guest flag.
            if (App.IsGuestMeasurement) return;
            if (!string.IsNullOrWhiteSpace(_email)
                 && !string.IsNullOrWhiteSpace(_password))
            {
                _isAutoUpload = true;
                OnUpload();
            }
        }

        private bool _isAutoUpload;

        private async void OnUpload()
        {
            // Guest data must never reach the Garmin upload path, even via manual taps or
            // stale flag/BC combinations.
            if (App.IsGuestMeasurement) return;
            var wasAutoUpload = _isAutoUpload;
            this.IsBusyForm = true;
            try
            {
                var credencials = new CredentialsData
                {
                    Email = _email,
                    Password = _password,
                    AccessToken = this._accessToken,
                    TokenSecret = this._tokenSecret,
                };

                GarminApiResponse response = null;
                bool responseIsSynthetic = false;
                var request = this.PrepareRequest();
                var measuredAt = Date.Date.Add(Time);
                try
                {
                    response = await this._garminService.UploadAsync(request, measuredAt, credencials);
                }
                catch
                {
                    // Transient (network/IO) failure: wait briefly and retry exactly once.
                    // Do NOT retry on a returned IsSuccess==false (logical failure, e.g. bad creds/MFA).
                    await Task.Delay(1500);
                    try
                    {
                        response = await this._garminService.UploadAsync(request, measuredAt, credencials);
                    }
                    catch (Exception ex2)
                    {
                        responseIsSynthetic = true;
                        response = new GarminApiResponse
                        {
                            IsSuccess = false,
                            MFARequested = false,
                            Message = ex2.Message,
                        };
                    }
                }

                // --- Token-save logic preserved exactly as before ---
                // Guard: when the response is synthetic (both upload attempts threw — pure transient
                // failure), skip the token-save block entirely so we don't wipe the user's saved
                // Garmin OAuth tokens in SecureStorage on a flaky network.
                if (!responseIsSynthetic)
                {
                    if (this._saveTokens)
                    {
                        this._accessToken = response?.AccessToken ?? string.Empty;
                        this._tokenSecret = response?.TokenSecret ?? string.Empty;
                    }
                    else
                    {
                        this._accessToken = string.Empty;
                        this._tokenSecret = string.Empty;
                    }
                    await SecureStorage.SetAsync(PreferencesKeys.GarminUserAccessToken, this._accessToken);
                    await SecureStorage.SetAsync(PreferencesKeys.GarminUserTokenSecret, this._tokenSecret);
                }

                if (response != null && !response.LocalReceiptSaved)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        AppSnippets.Response,
                        response.Message,
                        AppSnippets.OK);
                    if (response.IsSuccess)
                    {
                        await Shell.Current.GoToAsync("//ScalePage/ResultPage?uploaded=true");
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("..?autoUpload=false");
                    }
                    return;
                }

                // --- MFA state-setting logic preserved exactly as before ---
                if (response?.MFARequested ?? false)
                {
                    this.ShowMFACode = true;
                    this.ShowEmail = false;
                    this.ShowPassword = false;
                    this.ExternalApiClientId = response?.ExternalApiClientId;
                    // MFA requested: stay on FormPage so the user can enter the code.
                    return;
                }
                else
                {
                    this.ShowMFACode = false;
                    this.MFACode = null;
                    this.ExternalApiClientId = null;
                    this.ShowEmail = string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty));
                    this.ShowPassword = string.IsNullOrWhiteSpace(await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword));
                }

                if (response?.IsSuccess ?? false)
                {
                    if (wasAutoUpload)
                    {
                        // Silent auto-upload success: non-blocking toast, then land on the Result screen.
                        // Absolute flyout-root base (//ScalePage) + the registered global ResultPage route,
                        // so the back-stack is ScalePage -> ResultPage (FormPage is a flyout root, so ".." is unsafe).
                        try { await Toast.Make(AppSnippets.UploadedToGarmin).Show(); } catch { }
                        await Shell.Current.GoToAsync("//ScalePage/ResultPage?uploaded=true");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(AppSnippets.Response, AppSnippets.Uploaded, AppSnippets.OK);
                        await Shell.Current.GoToAsync("..?autoUpload=false");
                    }
                }
                else
                {
                    // Failure (both modes): show the error and pop back to ScalePage. Do NOT route to ResultPage.
                    await Application.Current.MainPage.DisplayAlert(AppSnippets.Response, response?.Message, AppSnippets.OK);
                    await Shell.Current.GoToAsync("..?autoUpload=false");
                }
            }
            finally
            {
                this.IsBusyForm = false;
                _isAutoUpload = false;
            }
        }

        private async void OnGenerateFitFileAsync()
        {
            this.IsBusyForm = true;

            var response = await this._garminService.GenerateFitFileAsync(this.PrepareRequest(), Date.Date.Add(Time));

            if (!response.IsSuccess && response.file != null)
            {
                await Application.Current.MainPage.DisplayAlert(AppSnippets.Response, response?.Message, AppSnippets.OK);
            }
            else
            {
                using var stream = new MemoryStream(response.file);
                var fileSaverResult = await _fileSaver.SaveAsync($"activity_{Date.Date.Add(Time).ToShortDateString()}.fit", stream);
                if (fileSaverResult.IsSuccessful)
                {
                    await Toast.Make($"The file was saved successfully to location: {fileSaverResult.FilePath}").Show();
                }
                else
                {
                    await Toast.Make($"The file was not saved successfully with error: {fileSaverResult.Exception.Message}").Show();
                }
            }

            this.IsBusyForm = false;

            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..?autoUpload=false");
        }

        private async void OnCancelMFA()
        {
            this.ShowMFACode = false;
            this.MFACode = null;
            this.ExternalApiClientId = null;
            this.ShowEmail = string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty));
            this.ShowPassword = string.IsNullOrWhiteSpace(await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword));
        }

        private BodyComposition PrepareRequest()
        {
            var bc = new BodyComposition
            {
                Fat = DoubleValueParser.ParseValueFromUsersCulture(_fat) ?? 0,
                BodyType = _bodyType ?? 0,
                Weight = ConvertToKg(DoubleValueParser.ParseValueFromUsersCulture(_weight) ?? 0),
                BoneMass = ConvertToKg(DoubleValueParser.ParseValueFromUsersCulture(_boneMass) ?? 0),
                MuscleMass = ConvertToKg(DoubleValueParser.ParseValueFromUsersCulture(_muscleMass) ?? 0),
                MetabolicAge = DoubleValueParser.ParseValueFromUsersCulture(_metabolicAge) ?? 0,
                ProteinPercentage = DoubleValueParser.ParseValueFromUsersCulture(_proteinPercentage) ?? 0,
                VisceralFat = DoubleValueParser.ParseValueFromUsersCulture(_visceralFat) ?? 0,
                BMI = DoubleValueParser.ParseValueFromUsersCulture(_bmi) ?? 0,
                BMR = DoubleValueParser.ParseValueFromUsersCulture(_bmr) ?? 0,
                WaterPercentage = DoubleValueParser.ParseValueFromUsersCulture(_waterPercentage) ?? 0,
                MFACode = _mfaCode,
                ExternalApiClientId = _externalApiClientId,
                MeasurementId = App.BodyComposition?.MeasurementId,
                MeasuredAt = App.BodyComposition?.MeasuredAt,
                MeasurementQuality = App.BodyComposition?.MeasurementQuality,
                HasImpedance = App.BodyComposition?.HasImpedance == true,
            };

            if (Preferences.Get(PreferencesKeys.MuscleMassAsPercentage, false)
                && bc.MuscleMass != 0
                && bc.Weight != 0)
            {
                bc.MuscleMass = (bc.MuscleMass / 100) * bc.Weight;
            }
            return bc;
        }

        public void LoadBodyComposition()
        {
            if (App.BodyComposition is null) return;

            if (App.BodyComposition.Date != default)
            {
                Date = App.BodyComposition.Date.Date;
                Time = App.BodyComposition.Date.TimeOfDay;
            }

            Weight = ConvertFromKg(App.BodyComposition.Weight).ToString("0.##");
            BMI = App.BodyComposition.BMI.ToString();
            BoneMass = ConvertFromKg(App.BodyComposition.BoneMass).ToString("0.##");
            MuscleMass = ConvertFromKg(App.BodyComposition.MuscleMass).ToString("0.##");
            IdealWeight = ConvertFromKg(App.BodyComposition.IdealWeight).ToString("0.##");
            BMR = App.BodyComposition.BMR.ToString();
            MetabolicAge = App.BodyComposition.MetabolicAge.ToString();
            ProteinPercentage = App.BodyComposition.ProteinPercentage.ToString();
            VisceralFat = App.BodyComposition.VisceralFat.ToString();
            Fat = App.BodyComposition.Fat.ToString();
            WaterPercentage = App.BodyComposition.WaterPercentage.ToString();
            BodyType = App.BodyComposition.BodyType;
            IsAutomaticCalculation = true;
        }

        public Command UploadCommand { get; }
        public Command CancelMFACommand { get; }

        public Command GenerateFitFileCommand { get; }

        private string _weight;

        public string Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, DoubleValueParser.CheckValue(value));
        }

        private string _bmi;

        public string BMI
        {
            get => _bmi;
            set => SetProperty(ref _bmi, DoubleValueParser.CheckValue(value));
        }

        private string _idealWeight;

        public string IdealWeight
        {
            get => _idealWeight;
            set => SetProperty(ref _idealWeight, DoubleValueParser.CheckValue(value));
        }

        private string _metabolicAge;

        public string MetabolicAge
        {
            get => _metabolicAge;
            set => SetProperty(ref _metabolicAge, DoubleValueParser.CheckValue(value));
        }

        private string _proteinPercentage;

        public string ProteinPercentage
        {
            get => _proteinPercentage;
            set => SetProperty(ref _proteinPercentage, DoubleValueParser.CheckValue(value));
        }

        private string _bmr;

        public string BMR
        {
            get => _bmr;
            set => SetProperty(ref _bmr, DoubleValueParser.CheckValue(value));
        }

        private string _fat;

        public string Fat
        {
            get => _fat;
            set => SetProperty(ref _fat, DoubleValueParser.CheckValue(value));
        }

        private string _muscleMass;

        public string MuscleMass
        {
            get => _muscleMass;
            set => SetProperty(ref _muscleMass, DoubleValueParser.CheckValue(value));
        }

        private string _boneMass;

        public string BoneMass
        {
            get => _boneMass;
            set => SetProperty(ref _boneMass, DoubleValueParser.CheckValue(value));
        }

        private string _visceralFat;

        public string VisceralFat
        {
            get => _visceralFat;
            set => SetProperty(ref _visceralFat, DoubleValueParser.CheckValue(value));
        }

        private int? _bodyType;

        public int? BodyType
        {
            get => _bodyType;
            set => SetProperty(ref _bodyType, value);
        }

        private string _waterPercentage;

        public string WaterPercentage
        {
            get => _waterPercentage;
            set => SetProperty(ref _waterPercentage, DoubleValueParser.CheckValue(value));
        }

        private string _email;

        private string _password;

        private string _accessToken;

        private string _tokenSecret;
        private bool _saveTokens;

        private DateTime _date;

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private TimeSpan _time;

        public TimeSpan Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        private bool _isAutomaticCalculation;

        public bool IsAutomaticCalculation
        {
            get => _isAutomaticCalculation;
            set => SetProperty(ref _isAutomaticCalculation, value);
        }

        private bool _isBusyForm;

        public bool IsBusyForm
        {
            get => _isBusyForm;
            set => SetProperty(ref _isBusyForm, value);
        }

        private bool _showMFACode;
        public bool ShowMFACode
        {
            get => _showMFACode;
            set => SetProperty(ref _showMFACode, value);
        }

        private string _externalApiClientId;
        public string ExternalApiClientId
        {
            get => _externalApiClientId;
            set
            {
                SetProperty(ref _externalApiClientId, value);
            }
        }
        private string _mfaCode;
        public string MFACode
        {
            get => _mfaCode;
            set
            {
                SetProperty(ref _mfaCode, value);
            }
        }

        private bool _muscleMassAsPercentage;
        public bool MuscleMassAsPercentage
        {
            get => _muscleMassAsPercentage;
            set => SetProperty(ref _muscleMassAsPercentage, value);
        }

        private bool _muscleMassAsKg;
        public bool MuscleMassAsKg
        {
            get => _muscleMassAsKg;
            set => SetProperty(ref _muscleMassAsKg, value);
        }

        private bool _muscleMassAsLbs;
        public bool MuscleMassAsLbs
        {
            get => _muscleMassAsLbs;
            set => SetProperty(ref _muscleMassAsLbs, value);
        }

        private bool _showWeightInKg;
        public bool ShowWeightInKg
        {
            get => _showWeightInKg;
            set => SetProperty(ref _showWeightInKg, value);
        }
        private bool _showWeightInLbs;
        public bool ShowWeightInLbs
        {
            get => _showWeightInLbs;
            set => SetProperty(ref _showWeightInLbs, value);
        }

        private double ConvertFromKg(double valueInKg)
        {
            return _displayWeightInLbs ? valueInKg * KgToLbsConversion : valueInKg;
        }

        private double ConvertToKg(double displayValue)
        {
            return _displayWeightInLbs ? displayValue / KgToLbsConversion : displayValue;
        }

        private bool _displayWeightInLbs;

        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                UploadCommand?.ChangeCanExecute();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                UploadCommand?.ChangeCanExecute();
            }
        }

        private bool _showEmail;
        private bool _showPassword;

        public bool ShowEmail
        {
            get => _showEmail;
            set => SetProperty(ref _showEmail, value);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

    }
}