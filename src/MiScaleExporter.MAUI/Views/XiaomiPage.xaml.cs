using Autofac;
using Microsoft.Maui.Controls;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    public partial class XiaomiPage : ContentPage
    {
        public XiaomiPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = scope.Resolve<XiaomiViewModel>();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
        }
    }
}
