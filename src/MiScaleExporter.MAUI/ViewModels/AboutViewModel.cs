using MiScaleExporter.MAUI.Resources.Localization;
using MiScaleExporter.Models;
using System.Windows.Input;
 
 

namespace MiScaleExporter.MAUI.ViewModels;

public class AboutViewModel : BaseViewModel
{
    public AboutViewModel()
    {
        this.Title = AppSnippets.MiScaleExporter;
        GoToScanCommand = new Command(async () =>
            await Shell.Current.GoToAsync("///ScalePage"));
        OpenGithubCommand = new Command(async () =>
            await Browser.OpenAsync("https://github.com/lswiderski/mi-scale-exporter"));
        OpenCoffeeCommand = new Command(async () =>
            await Browser.OpenAsync("https://www.buymeacoffee.com/lukaszswiderski"));
        OpenHelpCommand = new Command(async () =>
         await Shell.Current.GoToAsync("///HelpPage"));
        GoToSettingsCommand = new Command(async () =>
            await Shell.Current.GoToAsync("///Settings"));
        RerunSetupCommand = new Command(() =>
        {
            Preferences.Set(PreferencesKeys.OnboardingCompleted, false);
            if (Application.Current is App)
            {
                Application.Current.MainPage = new NavigationPage(new Views.OnboardingPage());
            }
        });
        RefreshStatus();
    }
    
    public ICommand GoToScanCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenCoffeeCommand { get; }
    public ICommand OpenHelpCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand RerunSetupCommand { get; }

    private bool _profileConfigured;
    public bool ProfileConfigured
    {
        get => _profileConfigured;
        set => SetProperty(ref _profileConfigured, value);
    }

    private bool _scaleConfigured;
    public bool ScaleConfigured
    {
        get => _scaleConfigured;
        set => SetProperty(ref _scaleConfigured, value);
    }

    private bool _garminConfigured;
    public bool GarminConfigured
    {
        get => _garminConfigured;
        set => SetProperty(ref _garminConfigured, value);
    }

    private string _profileStatus;
    public string ProfileStatus
    {
        get => _profileStatus;
        set => SetProperty(ref _profileStatus, value);
    }

    private string _scaleStatus;
    public string ScaleStatus
    {
        get => _scaleStatus;
        set => SetProperty(ref _scaleStatus, value);
    }

    private string _garminStatus;
    public string GarminStatus
    {
        get => _garminStatus;
        set => SetProperty(ref _garminStatus, value);
    }

    public void RefreshStatus()
    {
        var height = Preferences.Get(PreferencesKeys.UserHeight, 0);
        var age = Preferences.Get(PreferencesKeys.UserAge, 0);
        var birthMode = Preferences.Get(PreferencesKeys.UseBirthDateMode, false);
        ProfileConfigured = height > 0 && (age > 0 || birthMode);

        ScaleConfigured = !string.IsNullOrWhiteSpace(
            Preferences.Get(PreferencesKeys.MiScaleBluetoothAddress, string.Empty));

        GarminConfigured = !string.IsNullOrWhiteSpace(
            Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty));

        ProfileStatus = ProfileConfigured ? AppSnippets.HomeConfigured : AppSnippets.HomeSetUp;
        ScaleStatus = ScaleConfigured ? AppSnippets.HomeConfigured : AppSnippets.HomeSetUp;
        GarminStatus = GarminConfigured ? AppSnippets.HomeConfigured : AppSnippets.HomeSetUp;
    }
}