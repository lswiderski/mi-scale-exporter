using MiScaleExporter.Models;
using MiScaleExporter.Services;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace MiScaleExporter.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private readonly IScaleService _scaleService;

        public ScaleViewModel(IScaleService scaleService)
        {
            _scaleService = scaleService;
            this.LoadPreferences();
            
            Title = "Mi Scale Data";

            CancelCommand = new Command(OnCancel);
            OpenWebCommand = new Command(async () =>
                await Browser.OpenAsync("https://github.com/lswiderski/MiScaleExporter-mobile"));
            ScanCommand = new Command(async () =>
            {
                this.SavePrefences();
                Scale scale = new Scale()
                {
                    Address = Address,
                };
                ScanningLabel = "Scanning";
                var bc = await _scaleService.GetBodyCompositonAsync(scale,
                    new User {Sex = _sex, Age = _age, Height = _height});

                if (bc is null || !bc.IsValid)
                {
                    await Application.Current.MainPage.DisplayAlert("Problem", "Data could not be obtained. try again",
                        "OK");
                    ScanningLabel = "Not found";
                }
                else
                {
                    App.BodyComposition = bc;
                    await Shell.Current.GoToAsync("///FormPage");
                }
            });
        }

        private void LoadPreferences()
        {
            this._age = Preferences.Get(PreferencesKeys.UserAge, 25);
            this._height = Preferences.Get(PreferencesKeys.UserHeight, 170);
            this._sex = (Sex) Preferences.Get(PreferencesKeys.UserSex, (byte) Sex.Male);
            this._address = Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty);
        }

        private void SavePrefences()
        {
            Preferences.Set(PreferencesKeys.UserAge, _age);
            Preferences.Set(PreferencesKeys.UserHeight, _height);
            Preferences.Set(PreferencesKeys.UserSex, (byte) _sex);
            Preferences.Set(PreferencesKeys.MiScaleBluetoothAddress, _address);
        }

        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(_address);
        }

        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await _scaleService.CancelSearchAsync();
        }


        public void SexRadioButtonChanged(object s, CheckedChangedEventArgs e)
        {
            var radio = s as RadioButton;
            this.Sex = radio.Value as string == "1" ? Models.Sex.Male : Models.Sex.Female;
        }


        public ICommand OpenWebCommand { get; }

        public ICommand ScanCommand { get; }

        private string _address;

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
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
                }
            }
        }

        private Sex _sex;

        public Sex Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }

        private string _scanningLabel;

        public string ScanningLabel
        {
            get => _scanningLabel;
            set => SetProperty(ref _scanningLabel, value);
        }
    }
}