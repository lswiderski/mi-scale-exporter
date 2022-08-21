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
            this._showReceivedByteArray = Preferences.Get(PreferencesKeys.ShowReceivedByteArray, false);
            this._saveToStorage = Preferences.Get(PreferencesKeys.SaveToStorage,false);
            this._autoScan = Preferences.Get(PreferencesKeys.AutoScan, false);
            this._oneClickScanAndUpload = Preferences.Get(PreferencesKeys.OneClickScanAndUpload, false);
            this._showUnstabilizedData = Preferences.Get(PreferencesKeys.ShowUnstabilizedData, false);
            ResetCommand = new Command(() =>
                {
                    Preferences.Remove(PreferencesKeys.ApiServerAddressOverride);
                    this.ApiAddress = string.Empty;
                    Preferences.Remove(PreferencesKeys.ShowReceivedByteArray);
                    Preferences.Remove(PreferencesKeys.SaveToStorage);
                    Preferences.Remove(PreferencesKeys.AutoScan);
                    Preferences.Remove(PreferencesKeys.OneClickScanAndUpload);
                    Preferences.Remove(PreferencesKeys.ShowUnstabilizedData);
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

        private bool _showReceivedByteArray;

        public bool ShowReceivedByteArray
        {
            get => _showReceivedByteArray;
            set
            {
                Preferences.Set(PreferencesKeys.ShowReceivedByteArray, value);
                SetProperty(ref _showReceivedByteArray, value);
            }
        }

        private bool _autoScan;

        public bool AutoScan
        {
            get => _autoScan;
            set
            {
                Preferences.Set(PreferencesKeys.AutoScan, value);
                SetProperty(ref _autoScan, value);
            }
        }

        private bool _oneClickScanAndUpload;

        public bool OneClickScanAndUpload
        {
            get => _oneClickScanAndUpload;
            set
            {
                Preferences.Set(PreferencesKeys.OneClickScanAndUpload, value);
                SetProperty(ref _oneClickScanAndUpload, value);
            }
        }

        private bool _showUnstabilizedData;

        public bool ShowUnstabilizedData
        {
            get => _showUnstabilizedData;
            set
            {
                Preferences.Set(PreferencesKeys.ShowUnstabilizedData, value);
                SetProperty(ref _showUnstabilizedData, value);
            }
        }

    }
}