using MiScaleExporter.Models;
using Plugin.Maui.Health;
using Plugin.Maui.Health.Constants;
using Plugin.Maui.Health.Enums;

namespace MiScaleExporter.Services;

public class HealthConnectService : IHealthConnectService
{
    private static readonly (HealthParameter Parameter, PermissionType Permission)[] WriteRequests =
    {
        (HealthParameter.BodyMass, PermissionType.Write),
        (HealthParameter.BodyFatPercentage, PermissionType.Write),
        (HealthParameter.BoneMass, PermissionType.Write),
        (HealthParameter.BodyWaterMass, PermissionType.Write),
        (HealthParameter.LeanBodyMass, PermissionType.Write),
        (HealthParameter.BasalEnergyBurned, PermissionType.Write),
    };

    private readonly IHealth _health;
    private readonly ILogService _logService;

    public HealthConnectService(IHealth health, ILogService logService)
    {
        _health = health;
        _logService = logService;
    }

    public bool IsSupported => _health.IsSupported;

    public async Task<bool> EnsurePermissionsAsync()
    {
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            return await _health.RequestPermissionsAsync(WriteRequests);
        }
        catch (Exception ex)
        {
            _logService.LogError($"Health Connect permission request failed: {ex.Message}");
            return false;
        }
    }

    public async Task<HealthConnectUploadResult> WriteAsync(BodyComposition bodyComposition)
    {
        if (bodyComposition is null)
        {
            return HealthConnectUploadResult.Failure("No body composition data to export.");
        }

        if (!IsSupported)
        {
            return HealthConnectUploadResult.Unavailable();
        }

        try
        {
            var when = new DateTimeOffset(bodyComposition.Date);
            var weight = bodyComposition.Weight;
            var fat = bodyComposition.Fat;
            var leanBodyMass = weight * (1 - fat / 100.0);
            var bodyWaterMass = weight * bodyComposition.WaterPercentage / 100.0;

            await _health.WriteAsync(HealthParameter.BodyMass, when, weight, Units.Mass.Kilograms);

            if (fat > 0)
            {
                await _health.WriteAsync(HealthParameter.BodyFatPercentage, when, fat, Units.Others.Percentage);
            }

            if (bodyComposition.BoneMass > 0)
            {
                await _health.WriteAsync(HealthParameter.BoneMass, when, bodyComposition.BoneMass, Units.Mass.Kilograms);
            }

            if (bodyWaterMass > 0)
            {
                await _health.WriteAsync(HealthParameter.BodyWaterMass, when, bodyWaterMass, Units.Mass.Kilograms);
            }

            if (leanBodyMass > 0)
            {
                await _health.WriteAsync(HealthParameter.LeanBodyMass, when, leanBodyMass, Units.Mass.Kilograms);
            }

            if (bodyComposition.BMR > 0)
            {
                await _health.WriteAsync(HealthParameter.BasalEnergyBurned, when, bodyComposition.BMR, Units.Energy.Kilocalories);
            }

            return HealthConnectUploadResult.Success();
        }
        catch (Exception ex)
        {
            _logService.LogError($"Health Connect export failed: {ex.Message}");
            return HealthConnectUploadResult.Failure(ex.Message);
        }
    }
}
