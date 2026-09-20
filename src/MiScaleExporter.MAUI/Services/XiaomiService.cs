using AndroidX.ConstraintLayout.Core;
using Microsoft.Maui.Storage;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YetAnotherXiaomiCloudClient;

namespace MiScaleExporter.Services
{
    public class XiaomiService
    {



        private readonly ILogService _logService;
        private XiaomiClientAuthorization _authorization;

        public XiaomiService(ILogService logService)
        {
            _logService = logService;
        }

        public record XiaomiLoginRequestResult(
            string LoginUrl,
            byte[]? QrCodeBytes
        );

        public record XiaomiLoginStatusResponse(
       string Status,
       long? UserId = null,
       string? PassToken = null,
       string? CUserId = null,
       string? ServiceToken = null
   );

        /// <summary>
        /// Authenticates with Xiaomi cloud using email and password, retrieves and stores the pass token.
        /// </summary>
        public async Task<XiaomiLoginRequestResult> LoginAsync()
        {
            try
            {

                var authorization = new XiaomiClientAuthorization();
                _authorization = authorization;

                // Execute steps 1-2: Get QR code
                if (!await authorization.LoginStep1Async())
                {
                    throw new InvalidOperationException("Unable to initiate QR code login");
                }

                var qrCodeBytes = await authorization.LoginStep2Async();
                if (qrCodeBytes == null || qrCodeBytes.Length == 0)
                {
                    throw new InvalidOperationException("Unable to fetch QR code");
                }

                var result = new XiaomiLoginRequestResult(
                   LoginUrl: authorization.LoginUrl,
                   QrCodeBytes: qrCodeBytes
               );

                return result;
            }
            catch (Exception ex)
            {
                _logService?.LogError($"Error while logging in to Xiaomi: {ex.Message}");
                throw;
            }
        }

        private async Task SavePassTokens(XiaomiLoginStatusResponse response)
        {
            if (response != null)
            {
                Preferences.Set(PreferencesKeys.XiaomiPassToken, response.PassToken);
                Preferences.Set(PreferencesKeys.XiaomiUserId, response.UserId.ToString());
            }
        }

        public async Task<XiaomiLoginStatusResponse> CheckLoginStatusAsync()
        {
            try
            {
                // A restart replaces the authorization object. Never let a stale
                // polling callback dereference the old/null authorization state.
                var authorization = _authorization;
                if (authorization == null)
                {
                    return new XiaomiLoginStatusResponse(Status: "pending");
                }

                // Check if already logged in
                if (authorization.PassToken != null && authorization.UserId > 0)
                {
                    var completedResponse = new XiaomiLoginStatusResponse(
                        Status: "completed",
                        UserId: authorization.UserId,
                        PassToken: authorization.PassToken,
                        CUserId: authorization.CUserId,
                        ServiceToken: authorization.ServiceToken
                    );
                    await SavePassTokens(completedResponse);

                    return completedResponse;
                }

                // Execute steps 3-4: Poll for result and get service token
                var step3Result = await authorization.LoginStep3Async();

                if (step3Result)
                {
                    var step4Result = await authorization.LoginStep4Async();

                    if (step4Result)
                    {
                        var completedResponse = new XiaomiLoginStatusResponse(
                            Status: "completed",
                            UserId: authorization.UserId,
                            PassToken: authorization.PassToken,
                            CUserId: authorization.CUserId,
                            ServiceToken: authorization.ServiceToken
                        );

                        await SavePassTokens(completedResponse);
                        
                        return completedResponse;
                    }
                }

                // Still pending
                var pendingResponse = new XiaomiLoginStatusResponse(Status: "pending");
                return pendingResponse;

            }
            catch (InvalidOperationException ex)
            {
                // QR code not scanned yet or timeout
                var pendingResponse = new XiaomiLoginStatusResponse(Status: "pending");
                return pendingResponse;
            }
            catch (Exception ex)
            {
                _logService?.LogError($"Error while checking Xiaomi login status: {ex.Message}");
                var pendingResponse = new XiaomiLoginStatusResponse(Status: "error");
                return pendingResponse;
            }
        }

        /// <summary>
        /// Retrieves weights from Xiaomi cloud using credentials and settings stored in Preferences.
        /// Returns the list of Weight DTOs.
        /// </summary>
        public async Task<List<MiScaleExporter.Models.Weight>> GetModelWeightsFromCloudAsync()
        {
            var userId = Preferences.Get(PreferencesKeys.XiaomiUserId, string.Empty);
            var passToken = Preferences.Get(PreferencesKeys.XiaomiPassToken, string.Empty);
            var region = Preferences.Get(PreferencesKeys.XiaomiAccountRegion, string.Empty);
            var model = Preferences.Get(PreferencesKeys.XiaomiScaleModel, string.Empty);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException("Xiaomi UserId not set in preferences.");
            }

            if (string.IsNullOrWhiteSpace(passToken))
            {
                throw new InvalidOperationException("Xiaomi PassToken not set in preferences.");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new InvalidOperationException("Xiaomi account region not set in preferences.");
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException("Xiaomi scale model not set in preferences.");
            }

            try
            {
                var client = new XiaomiClient("xiaomiio");

                // Login with token. XiaomiClient.LoginWithToken expects long userId.
                if (!long.TryParse(userId, out var userIdLong))
                {
                    throw new InvalidOperationException("Xiaomi UserId is not a valid number.");
                }

                await client.LoginWithToken(userIdLong, passToken);

                // Get model weights
                var raw = await client.GetModelWeights(region, model);

                // If SDK returns typed weights, map them 1:1 to local DTO to avoid JSON round-trip
                if (raw is IEnumerable<YetAnotherXiaomiCloudClient.Weight> sdkEnumerable)
                {
                    return sdkEnumerable.Select(w => new MiScaleExporter.Models.Weight
                    {
                        Date = w.Date,
                        WeightKg = w.WeightKg,
                        Height = w.Height,
                        BMI = w.BMI,
                        BodyFat = w.BodyFat,
                        BodyWater = w.BodyWater,
                        BoneMass = w.BoneMass,
                        MetabolicAge = w.MetabolicAge,
                        MuscleMass = w.MuscleMass,
                        ProteinMass = w.ProteinMass,
                        VisceralFat = w.VisceralFat,
                        BasalMetabolism = w.BasalMetabolism,
                        BodyScore = w.BodyScore,
                        HeartRate = w.HeartRate,
                        SkeletalMuscleMass = w.SkeletalMuscleMass,
                        Source = w.Source,
                        User = w.User
                    }).ToList();
                }


                // Unknown payload shape - log and return empty list
                _logService?.LogWarning("XiaomiService: unexpected payload from GetModelWeights");
                return new List<MiScaleExporter.Models.Weight>();
            }
            catch (Exception ex)
            {
                _logService?.LogError($"Error while retrieving Xiaomi weights: {ex.Message}");
                throw;
            }
        }
    }
}
