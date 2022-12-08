using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using MiScaleExporter.MAUI.ViewModels;
using IContainer = Autofac.IContainer;
using MiScaleExporter.Droid;

namespace MiScaleExporter.MAUI
{
    public partial class App : Application
    {
        public static IContainer Container;
        public static BodyComposition BodyComposition;
        public App()
        {
            InitializeComponent();

        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // Workaround for: 'Either set MainPage or override CreateWindow.'??
            if (this.MainPage == null)
            {
                AutofacInit();
                this.MainPage = new AppShell();
            }

            return base.CreateWindow(activationState);
        }

        protected override void OnStart()
        {
            base.OnStart();
            AutofacInit();
            MainPage = new AppShell();
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
            builder.RegisterType<SettingsViewModel>().As<ISettingsViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<AboutViewModel>().AsSelf();

            App.Container = builder.Build();
            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(Container));
        }
    }
}