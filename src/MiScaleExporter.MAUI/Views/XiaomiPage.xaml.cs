using Autofac;
using MiScaleExporter.MAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace MiScaleExporter.MAUI.Views
{
    public partial class XiaomiPage : ContentPage
    {
        public XiaomiPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = scope.Resolve<XiaomiViewModel>();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // nothing for now
        }
    }
}
