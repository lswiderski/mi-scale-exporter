using NLog.Config;
using NLog.Targets;
using NLog;

namespace MiScaleExporter.Services;

public class LogService : ILogService
{
    private static string TARGET_NAME = "logmemory";
    private static string ERRORS_TARGET_NAME = "logmemory_errors";
    private static bool _isCreated = false;
    private static LoggingConfiguration _configuration;

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

    public static LoggingConfiguration CreateLogger()
    {
        if (!_isCreated)
        {
            _isCreated = true;

            LoggingConfiguration configuration = new LoggingConfiguration();

            MemoryTarget target = new MemoryTarget(TARGET_NAME);
            target.Layout = "${message}";

            configuration.AddTarget(target);
            LoggingRule traceRule = new LoggingRule("*", NLog.LogLevel.Trace, target);
            configuration.LoggingRules.Add(traceRule);

            MemoryTarget errorsTarget = new MemoryTarget(ERRORS_TARGET_NAME);
            errorsTarget.Layout = "${message}";

            configuration.AddTarget(errorsTarget);
            LoggingRule errorsRule = new LoggingRule("*", NLog.LogLevel.Error, errorsTarget);
            configuration.LoggingRules.Add(errorsRule);
            LogManager.Configuration = configuration;
            _configuration = configuration;
        }

        return _configuration;
    }

    public static IList<string> GetLogs()
    {
        if (!_isCreated)
        {
            return new List<string>();
        }

        var target = LogManager.Configuration.FindTargetByName<MemoryTarget>(TARGET_NAME);
        var logEvents = target.Logs;
        return logEvents;
    }

    public static IList<string> GetErrorLogs()
    {
        if (!_isCreated)
        {
            return new List<string>();
        }

        var target = LogManager.Configuration.FindTargetByName<MemoryTarget>(ERRORS_TARGET_NAME);
        var logEvents = target.Logs;
        return logEvents;
    }
}