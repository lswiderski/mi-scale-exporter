using MiScaleExporter.Models;
using MiScaleExporter.Services;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace MiScaleExporter.ViewModels
{
    public class ScaleViewModel : BaseViewModel, IScaleViewModel
    {
        private readonly IScaleService _scaleService;
        
        public ScaleViewModel(IScaleService scaleService)
        {
            _scaleService = scaleService;
            this.Sex = Sex.Male;
            CancelCommand = new Command(OnCancel);
            
            Title = "Mi Scale Data";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://github.com/lswiderski/MiScaleExporter-mobile"));
            ScanCommand = new Command(async () =>
            {

                Scale scale = new Scale()
                {
                    Address = Address,
                };
                ScanningLabel = "Scanning";
                var bc = await _scaleService.GetBodyCompositonAsync(scale, new User { Sex = _sex, Age = _age, Height = _height});

                if (bc.IsValid)
                {
                    ScanningLabel = "";
                }
                else
                {
                    ScanningLabel = "Not found";
                }

                App.BodyComposition = bc;
                await Shell.Current.GoToAsync("///FormPage");
            });
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
            set => SetProperty(ref _sex , value);
        }

        private string _scanningLabel;
        public string ScanningLabel
        {
            get => _scanningLabel;
            set => SetProperty(ref _scanningLabel, value);
        }

    }
}