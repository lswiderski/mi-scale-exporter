using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xamarin.Forms;

namespace MiScaleExporter.Services;

public class LogService : ILogService
{
    private readonly ILogger logger;
    public LogService(ILogger logger)
    {
        this.logger = logger;
    }

    public void LogDebug(string message)
    {
        this.logger.Info(message);
    }

    public void LogError(string message)
    {
        Application.Current.MainPage.DisplayAlert("Error", message,
                  "OK");
        this.logger.Error(message);
    }

    public void LogFatal(string message)
    {
        Application.Current.MainPage.DisplayAlert("Fatal", message,
                "OK");
        this.logger.Fatal(message);
    }

    public void LogInfo(string message)
    {
        Application.Current.MainPage.DisplayAlert("Info", message,
                "OK");
        this.logger.Info(message);
    }

    public void LogWarning(string message)
    {
        Application.Current.MainPage.DisplayAlert("Warning", message,
                "OK");
        this.logger.Warn(message);
    }
}