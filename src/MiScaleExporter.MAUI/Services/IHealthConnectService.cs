using MiScaleExporter.Models;

namespace MiScaleExporter.Services;

public interface IHealthConnectService
{
    bool IsSupported { get; }

    /// <summary>
    /// Prompts for the Health Connect write permissions. Returns true when at least one of the
    /// requested permissions is granted (partial grants are allowed — ungranted metrics are skipped
    /// on write); false when none are granted.
    /// </summary>
    Task<bool> EnsurePermissionsAsync();

    Task<HealthConnectUploadResult> WriteAsync(BodyComposition bodyComposition);
}
