using Autofac;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IntervalsFormPage : ContentPage
    {
        private IIntervalsFormViewModel vm;

        public IntervalsFormPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IIntervalsFormViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
            await vm.LoadPreferencesAsync();
            vm.LoadBodyComposition();
        }
    }
}
