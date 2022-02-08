using miscale2garmin.Services;
using miscale2garmin.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace miscale2garmin
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<ScaleService>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
