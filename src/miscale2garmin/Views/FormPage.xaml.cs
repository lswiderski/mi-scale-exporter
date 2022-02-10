using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using miscale2garmin.Models;
using miscale2garmin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace miscale2garmin.Views
{
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

            vm.LoadBodyComposition();
        }
      
    }
}