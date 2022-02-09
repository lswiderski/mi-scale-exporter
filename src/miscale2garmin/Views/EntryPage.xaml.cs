
using Autofac;
using miscale2garmin.Models;
using miscale2garmin.Services;
using miscale2garmin.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace miscale2garmin.Views
{
    public partial class EntryPage : ContentPage
    {
        public Scale Item { get; set; }
        public EntryPage()
        {
            InitializeComponent();
            using (var scope = App.Container.BeginLifetimeScope())
            {
                this.BindingContext = scope.Resolve<IEntryViewModel>();
            }
        }
    }
}