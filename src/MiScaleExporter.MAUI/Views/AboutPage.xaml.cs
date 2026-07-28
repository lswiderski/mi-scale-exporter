using Autofac;
using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;
#if ANDROID
using Plugin.AdMob;
#endif

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        private readonly AboutViewModel vm;
#if ANDROID
        private BannerAd _bannerAd;
#endif

        public AboutPage()
        {
            InitializeComponent();
            this.BindingContext = vm = App.Container.Resolve<AboutViewModel>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            vm.RefreshStatus();
            InjectAdBanner();
        }

        private void InjectAdBanner()
        {
#if ANDROID
            if (Preferences.Get(PreferencesKeys.HideAds, false))
            {
                if (_bannerAd != null && AdContainer.Contains(_bannerAd))
                {
                    AdContainer.Remove(_bannerAd);
                }
                return;
            }

            if (_bannerAd != null)
            {
                return;
            }

            _bannerAd = new BannerAd
            {
                AdUnitId = "ca-app-pub-1938975042085430/4160336701",
                HorizontalOptions = LayoutOptions.Center,
            };
            AdContainer.Add(_bannerAd);
#endif
        }
    }
}
