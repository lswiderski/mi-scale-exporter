
using Autofac;
using MiScaleExporter.Models;
using MiScaleExporter.MAUI.ViewModels;


namespace MiScaleExporter.MAUI.Views
{
    public partial class ScalePage : ContentPage
    {
        public Scale Item { get; set; }
        private IScaleViewModel vm;
        public ScalePage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<IScaleViewModel>();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await vm.CheckPreferencesAsync();
        }

    }
}