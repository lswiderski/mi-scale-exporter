using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MiScaleExporter.MAUI;
using MiScaleExporter.MAUI.ViewModels;

namespace MiScaleExporter.MAUI.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingPage : ContentPage
    {
        private ISettingsViewModel vm;
        public SettingPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = vm = scope.Resolve<ISettingsViewModel>();
            }
        }

        private void SexRadioSetToMale(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                vm.SexRadioSetToMale();
            }
        }
        private void SexRadioSetToFemale(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                vm.SexRadioSetToFemale();
            }
        }
        private void ScaleTypeSetToBodyCompositionScale(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                vm.ScaleTypeSetToBodyCompositionScale();
            }

        }
        private void ScaleTypeSetToMiscale(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                vm.ScaleTypeSetToMiscale();
            }

        }
        private void ScaleTypeSetToS400(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                vm.ScaleTypeSetToS400();
            }

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await vm.LoadPreferencesAsync();
        }
    }
}