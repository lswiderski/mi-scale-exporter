using System;
using System.IO;
using System.Reflection;
using System.Xml;
using MiScaleExporter.Log;
using MiScaleExporter.Models;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xamarin.Essentials;
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
        if (Preferences.Get(PreferencesKeys.SaveToStorage, false))
        {
            this.logManager.CreateFolderIfNecesarry();
            this.logger.Info(message);
        }
    }

    public void LogError(string message)
    {
        if (Preferences.Get(PreferencesKeys.SaveToStorage, false))
        {
            this.logManager.CreateFolderIfNecesarry();
            this.logger.Error(message);
        }
        Application.Current.MainPage.DisplayAlert("Error", message,
                  "OK");
    }

    public void LogFatal(string message)
    {
        if (Preferences.Get(PreferencesKeys.SaveToStorage, false))
        {
            this.logManager.CreateFolderIfNecesarry();
            this.logger.Fatal(message);
        }
      
        Application.Current.MainPage.DisplayAlert("Fatal", message,
                "OK");
    }

    public void LogInfo(string message)
    {
        if (Preferences.Get(PreferencesKeys.SaveToStorage, false))
        {
            this.logManager.CreateFolderIfNecesarry();
            this.logger.Info(message);
        }
      
        Application.Current.MainPage.DisplayAlert("Info", message,
                "OK");
    }

    public void LogWarning(string message)
    {
        if (Preferences.Get(PreferencesKeys.SaveToStorage, false))
        {
            this.logManager.CreateFolderIfNecesarry();
            this.logger.Warn(message);
        }
       
        Application.Current.MainPage.DisplayAlert("Warning", message,
                "OK");
    }
}