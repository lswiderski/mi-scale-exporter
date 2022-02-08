using miscale2garmin.ViewModels;
using miscale2garmin.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace miscale2garmin
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
