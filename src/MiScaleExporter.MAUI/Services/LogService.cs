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
        ShowAlert("Error", message);
    }

    public void LogFatal(string message)
    {

        ShowAlert("Fatal", message);
    }

    public void LogInfo(string message)
    {
        ShowAlert("Info", message);
    }

    public void LogWarning(string message)
    {

        ShowAlert("Warning", message);
    }

    // Log methods are called from background threads (BLE callbacks, Task.Run),
    // so any UI interaction must be marshalled onto the main thread.
    private static void ShowAlert(string title, string message)
    {
        try
        {
            if (MainThread.IsMainThread)
            {
                ShowAlertOnMainThread(title, message);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => ShowAlertOnMainThread(title, message));
            }
        }
        catch
        {
            // Logging must never throw; swallow any UI/marshalling failure.
        }
    }

    private static void ShowAlertOnMainThread(string title, string message)
    {
        try
        {
            var page = Application.Current?.MainPage;
            page?.DisplayAlert(title, message, "OK");
        }
        catch
        {
            // Logging must never throw; swallow any UI failure.
        }
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
