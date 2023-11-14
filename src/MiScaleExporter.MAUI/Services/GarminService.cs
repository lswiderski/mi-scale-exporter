using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiScaleExporter.Models;
using Newtonsoft.Json;
using NLog;
using NLog.Extensions.Logging;
using Xamarin.Android.Net;
using YetAnotherGarminConnectClient;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace MiScaleExporter.Services;

public class GarminService : IGarminService
{
    private HttpClient _httpClient;
    private ILogService _logService;
    private Microsoft.Extensions.Logging.ILogger _logger;
    private bool _garminClientAlreadyCreatedForMFA = false;
    private IClient _garminClient;

    public GarminService(ILogService logService)
    {
        _logService = logService;
        var configuration = LogService.CreateLogger();
        using (ILoggerFactory factory = LoggerFactory.Create(builder =>
        builder.AddNLog(configuration))
    )
        {
            _logger =  factory.CreateLogger<GarminService>();
        }

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            _httpClient = new HttpClient(new AndroidMessageHandler())
            {
                Timeout = TimeSpan.FromMinutes(5),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                DefaultRequestVersion = HttpVersion.Version11
            };
        }
        else
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(5),
            };
        }
        
    }

    public async Task<GarminApiResponse> UploadAsync(BodyComposition bodyComposition, DateTime time, string email, string password)
    {

        if (DeviceInfo.Platform == DevicePlatform.Android) //Older android versions do not support TLS 1.3, so force to use external api for them
        {
            if (DeviceInfo.Version.Major < 10)
            {
                Preferences.Set(PreferencesKeys.UseExternalAPI, true);
            }
        }
        if (Preferences.Get(PreferencesKeys.UseExternalAPI, false))
        {
            return await UploadViaExternalAPIAsync(bodyComposition, time, email, password);
        }
        else
        {
            return await UploadViaDirectCallToGarminAsync(bodyComposition, time, email, password);
        }
    }

    private async Task<GarminApiResponse> UploadViaDirectCallToGarminAsync(BodyComposition bodyComposition, DateTime time, string email, string password)
    {
        var result = new GarminApiResponse();
        try
        {
            var userProfileSettings = new UserProfileSettings
            {
                Age = Preferences.Get(PreferencesKeys.UserAge, 25),
                Height = Preferences.Get(PreferencesKeys.UserHeight, 170),
            };
            var scaleDTO = new GarminWeightScaleDTO
            {
                TimeStamp = time,
                Weight = Convert.ToSingle(bodyComposition.Weight),
                PercentFat = Convert.ToSingle(bodyComposition.Fat),
                PercentHydration = Convert.ToSingle(bodyComposition.WaterPercentage),
                BoneMass = Convert.ToSingle(bodyComposition.BoneMass),
                MuscleMass = Convert.ToSingle(bodyComposition.MuscleMass),
                VisceralFatRating = Convert.ToByte(bodyComposition.VisceralFat),
                VisceralFatMass = Convert.ToSingle(bodyComposition.VisceralFat),
                PhysiqueRating = Convert.ToByte(bodyComposition.BodyType),
                MetabolicAge = Convert.ToByte(bodyComposition.MetabolicAge),
                BodyMassIndex = Convert.ToSingle(bodyComposition.BMI),
                Email = email,
                Password = password,
            };

            var data = scaleDTO with { Email = email, Password = password};
            if (!_garminClientAlreadyCreatedForMFA)
            {
                _garminClient = await ClientFactory.Create();
            }
           
            try
            {
                var garminApiReponse = await _garminClient.UploadWeight(data, userProfileSettings, bodyComposition.MFACode);
                var logs = LogService.GetLogs();
                var errorlogs = LogService.GetErrorLogs();

                result.IsSuccess = garminApiReponse.IsSuccess;
                result.MFARequested = garminApiReponse.MFACodeRequested;

                if (result.MFARequested)
                {
                    result.Message = "Please provide MFA/2FA Code";
                    _garminClientAlreadyCreatedForMFA = true;
                }

                if (result.IsSuccess)
                {
                    _garminClientAlreadyCreatedForMFA = false;
                }

                if (!result.IsSuccess && !result.MFARequested) {
                   
                    throw new Exception(garminApiReponse.ErrorLogs.FirstOrDefault());
                }
                return result;
            }
            catch (Exception ex)
            {
                var logs = LogService.GetLogs();
                var errorlogs = LogService.GetErrorLogs();
                throw;
            }
            
            
        }
        catch (Exception ex)
        {
            _logService.LogError(ex.Message);
            result.Message = ex.Message;
            return result;
        }

    }

    private async Task<GarminApiResponse> UploadViaExternalAPIAsync(BodyComposition bodyComposition, DateTime time, string email, string password)
    {
        var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
        var request = new GarminBodyCompositionRequest
        {
            Email = email,
            Password = password,
            Weight = bodyComposition.Weight,
            BoneMass = bodyComposition.BoneMass,
            MuscleMass = bodyComposition.MuscleMass,
            MetabolicAge = bodyComposition.MetabolicAge,
            PercentFat = bodyComposition.Fat,
            VisceralFatRating = bodyComposition.VisceralFat,
            BodyMassIndex = bodyComposition.BMI,
            PercentHydration = bodyComposition.WaterPercentage,
            PhysiqueRating = bodyComposition.BodyType,
            TimeStamp = unixTime
        };
        return await UploadToGarminCloud(request);
    }

    private async Task<GarminApiResponse> UploadToGarminCloud(GarminBodyCompositionRequest request)
    {
        var result = new GarminApiResponse();
        try
        {

            var dataAsString = JsonConvert.SerializeObject(request);
            var content = new StringContent(dataAsString, Encoding.UTF8, "application/json");

            var response = await PostAsync("/upload", content);
            result.IsSuccess = response.IsSuccessStatusCode;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                result.Message = await reader.ReadToEndAsync();
                return result;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex.Message);
            result.Message = ex.Message;
            return result;
        }
    }

    private async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
    {
        var baseAddress = Preferences.Get(PreferencesKeys.ApiServerAddressOverride, SettingKeys.ApiServerAddress);
        var response = await _httpClient.PostAsync($"{baseAddress}{requestUri}", content);
        return response;
    }
}