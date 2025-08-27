
using Autofac;

using MiScaleExporter.MAUI.ViewModels;
using MiScaleExporter.Models;

namespace MiScaleExporter.MAUI.Views
{
    [QueryProperty(nameof(AutoUpload), "autoUpload")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FormPage : ContentPage
    {
        private IFormViewModel vm;
        public FormPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IFormViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            this.adMobBanner.IsVisible = !Preferences.Get(PreferencesKeys.HideAds, false);
            await vm.LoadPreferencesAsync();
            vm.LoadBodyComposition();
        }

        public bool AutoUpload
        {
            set
            {
                if (value)
                {
                    vm.AutoUpload();
                }
            }
        }
      
    }
}