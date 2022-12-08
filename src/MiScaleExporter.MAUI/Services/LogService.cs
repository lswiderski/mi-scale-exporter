namespace MiScaleExporter.Services;

public class LogService : ILogService
{
    public LogService()
    {
    }

    public void LogDebug(string message)
    {
    }

    public void LogError(string message)
    {
        Application.Current.MainPage.DisplayAlert("Error", message,
                  "OK");
    }

    public void LogFatal(string message)
    {

        Application.Current.MainPage.DisplayAlert("Fatal", message,
                "OK");
    }

    public void LogInfo(string message)
    {
        Application.Current.MainPage.DisplayAlert("Info", message,
                "OK");
    }

    public void LogWarning(string message)
    {

        Application.Current.MainPage.DisplayAlert("Warning", message,
                "OK");
    }
}