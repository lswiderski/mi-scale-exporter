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
    private Logger logger;

    public LogService()
    {
        var config = new LoggingConfiguration();

        var consoleTarget = new ConsoleTarget();
        config.AddTarget("console", consoleTarget);

        var consoleRule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
        config.LoggingRules.Add(consoleRule);

        var fileTarget = new FileTarget();

        string folder = Xamarin.Essentials.FileSystem.AppDataDirectory;

        var date = DateTime.UtcNow.Date.ToString("dd.MM.yyyy");
        fileTarget.FileName = Path.Combine(folder, "Logs", string.Format("log-{0}.txt", date));
        config.AddTarget("file", fileTarget);

        var fileRule = new LoggingRule("*", LogLevel.Info, fileTarget);
        config.LoggingRules.Add(fileRule);

        LogManager.Configuration = config;
        
        this.logger = LogManager.GetCurrentClassLogger();
    }

    public void LogDebug(string message)
    {
        this.logger.Info(message);
    }

    public void LogError(string message)
    {
        this.logger.Error(message);
    }

    public void LogFatal(string message)
    {
        this.logger.Fatal(message);
    }

    public void LogInfo(string message)
    {
        this.logger.Info(message);
    }

    public void LogWarning(string message)
    {
        this.logger.Warn(message);
    }
}