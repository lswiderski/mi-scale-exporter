using MiScaleExporter.Models;
using Plugin.Maui.Health;
using Plugin.Maui.Health.Constants;
using Plugin.Maui.Health.Enums;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
using AndroidX.Activity.Result;
#endif

namespace MiScaleExporter.Services;

public class HealthConnectService : IHealthConnectService
{
    // Health Connect write permissions we need. We request these directly instead of going through
    // Plugin.Maui.Health's RequestPermissionsAsync: its PermissionType enum is [Flags] with Read = 0,
    // so requesting Write also requires the READ_* permissions, which a write-only app does not declare.
    private static readonly string[] WritePermissionNames =
    {
        "android.permission.health.WRITE_WEIGHT",
        "android.permission.health.WRITE_BODY_FAT",
        "android.permission.health.WRITE_BONE_MASS",
        "android.permission.health.WRITE_BODY_WATER_MASS",
        "android.permission.health.WRITE_LEAN_BODY_MASS",
        "android.permission.health.WRITE_BASAL_METABOLIC_RATE",
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

#if ANDROID
        try
        {
            return await RequestWritePermissionsAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError($"Health Connect permission request failed: {ex.Message}");
            return false;
        }
#else
        return false;
#endif
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

            var written = 0;

            async Task TryWriteAsync(HealthParameter parameter, double value, string unit)
            {
                if (value <= 0)
                {
                    return;
                }

                try
                {
                    await _health.WriteAsync(parameter, when, value, unit);
                    written++;
                }
                catch (Exception ex)
                {
                    // A metric whose write permission was not granted (or that otherwise fails) is
                    // skipped so the remaining metrics still get exported.
                    _logService.LogWarning($"Health Connect metric skipped: {parameter} - {ex.Message}");
                }
            }

            await TryWriteAsync(HealthParameter.BodyMass, weight, Units.Mass.Kilograms);
            await TryWriteAsync(HealthParameter.BodyFatPercentage, fat, Units.Others.Percentage);
            await TryWriteAsync(HealthParameter.BoneMass, bodyComposition.BoneMass, Units.Mass.Kilograms);
            await TryWriteAsync(HealthParameter.BodyWaterMass, bodyWaterMass, Units.Mass.Kilograms);
            await TryWriteAsync(HealthParameter.LeanBodyMass, leanBodyMass, Units.Mass.Kilograms);
            await TryWriteAsync(HealthParameter.BasalEnergyBurned, bodyComposition.BMR, Units.Energy.Kilocalories);

            if (written == 0)
            {
                return HealthConnectUploadResult.Failure("No Health Connect metrics could be written.");
            }

            return HealthConnectUploadResult.Success();
        }
        catch (Exception ex)
        {
            _logService.LogError($"Health Connect export failed: {ex.Message}");
            return HealthConnectUploadResult.Failure(ex.Message);
        }
    }

#if ANDROID
    private Task<bool> RequestWritePermissionsAsync()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
                {
                    tcs.TrySetResult(false);
                    return;
                }

                var contract = AndroidX.Health.Connect.Client.PermissionController.CreateRequestPermissionResultContract();
                var key = "miscale_health_permissions_" + Guid.NewGuid().ToString("N");

                AndroidX.Activity.Result.ActivityResultLauncher launcher = null;
                var callback = new HealthPermissionResultCallback(granted =>
                {
                    try
                    {
                        launcher?.Unregister();
                    }
                    catch
                    {
                        // The activity may already have disposed the launcher; nothing to do.
                    }

                    tcs.TrySetResult(WritePermissionNames.Any(granted.Contains));
                });

                launcher = activity.ActivityResultRegistry.Register(key, contract, callback);
                launcher.Launch(BuildPermissionSet());
            }
            catch (Exception ex)
            {
                _logService.LogError($"Health Connect permission request failed: {ex.Message}");
                tcs.TrySetResult(false);
            }
        });

        return tcs.Task;
    }

    private static Java.Util.HashSet BuildPermissionSet()
    {
        var set = new Java.Util.HashSet();
        foreach (var permission in WritePermissionNames)
        {
            set.Add(new Java.Lang.String(permission));
        }

        return set;
    }
#endif
}

#if ANDROID
internal sealed class HealthPermissionResultCallback : Java.Lang.Object, IActivityResultCallback
{
    private readonly Action<HashSet<string>> _onResult;

    public HealthPermissionResultCallback(Action<HashSet<string>> onResult)
    {
        _onResult = onResult;
    }

    public void OnActivityResult(Java.Lang.Object result)
    {
        var granted = new HashSet<string>();
        var collection = Java.Interop.JavaObjectExtensions.JavaCast<Java.Util.AbstractCollection>(result);
        var iterator = collection?.Iterator();
        while (iterator != null && iterator.HasNext)
        {
            granted.Add(iterator.Next()?.ToString() ?? string.Empty);
        }

        _onResult(granted);
    }
}
#endif
