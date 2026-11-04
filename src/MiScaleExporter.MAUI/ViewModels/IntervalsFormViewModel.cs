using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.MAUI.Utils;
using MiScaleExporter.Models;
using MiScaleExporter.Services;

namespace MiScaleExporter.MAUI.ViewModels
{
    public class IntervalsFormViewModel : BaseViewModel, IIntervalsFormViewModel
    {
        private readonly IIntervalsService _intervalsService;
        private const double KgToLbsConversion = 2.20462;

        public IntervalsFormViewModel(IIntervalsService intervalsService)
        {
            _intervalsService = intervalsService;
            Title = AppSnippets.IntervalsBodyCompositionForm;
            Date = DateTime.Now;
            _showWeightInKg = true;
            UploadCommand = new Command(async () => await OnUpload());
        }

        public Command UploadCommand { get; }

        public async Task LoadPreferencesAsync()
        {
            this._displayWeightInLbs = Preferences.Get(PreferencesKeys.DisplayWeightInLbs, false);
            this.ShowWeightInKg = !_displayWeightInLbs;
            this.ShowWeightInLbs = _displayWeightInLbs;

            this.Date = DateTime.Now;
            await Task.CompletedTask;
        }

        public void LoadBodyComposition()
        {
            if (App.BodyComposition is null) return;

            Weight = ConvertFromKg(App.BodyComposition.Weight).ToString("0.##");
            Fat = App.BodyComposition.Fat.ToString();
            Date = App.BodyComposition.Date.Date;
        }

        private async Task OnUpload()
        {
            if (IsBusyForm)
            {
                return;
            }

            this.IsBusyForm = true;
            try
            {
                var weight = DoubleValueParser.ParseValueFromUsersCulture(_weight) ?? 0;
                var fat = DoubleValueParser.ParseValueFromUsersCulture(_fat) ?? 0;

                var response = await Task.Run(() =>
                    this._intervalsService.UploadAsync(Date.Date, ConvertToKg(weight), fat));

                var message = (response?.IsSuccess ?? false) ? AppSnippets.Uploaded : response?.Message;
                await Application.Current.MainPage.DisplayAlert(AppSnippets.Response, message, AppSnippets.OK);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(AppSnippets.Response, ex.Message, AppSnippets.OK);
            }
            finally
            {
                this.IsBusyForm = false;
            }
        }

        private double ConvertFromKg(double valueInKg)
        {
            return _displayWeightInLbs ? valueInKg * KgToLbsConversion : valueInKg;
        }

        private double ConvertToKg(double displayValue)
        {
            return _displayWeightInLbs ? displayValue / KgToLbsConversion : displayValue;
        }

        private string _weight;

        public string Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, DoubleValueParser.CheckValue(value));
        }

        private string _fat;

        public string Fat
        {
            get => _fat;
            set => SetProperty(ref _fat, DoubleValueParser.CheckValue(value));
        }

        private DateTime _date;

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private bool _isBusyForm;

        public bool IsBusyForm
        {
            get => _isBusyForm;
            set => SetProperty(ref _isBusyForm, value);
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

        private bool _displayWeightInLbs;
    }
}
