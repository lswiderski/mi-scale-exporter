using System;
using System.Threading.Tasks;
using MiScaleExporter.MAUI;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
 
 

namespace MiScaleExporter.MAUI.ViewModels
{
    public class FormViewModel : BaseViewModel, IFormViewModel
    {
        private readonly IGarminService _garminService;

        public FormViewModel(IGarminService garminService)
        {
            _garminService = garminService;
            Title = "Garmin Body Composition Form";
            Date = DateTime.Now;
            Time = DateTime.Now.TimeOfDay;
            UploadCommand = new Command(OnUpload, ValidateSave);
            this.PropertyChanged +=
                (_, __) => UploadCommand.ChangeCanExecute();
        }
        
        public async Task LoadPreferencesAsync()
        {
            this._email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
            this._password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);

            this.ShowEmail = string.IsNullOrWhiteSpace(_email);
            this.ShowPassword = string.IsNullOrWhiteSpace(_password);
        }
        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(_email) 
                   &&  !String.IsNullOrWhiteSpace(_password);
        }

        public void AutoUpload()
        {
            if (!string.IsNullOrWhiteSpace(_email)
                 && !string.IsNullOrWhiteSpace(_password))
            {
                OnUpload();
            }
        }

        private async void OnUpload()
        {
            this.IsBusyForm = true;
            var response = await this._garminService.UploadAsync(this.PrepareRequest(), Date.Date.Add(Time), _email, _password);
            var message = response.IsSuccess ? "Uploaded" : response.Message;
            await Application.Current.MainPage.DisplayAlert ("Response", message, "OK");
            this.IsBusyForm = false;
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..?autoUpload=false");
        }

        private BodyComposition PrepareRequest()
        {
            var bc = new BodyComposition
            {
                Fat = _fat ?? 0,
                BodyType = _bodyType ?? 0,
                Weight = _weight ?? 0,
                BoneMass = _boneMass ?? 0,
                MuscleMass = _muscleMass ?? 0,
                MetabolicAge = _metabolicAge ?? 0,
                ProteinPercentage = _proteinPercentage ?? 0,
                VisceralFat = _visceralFat ?? 0,
                BMI = _bmi ?? 0,
                BMR = _bmr ?? 0,
                WaterPercentage = _waterPercentage ?? 0,
            };
            return bc;
        }

        public void LoadBodyComposition()
        {
            if (App.BodyComposition is null) return;

            Weight = App.BodyComposition.Weight;
            BMI = App.BodyComposition.BMI;
            BoneMass = App.BodyComposition.BoneMass;
            MuscleMass = App.BodyComposition.MuscleMass;
            IdealWeight = App.BodyComposition.IdealWeight;
            BMR = App.BodyComposition.BMR;
            MetabolicAge = App.BodyComposition.MetabolicAge;
            ProteinPercentage = App.BodyComposition.ProteinPercentage;
            VisceralFat = App.BodyComposition.VisceralFat;
            Fat = App.BodyComposition.Fat;
            WaterPercentage = App.BodyComposition.WaterPercentage;
            BodyType = App.BodyComposition.BodyType;
            IsAutomaticCalculation = true;
        }

        public Command UploadCommand { get; }

        private double? _weight;

        public double? Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        private double? _bmi;

        public double? BMI
        {
            get => _bmi;
            set => SetProperty(ref _bmi, value);
        }

        private double? _idealWeight;

        public double? IdealWeight
        {
            get => _idealWeight;
            set => SetProperty(ref _idealWeight, value);
        }

        private double? _metabolicAge;

        public double? MetabolicAge
        {
            get => _metabolicAge;
            set => SetProperty(ref _metabolicAge, value);
        }

        private double? _proteinPercentage;

        public double? ProteinPercentage
        {
            get => _proteinPercentage;
            set => SetProperty(ref _proteinPercentage, value);
        }

        private double? _bmr;

        public double? BMR
        {
            get => _bmr;
            set => SetProperty(ref _bmr, value);
        }

        private double? _fat;

        public double? Fat
        {
            get => _fat;
            set => SetProperty(ref _fat, value);
        }

        private double? _muscleMass;

        public double? MuscleMass
        {
            get => _muscleMass;
            set => SetProperty(ref _muscleMass, value);
        }

        private double? _boneMass;

        public double? BoneMass
        {
            get => _boneMass;
            set => SetProperty(ref _boneMass, value);
        }

        private double? _visceralFat;

        public double? VisceralFat
        {
            get => _visceralFat;
            set => SetProperty(ref _visceralFat, value);
        }

        private int? _bodyType;

        public int? BodyType
        {
            get => _bodyType;
            set => SetProperty(ref _bodyType, value);
        }

        private double? _waterPercentage;

        public double? WaterPercentage
        {
            get => _waterPercentage;
            set => SetProperty(ref _waterPercentage, value);
        }

        private string _email;

        private string _password;

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