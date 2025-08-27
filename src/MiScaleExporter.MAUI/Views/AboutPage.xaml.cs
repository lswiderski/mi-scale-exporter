using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MiScaleExporter.MAUI;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = scope.Resolve<AboutViewModel>();
            }
        }

        protected override void OnAppearing()
        {
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
        }
    }
}