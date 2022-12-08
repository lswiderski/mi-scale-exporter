using System;
using System.IO;
using System.Reflection;
using System.Xml;
using MiScaleExporter.Log;
using MiScaleExporter.Models;
using NLog;
using NLog.Config;
using NLog.Targets;

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