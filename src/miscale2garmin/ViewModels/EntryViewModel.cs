using miscale2garmin.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace miscale2garmin.ViewModels
{
    public class EntryViewModel : BaseViewModel
    {
        
        public EntryViewModel()
        {
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();


            Title = "Form";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://github.com/lswiderski/miscale2garmin-mobile"));
            ScanCommand = new Command(async () =>
            {

                Scale scale = new Scale()
                {
                    Address = Address,
                };
               var bc = await ScaleService.GetBodyCompositonAsync(scale);

                Weight = bc.Weight;
                OnPropertyChanged(nameof(Weight));

            });
        }

        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(address);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await ScaleService.CancelSearchAsync();
        }

        private async void OnSave()
        {

            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }


        public ICommand OpenWebCommand { get; }

        public ICommand ScanCommand { get; }

        private string address;
        public string Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        private double weight;
        public double Weight
        {
            get => weight;
            set => SetProperty(ref weight, value);
        }
    }
}