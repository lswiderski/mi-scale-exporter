
using Autofac;
using MiScaleExporter.Models;
using MiScaleExporter.MAUI.ViewModels;


namespace MiScaleExporter.MAUI.Views
{
    public partial class ScalePage : ContentPage
    {
        private IScaleViewModel vm;
        public ScalePage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IScaleViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
            await vm.CheckPreferencesAsync();
        }

    }
}