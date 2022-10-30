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

        private void SexRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            vm.SexRadioButtonChanged(sender, e);
        }
        private void ScaleTypeRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            vm.ScaleTypeRadioButton_Changed(sender, e);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await vm.LoadPreferencesAsync();
        }
    }
}