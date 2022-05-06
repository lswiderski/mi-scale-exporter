using System;
using System.Windows.Input;
using MiScaleExporter.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MiScaleExporter.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel()
        {
            this.Title = "Settings";
            this._apiAddress = Preferences.Get(PreferencesKeys.ApiServerAddressOverride, string.Empty);
            this._saveToStorage = Preferences.Get(PreferencesKeys.SaveToStorage,false);
            ResetCommand = new Command(() =>
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                    this.ApiAddress = string.Empty;
                    Preferences.Remove(PreferencesKeys.SaveToStorage);
                }
            );
        }

        public ICommand ResetCommand { get; }
        private string _apiAddress;

        public string ApiAddress
        {
            get => _apiAddress;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                }
                else if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    {
                        Preferences.Set(PreferencesKeys.ApiServerAddressOverride, value);
                    }
                }
                SetProperty(ref _apiAddress, value);
            }
        }

        private bool _saveToStorage;

        public bool SaveToStorage
        {
            get => _saveToStorage;
            set {
                Preferences.Set(PreferencesKeys.SaveToStorage, value);
                SetProperty(ref _saveToStorage, value);
            } 
        }
    }
}