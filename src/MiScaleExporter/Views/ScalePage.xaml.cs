
using Autofac;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using MiScaleExporter.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MiScaleExporter.Views
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

        private void SexRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            vm.SexRadioButtonChanged(sender, e);
        }
        private void ScaleTypeRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            vm.ScaleTypeRadioButton_Changed(sender, e);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            vm.CheckPreferences();
        }

    }
}