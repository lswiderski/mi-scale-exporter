
using Autofac;

using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    [QueryProperty(nameof(AutoUpload), "autoUpload")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FormPage : ContentPage
    {
        private IFormViewModel vm;
        public FormPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IFormViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Block guest data from ever entering FormPage via the flyout drawer. Clear the
            // global BC and bounce back to ScalePage so the guest's measurement can never be
            // uploaded to the owner's Garmin account.
            if (App.IsGuestMeasurement)
            {
                App.BodyComposition = null;
                await Shell.Current.GoToAsync("//ScalePage");
                return;
            }
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
            await vm.LoadPreferencesAsync();
            vm.LoadBodyComposition();
        }

        public bool AutoUpload
        {
            set
            {
                if (value)
                {
                    vm.AutoUpload();
                }
            }
        }
      
    }
}