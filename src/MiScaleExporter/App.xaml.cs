using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using MiScaleExporter.Services;
using MiScaleExporter.ViewModels;
using MiScaleExporter.Views;
using System;
using System.Reflection;
using MiScaleExporter.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace MiScaleExporter
{
    public partial class App : Application
    {
        public static IContainer Container;
        public static BodyComposition BodyComposition;
        private Assembly _assembly;
        private string _assemblyName;
        public App(Assembly assembly, string assemblyName)
        {
            _assembly = assembly;
            _assemblyName = assemblyName;
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
            builder.RegisterType<GarminService>().As<IGarminService>().InstancePerLifetimeScope();
            builder.RegisterType<ScaleViewModel>().As<IScaleViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<FormViewModel>().As<IFormViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<LogService>().As<ILogService>().SingleInstance();
            builder.RegisterType<SettingsViewModel>().AsSelf();
            App.Container = builder.Build();
            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(Container));
        }
        
    }
}
