using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class XiaomiMeasurementItem
    {
        public DateTime Date { get; set; }
        public double Weight { get; set; }
        public string WeightDisplay => Weight.ToString("0.##");
        public Weight WeightDto { get; set; }
    }

    public class XiaomiViewModel : BaseViewModel
    {
        private readonly XiaomiService _xiaomiService;

        public XiaomiViewModel(XiaomiService xiaomiService)
        {
            _xiaomiService = xiaomiService;
            Title = "Xiaomi";
            Measurements = new ObservableCollection<XiaomiMeasurementItem>();
            DownloadCommand = new Command(async () => await DownloadAsync());
            OpenSettingsCommand = new Command(() => Shell.Current.GoToAsync("Settings"));
            ShowDetailsCommand = new Command<XiaomiMeasurementItem>(async item => await ShowDetails(item));
            SelectCommand = new Command<XiaomiMeasurementItem>(async item => await SelectMeasurement(item));
        }

        public ObservableCollection<XiaomiMeasurementItem> Measurements { get; }

        public ICommand DownloadCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        public ICommand SelectCommand { get; }

        public bool HasPreferencesSet =>
            !string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.XiaomiUserId, string.Empty))
            && !string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.XiaomiPassToken, string.Empty))
            && !string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.XiaomiAccountRegion, string.Empty))
            && !string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.XiaomiScaleModel, string.Empty));

        private async Task DownloadAsync()
        {
            if (!HasPreferencesSet)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Xiaomi preferences are not set. Please configure them in Settings.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                Measurements.Clear();
                var weights = await _xiaomiService.GetModelWeightsFromCloudAsync();

                if (weights == null || weights.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No weights returned from Xiaomi cloud.", "OK");
                    return;
                }

                foreach (var w in weights)
                {
                    Measurements.Add(new XiaomiMeasurementItem
                    {
                        Date = w.Date,
                        Weight = w.WeightKg,
                        WeightDto = w
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowDetails(XiaomiMeasurementItem item)
        {
            if (item == null) return;
            // show detailed JSON in alert
            var json = JsonConvert.SerializeObject(item.WeightDto, Formatting.Indented);
            await Application.Current.MainPage.DisplayAlert("Details", json, "OK");
        }

        private async Task SelectMeasurement(XiaomiMeasurementItem item)
        {
            if (item == null) return;

            var w = item.WeightDto;

            var bc = new BodyComposition
            {
                Weight = w.WeightKg,
                Fat = w.BodyFat,
                MuscleMass = w.MuscleMass,
                BoneMass = w.BoneMass,
                WaterPercentage = w.BodyWater,
                VisceralFat = w.VisceralFat,
                BMI = w.BMI,
                MetabolicAge = w.MetabolicAge
            };

            App.BodyComposition = bc;

            // navigate to form page
            await Shell.Current.GoToAsync("//FormPage");
        }
    }
}
