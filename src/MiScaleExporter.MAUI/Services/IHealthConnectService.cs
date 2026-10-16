using MiScaleExporter.Models;

namespace MiScaleExporter.Services;

public interface IHealthConnectService
{
    bool IsSupported { get; }

    Task<bool> EnsurePermissionsAsync();

    Task<HealthConnectUploadResult> WriteAsync(BodyComposition bodyComposition);
}
