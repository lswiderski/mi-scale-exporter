using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using MiScaleExporter.Core.History;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using MiScaleExporter.MAUI.ViewModels;
using IContainer = Autofac.IContainer;
using System.Globalization;
using CommunityToolkit.Maui.Storage;

namespace MiScaleExporter.MAUI
{
    public partial class App : Application
    {
        public static IContainer Container;
        public static BodyComposition BodyComposition;
        public static bool IsGuestMeasurement { get; set; }
        public static User LastGuestUser { get; set; }
        public App()
        {
            CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture;
            InitializeComponent();

        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // Workaround for: 'Either set MainPage or override CreateWindow.'??
            if (this.MainPage == null)
            {
                AutofacInit();
                this.MainPage = BuildRootPage();
            }

            return base.CreateWindow(activationState);
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (MainPage == null)
            {
                AutofacInit();
                MainPage = BuildRootPage();
            }
        }

        private Page BuildRootPage()
        {
            var done = Preferences.Get(PreferencesKeys.OnboardingCompleted, false)
                       || !string.IsNullOrWhiteSpace(Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty));
            return done ? (Page)new AppShell() : new NavigationPage(new Views.OnboardingPage());
        }

        public static void CompleteOnboardingAndGoHome()
        {
            Preferences.Set(PreferencesKeys.OnboardingCompleted, true);
            if (Current is App app)
            {
                app.MainPage = new AppShell();
            }
        }

        protected void AutofacInit()
        {
            // Initialize Autofac builder
            var builder = new ContainerBuilder();

            // Register services
            builder.RegisterInstance<IMeasurementHistoryStore>(
                    new JsonMeasurementHistoryStore(
                        Path.Combine(FileSystem.AppDataDirectory, "s400-measurements.json")))
                .SingleInstance();
            builder.RegisterType<Scale>().As<IScale>().InstancePerLifetimeScope();
            builder.RegisterType<DataInterpreter>().As<IDataInterpreter>().InstancePerLifetimeScope();
            builder.RegisterType<GarminService>().As<IGarminService>().InstancePerLifetimeScope();
            builder.RegisterType<GarminAuthService>().As<IGarminAuthService>().InstancePerLifetimeScope();
            builder.RegisterType<ScaleViewModel>().As<IScaleViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<FormViewModel>().As<IFormViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<LogService>().As<ILogService>().SingleInstance();
            builder.RegisterType<SettingsViewModel>().As<ISettingsViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<AboutViewModel>().AsSelf();
            builder.RegisterType<OnboardingViewModel>().AsSelf();
            builder.RegisterInstance<IFileSaver>(FileSaver.Default).SingleInstance();

            builder.RegisterType<ScaleCapabilities>().As<IScaleCapabilities>().SingleInstance();
            builder.RegisterType<MetricEvaluator>().As<IMetricEvaluator>().SingleInstance();
            builder.RegisterType<MetricPresenter>().As<IMetricPresenter>().SingleInstance();
            builder.RegisterType<MockBodyCompositionProvider>().As<IMockBodyCompositionProvider>().SingleInstance();
            builder.RegisterType<ResultViewModel>().As<IResultViewModel>().InstancePerDependency();

            App.Container = builder.Build();
            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(Container));
        }
    }
}