using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using miscale2garmin.Services;
using miscale2garmin.ViewModels;
using miscale2garmin.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace miscale2garmin
{
    public partial class App : Application
    {
        public static IContainer Container;
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStart()
        {
            base.OnStart();
            AutofacInit();
            MainPage = new AppShell();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        protected void AutofacInit()
        {
            // Initialize Autofac builder
            var builder = new ContainerBuilder();

            // Register services
            builder.RegisterType<ScaleService>().As<IScaleService>().InstancePerLifetimeScope();
            builder.RegisterType<MetricsService>().As<IMetricsService>().InstancePerLifetimeScope();
            builder.RegisterType<EntryViewModel>().As<IEntryViewModel>().InstancePerLifetimeScope();
            App.Container = builder.Build();
            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(Container));
        }
    }
}
