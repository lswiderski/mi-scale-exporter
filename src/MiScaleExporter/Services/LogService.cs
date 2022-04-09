using System;
using System.IO;
using System.Reflection;
using System.Xml;
using MiScaleExporter.Log;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xamarin.Forms;

namespace MiScaleExporter.Services;

public class LogService : ILogService
{
    private readonly ILogger logger;
    private readonly ILogManager logManager;
    public LogService(ILogger logger, ILogManager logManager)
    {
        this.logger = logger;
        this.logManager = logManager;
    }

    public void LogDebug(string message)
    {
        logManager.CreateFolderIfNecesarry();
        this.logger.Info(message);
    }

    public void LogError(string message)
    {
        logManager.CreateFolderIfNecesarry();
        Application.Current.MainPage.DisplayAlert("Error", message,
                  "OK");
        this.logger.Error(message);
    }

    public void LogFatal(string message)
    {
        logManager.CreateFolderIfNecesarry();
        Application.Current.MainPage.DisplayAlert("Fatal", message,
                "OK");
        this.logger.Fatal(message);
    }

    public void LogInfo(string message)
    {
        logManager.CreateFolderIfNecesarry();
        Application.Current.MainPage.DisplayAlert("Info", message,
                "OK");
        this.logger.Info(message);
    }

    public void LogWarning(string message)
    {
        logManager.CreateFolderIfNecesarry();
        Application.Current.MainPage.DisplayAlert("Warning", message,
                "OK");
        this.logger.Warn(message);
    }
}