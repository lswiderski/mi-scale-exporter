using System.Windows.Input;

namespace MiScaleExporter.MAUI.Views;

public partial class HelpPage : ContentPage
{
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenCoffeeCommand { get; }
    public ICommand S400HelpCommand { get; } = new Command(async () =>
        await Launcher.OpenAsync("https://lswiderski.github.io/mi-scale-exporter/#steps-to-connect-xiaomi-body-composition-scale-s400"));

    public HelpPage()
	{

        this.BindingContext = this;
        OpenGithubCommand = new Command(async () =>
         await Browser.OpenAsync("https://github.com/lswiderski/mi-scale-exporter"));
        OpenCoffeeCommand = new Command(async () =>
            await Browser.OpenAsync("https://www.buymeacoffee.com/lukaszswiderski"));

        InitializeComponent();
    }
}