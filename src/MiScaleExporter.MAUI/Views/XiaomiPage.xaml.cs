using Autofac;
using Microsoft.Maui.Controls;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    public partial class XiaomiPage : ContentPage
    {
        private XiaomiViewModel vm;
        public XiaomiPage()
        {
            InitializeComponent();

            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<XiaomiViewModel>();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.vm.RefreshPreferences();
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
        }
    }
}
