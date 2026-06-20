using System;
using System.Threading.Tasks;
using MiScaleExporter.Models;
using YetAnotherGarminConnectClient;

namespace MiScaleExporter.Services;

/// <summary>
/// Performs a real Garmin Connect login (and inline 2FA) up front, separately from the
/// weight upload. Touches YAGCC types directly, so this lives in the app project only.
/// Every public method is wrapped in try/catch and never throws.
/// </summary>
public class GarminAuthService : IGarminAuthService
{
    private readonly ILogService _logService;

    // Kept across calls so a MfaRequired result from AuthenticateAsync can be continued by
    // CompleteMfaAsync on the very same client instance (YAGCC holds the MFA CSRF token on it).
    private IClient _client;

    public GarminAuthService(ILogService logService)
    {
        _logService = logService;
    }

    public async Task<GarminAuthResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new GarminAuthResult
                {
                    Status = GarminAuthStatus.Failed,
                    Message = "Email and password are required",
                };
            }

            var useChinaServer = Preferences.Get(PreferencesKeys.UseChinaServer, false);
            var garminServer = useChinaServer
                ? YetAnotherGarminConnectClient.Dto.GarminServer.CHINA
                : YetAnotherGarminConnectClient.Dto.GarminServer.GLOBAL;

            _client = await ClientFactory.Create(garminServer);

            var authResult = await _client.Authenticate(email, password);

            if (authResult != null && authResult.MFACodeRequested)
            {
                // Persist credentials now so a follow-up CompleteMfaAsync / upload has them.
                PersistCredentials(email, password);
                return new GarminAuthResult
                {
                    Status = GarminAuthStatus.MfaRequired,
                    Message = authResult.Error,
                };
            }

            if (authResult != null && authResult.IsSuccess)
            {
                await PersistSuccessAsync(email, password);
                return new GarminAuthResult { Status = GarminAuthStatus.Success };
            }

            return new GarminAuthResult
            {
                Status = GarminAuthStatus.Failed,
                Message = authResult?.Error,
            };
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex?.Message);
            return new GarminAuthResult
            {
                Status = GarminAuthStatus.Failed,
                Message = ex?.Message,
            };
        }
    }

    public async Task<GarminAuthResult> CompleteMfaAsync(string mfaCode)
    {
        try
        {
            if (_client == null)
            {
                return new GarminAuthResult
                {
                    Status = GarminAuthStatus.Failed,
                    Message = "Start the connection again",
                };
            }

            if (string.IsNullOrWhiteSpace(mfaCode))
            {
                return new GarminAuthResult
                {
                    Status = GarminAuthStatus.Failed,
                    Message = "Enter the 2FA code",
                };
            }

            var authResult = await _client.CompleteMFAAuthAsync(mfaCode.Trim());

            if (authResult != null && authResult.IsSuccess)
            {
                var email = Preferences.Get(PreferencesKeys.GarminUserEmail, string.Empty);
                var password = await SecureStorage.GetAsync(PreferencesKeys.GarminUserPassword);
                await PersistSuccessAsync(email, password);
                return new GarminAuthResult { Status = GarminAuthStatus.Success };
            }

            return new GarminAuthResult
            {
                Status = GarminAuthStatus.Failed,
                Message = authResult?.Error,
            };
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex?.Message);
            return new GarminAuthResult
            {
                Status = GarminAuthStatus.Failed,
                Message = ex?.Message,
            };
        }
    }

    private void PersistCredentials(string email, string password)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            Preferences.Set(PreferencesKeys.GarminUserEmail, email.Trim());
        }
        if (!string.IsNullOrWhiteSpace(password))
        {
            SecureStorage.SetAsync(PreferencesKeys.GarminUserPassword, password);
        }
    }

    private async Task PersistSuccessAsync(string email, string password)
    {
        PersistCredentials(email, password);

        // Tokens are persisted for session reuse; the client falls back to email/password
        // when they aren't directly reusable, so the connect flow stays smooth either way.
        var token = _client?.OAuth2Token;
        if (token != null)
        {
            if (!string.IsNullOrEmpty(token.Access_Token))
            {
                await SecureStorage.SetAsync(PreferencesKeys.GarminUserAccessToken, token.Access_Token);
            }
            if (!string.IsNullOrEmpty(token.Refresh_Token))
            {
                await SecureStorage.SetAsync(PreferencesKeys.GarminUserTokenSecret, token.Refresh_Token);
            }
        }
    }
}
