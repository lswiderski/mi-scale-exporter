using System.Reflection;

namespace MiScaleExporter.Services;

public interface ILogService
{
    void LogDebug(string message);
    void LogError(string message);
    void LogFatal(string message);
    void LogInfo(string message);
    void LogWarning(string message);
}