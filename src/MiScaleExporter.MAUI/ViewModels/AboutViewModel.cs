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
        GoToXiaomiCloudCommand = new Command(async () =>
          await Shell.Current.GoToAsync("///XiaomiPage"));
        OpenGithubCommand = new Command(async () =>
            await Browser.OpenAsync("https://github.com/lswiderski/mi-scale-exporter"));
        OpenCoffeeCommand = new Command(async () =>
            await Browser.OpenAsync("https://www.buymeacoffee.com/lukaszswiderski"));
        OpenHelpCommand = new Command(async () =>
         await Shell.Current.GoToAsync("///HelpPage"));
    }
    
    public ICommand GoToScanCommand { get; }
    public ICommand GoToXiaomiCloudCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenCoffeeCommand { get; }
    public ICommand OpenHelpCommand { get; }

    public void RefreshPreferences()
    {
        this.ScaleType = (ScaleType)Preferences.Get(PreferencesKeys.ScaleType, (byte)ScaleType.MiBodyCompositionScale);
    }

    private ScaleType _scaleType;

    public ScaleType ScaleType
    {
        get => _scaleType;
        set
        {
            if (_scaleType != value)
            {
                SetProperty(ref _scaleType, value);
               
                // Notify dependent properties
                OnPropertyChanged(nameof(IsXiaomiHomeSelected));
                OnPropertyChanged(nameof(IsXiaomiHomeNotSelected));
            }

        }
    }

    public bool IsXiaomiHomeSelected
    {
        get => _scaleType == ScaleType.XiaomiHome;
    }
    public bool IsXiaomiHomeNotSelected
    {
        get => _scaleType != ScaleType.XiaomiHome;
    }
}