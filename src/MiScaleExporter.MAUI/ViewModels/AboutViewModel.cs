using System.Windows.Input;
 
 

namespace MiScaleExporter.MAUI.ViewModels;

public class AboutViewModel : BaseViewModel
{
    public AboutViewModel()
    {
        this.Title = "About";
        GoToScanCommand = new Command(async () =>
            await Shell.Current.GoToAsync("///ScalePage"));
        OpenGithubCommand = new Command(async () =>
            await Browser.OpenAsync("https://github.com/lswiderski/mi-scale-exporter"));
        OpenCoffeeCommand = new Command(async () =>
            await Browser.OpenAsync("https://www.buymeacoffee.com/lukaszswiderski"));
    }
    
    public ICommand GoToScanCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenCoffeeCommand { get; }
}