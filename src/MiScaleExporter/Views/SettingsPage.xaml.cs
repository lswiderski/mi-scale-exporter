using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MiScaleExporter.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MiScaleExporter.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingPage : ContentPage
    {
        public SettingPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = scope.Resolve<SettingsViewModel>();
            }
        }
    }
}