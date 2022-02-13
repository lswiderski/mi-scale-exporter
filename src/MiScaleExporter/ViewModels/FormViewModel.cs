using System;
using Xamarin.Forms;

namespace MiScaleExporter.ViewModels
{
    public class FormViewModel: BaseViewModel, IFormViewModel
    {

        public FormViewModel()
        {
            Title = "Garmin Body Composition Form";
            Date = DateTime.Now;
            Time = DateTime.Now.TimeOfDay;
            SaveCommand = new Command(OnSave, ValidateSave);
            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();
        }
        private bool ValidateSave()
        {
            return true;// return !String.IsNullOrWhiteSpace(weight);
        }
        
        private async void OnSave()
        {
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
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

        public Command SaveCommand { get; }
        
        private double? weight;
        public double? Weight
        {
            get => weight;
            set => SetProperty(ref weight, value);
        }
        
        private double? bmi;
        public double? BMI
        {
            get => bmi;
            set => SetProperty(ref bmi, value);
        }
        
        private double? idealWeight;
        public double? IdealWeight
        {
            get => idealWeight;
            set => SetProperty(ref idealWeight, value);
        }
        
        private double? metabolicAge;
        public double? MetabolicAge
        {
            get => metabolicAge;
            set => SetProperty(ref metabolicAge, value);
        }
        
        private double? proteinPercentage;
        public double? ProteinPercentage
        {
            get => proteinPercentage;
            set => SetProperty(ref proteinPercentage, value);
        }
        
        private double? bmr;
        public double? BMR
        {
            get => bmr;
            set => SetProperty(ref bmr, value);
        }
        
        private double? fat;
        public double? Fat
        {
            get => fat;
            set => SetProperty(ref fat, value);
        }
        
        private double? muscleMass;
        public double? MuscleMass
        {
            get => muscleMass;
            set => SetProperty(ref muscleMass, value);
        }
        
        private double? boneMass;
        public double? BoneMass
        {
            get => boneMass;
            set => SetProperty(ref boneMass, value);
        }
        
        private double? visceralFat;
        public double? VisceralFat
        {
            get => visceralFat;
            set => SetProperty(ref visceralFat, value);
        }
        
        private double? bodyType;
        public double? BodyType
        {
            get => bodyType;
            set => SetProperty(ref bodyType, value);
        }
        
        private double? waterPercentage;
        public double? WaterPercentage
        {
            get => waterPercentage;
            set => SetProperty(ref waterPercentage, value);
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
        
    }
}