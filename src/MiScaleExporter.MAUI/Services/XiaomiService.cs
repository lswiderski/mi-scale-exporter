using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using MiScaleExporter.Models;
using MiScaleExporter.Services;
using Newtonsoft.Json;
using YetAnotherXiaomiCloudClient;

namespace MiScaleExporter.Services
{
    public class XiaomiService
    {
        private readonly ILogService _logService;

        public XiaomiService(ILogService logService)
        {
            _logService = logService;
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
