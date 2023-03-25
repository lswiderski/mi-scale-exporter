using System.Windows.Input;
 
 

namespace MiScaleExporter.MAUI.ViewModels;

public class AboutViewModel : BaseViewModel
{
    public AboutViewModel()
    {
        this.Title = "Mi Scale Exporter";
        GoToScanCommand = new Command(async () =>
            await Shell.Current.GoToAsync("///ScalePage"));
        OpenGithubCommand = new Command(async () =>
            await Browser.OpenAsync("https://github.com/lswiderski/mi-scale-exporter"));
        OpenCoffeeCommand = new Command(async () =>
            await Browser.OpenAsync("https://www.buymeacoffee.com/lukaszswiderski"));
        OpenHelpCommand = new Command(async () =>
         await Shell.Current.GoToAsync("///HelpPage"));
    }
    
    public ICommand GoToScanCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenCoffeeCommand { get; }
    public ICommand OpenHelpCommand { get; }
}