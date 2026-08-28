using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
#if ANDROID
using Android.Net;
using Xamarin.Android.Net;
#endif
using MiScaleExporter.Core.Composition;
using MiScaleExporter.Core.Garmin;
using MiScaleExporter.Core.History;
using MiScaleExporter.Core.S400;
using MiScaleExporter.Models;
using Newtonsoft.Json;
using YetAnotherGarminConnectClient;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace MiScaleExporter.Services;

public class GarminService : IGarminService
{
    private readonly HttpClient _httpClient;
    private readonly ILogService _logService;
    private IClient _garminClient;
    private readonly IMeasurementHistoryStore _historyStore;

    public GarminService(
        ILogService logService,
        IMeasurementHistoryStore historyStore)
    {
        _logService = logService;
        _historyStore = historyStore;

        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

#if ANDROID
        _httpClient = new HttpClient(new AndroidMessageHandler())
        {
            Timeout = TimeSpan.FromMinutes(5),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            DefaultRequestVersion = HttpVersion.Version11,
        };
#else
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };
#endif

    }

    public async Task<GarminApiResponse> UploadAsync(BodyComposition bodyComposition, DateTime time, CredentialsData credencials)
    {
        var guardedResult = await BeginMeasurementUploadAsync(bodyComposition);
        if (guardedResult != null)
        {
            return guardedResult;
        }

        if (DeviceInfo.Platform == DevicePlatform.Android) //Older android versions do not support TLS 1.3, so force to use external api for them
        {
            if (DeviceInfo.Version.Major < 10)
            {
                Preferences.Set(PreferencesKeys.UseExternalAPI, true);
            }
        }
        var result = Preferences.Get(PreferencesKeys.UseExternalAPI, false)
            ? await UploadViaExternalAPIAsync(bodyComposition, time, credencials)
            : await UploadViaDirectCallToGarminAsync(bodyComposition, time, credencials);
        await CompleteMeasurementUploadAsync(bodyComposition, result);
        return result;
    }

    private async Task<GarminApiResponse> BeginMeasurementUploadAsync(BodyComposition bodyComposition)
    {
        if (string.IsNullOrWhiteSpace(bodyComposition?.MeasurementId))
        {
            return null;
        }

        try
        {
            var beginResult = await _historyStore.TryBeginUploadAsync(bodyComposition.MeasurementId);
            if (beginResult == UploadBeginResult.NotFound)
            {
                return new GarminApiResponse
                {
                    Message = "The S400 measurement was not saved locally, so it was not uploaded.",
                };
            }
            if (beginResult == UploadBeginResult.AlreadySucceeded)
            {
                return new GarminApiResponse
                {
                    IsSuccess = true,
                    Message = "This measurement was already uploaded to Garmin.",
                };
            }
            if (beginResult == UploadBeginResult.AlreadyUploading)
            {
                return new GarminApiResponse
                {
                    Message = "The previous Garmin upload may have completed. Automatic retry was blocked to avoid a duplicate.",
                };
            }

            return null;
        }
        catch (Exception exception)
        {
            _logService.LogError(exception.Message);
            return new GarminApiResponse
            {
                Message = "Could not save the Garmin upload marker, so no upload was attempted.",
            };
        }
    }

    private async Task CompleteMeasurementUploadAsync(
        BodyComposition bodyComposition,
        GarminApiResponse result)
    {
        if (string.IsNullOrWhiteSpace(bodyComposition?.MeasurementId))
        {
            return;
        }

        var state = result?.MFARequested == true
            ? MeasurementUploadState.Pending
            : result?.IsSuccess == true
                ? MeasurementUploadState.Succeeded
                : MeasurementUploadState.Failed;
        try
        {
            await _historyStore.MarkUploadAsync(
                bodyComposition.MeasurementId,
                state,
                state == MeasurementUploadState.Failed ? "Garmin upload failed." : null);
        }
        catch (Exception exception)
        {
            _logService.LogError($"Garmin upload finished but its receipt could not be saved: {exception.Message}");
            if (result != null)
            {
                result.LocalReceiptSaved = false;
                result.Message = $"{result.Message} Garmin responded, but the local upload receipt could not be saved. Automatic retry is blocked to avoid a duplicate.".Trim();
            }
        }
    }

    private async Task<GarminApiResponse> UploadViaDirectCallToGarminAsync(BodyComposition bodyComposition, DateTime time, CredentialsData credencials)
    {
        var result = new GarminApiResponse();
        try
        {
            var payload = MapForGarmin(bodyComposition, time);
            var userProfileSettings = CreateUserProfileSettings(payload);
            var scaleDTO = CreateWeightScaleDto(payload);

            if (string.IsNullOrEmpty(bodyComposition.MFACode))
            {
                var useChinaServer = Preferences.Get(PreferencesKeys.UseChinaServer, false);
                var garminServer = useChinaServer
                    ? YetAnotherGarminConnectClient.Dto.GarminServer.CHINA
                    : YetAnotherGarminConnectClient.Dto.GarminServer.GLOBAL;
                _garminClient = await ClientFactory.Create(garminServer);
            }

            var garminApiReponse = await _garminClient.UploadWeight(scaleDTO, userProfileSettings, credencials, bodyComposition.MFACode);
            var errorlogs = LogService.GetErrorLogs();

            result.IsSuccess = garminApiReponse.IsSuccess;
            result.MFARequested = garminApiReponse.MFACodeRequested;
            result.AccessToken = garminApiReponse.AccessToken;
            result.TokenSecret = garminApiReponse.TokenSecret;

            if (result.MFARequested)
            {
                result.Message = "Please provide MFA/2FA Code";
            }

            if (!result.IsSuccess && !result.MFARequested)
            {
                var errorMessage = garminApiReponse?.ErrorLogs?.FirstOrDefault() ?? errorlogs?.FirstOrDefault() ?? "Error";
                throw new Exception(errorMessage);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logService.LogError(ex?.Message);
            result.Message = ex.Message;
            result.AccessToken = string.Empty;
            result.TokenSecret = string.Empty;
            return result;
        }

    }

    public async Task<GarminFitFileCreationResult> GenerateFitFileAsync(BodyComposition bodyComposition, DateTime time)
    {
        var result = new GarminFitFileCreationResult();
        try
        {
            var payload = MapForGarmin(bodyComposition, time);
            var userProfileSettings = CreateUserProfileSettings(payload);
            var scaleDTO = CreateWeightScaleDto(payload);

            _garminClient = await ClientFactory.Create();

            var file = _garminClient.GenerateWeightFitFile(scaleDTO, userProfileSettings);
            var errorlogs = LogService.GetErrorLogs();

            if(errorlogs.Count > 0)
            {
                result.Message = string.Join(Environment.NewLine, errorlogs);
            }
            result.IsSuccess = file != null;
            result.file = file;
            return result;
        }
        catch (Exception ex)
        {
            _logService.LogError(ex?.Message);
            result.Message = ex.Message;
            return result;
        }

    }

    private async Task<GarminApiResponse> UploadViaExternalAPIAsync(BodyComposition bodyComposition, DateTime time, CredentialsData credencials)
    {
        try
        {
            var payload = MapForGarmin(bodyComposition, time);
            var request = new GarminBodyCompositionRequest
            {
                Email = credencials.Email,
                Password = credencials.Password,
                AccessToken = credencials.AccessToken,
                TokenSecret = credencials.TokenSecret,
                Weight = payload.WeightKg,
                BoneMass = payload.BoneMassKg,
                MuscleMass = payload.LeanSoftMassKg,
                MetabolicAge = payload.MetabolicAge,
                PercentFat = payload.FatPercentage,
                VisceralFatRating = payload.VisceralFatRating,
                BodyMassIndex = payload.Bmi,
                PercentHydration = payload.HydrationPercentage,
                PhysiqueRating = payload.PhysiqueRating,
                TimeStamp = payload.MeasuredAt.ToUnixTimeSeconds(),
            };

            if (!string.IsNullOrEmpty(bodyComposition.ExternalApiClientId))
            {
                request.MFACode = bodyComposition.MFACode;
                request.ClientID = bodyComposition.ExternalApiClientId;
            }
            return await UploadToGarminCloud(request);
        }
        catch (GarminMappingException exception)
        {
            _logService.LogError(exception.Message);
            return new GarminApiResponse { Message = exception.Message };
        }
    }

    private static GarminCompositionPayload MapForGarmin(BodyComposition bodyComposition, DateTime time)
    {
        var sex = (Models.Sex)Preferences.Get(PreferencesKeys.UserSex, (byte)Models.Sex.Male);
        var measuredAt = bodyComposition.MeasuredAt ?? time.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(time),
            DateTimeKind.Local => new DateTimeOffset(time),
            _ => new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Local)),
        };
        var includeComposition = bodyComposition.MeasurementQuality is { } quality
            ? S400MeasurementCompletionPolicy.CanUploadComposition(quality)
            : bodyComposition.HasImpedance
                || bodyComposition.Fat > 0
                || bodyComposition.WaterPercentage > 0
                || bodyComposition.MuscleMass > 0
                || bodyComposition.BoneMass > 0;

        return GarminCompositionMapper.Map(new GarminCompositionInput(
            measuredAt,
            bodyComposition.Weight,
            bodyComposition.BMI,
            includeComposition,
            bodyComposition.Fat,
            bodyComposition.WaterPercentage,
            bodyComposition.MuscleMass,
            bodyComposition.BoneMass,
            bodyComposition.VisceralFat,
            bodyComposition.BodyType,
            bodyComposition.MetabolicAge,
            sex == Models.Sex.Female ? BodySex.Female : BodySex.Male));
    }

    private static UserProfileSettings CreateUserProfileSettings(GarminCompositionPayload payload) =>
        new()
        {
            Age = Preferences.Get(PreferencesKeys.UserAge, 25),
            Height = Preferences.Get(PreferencesKeys.UserHeight, 170),
            Gender = payload.Sex == BodySex.Female
                ? Dynastream.Fit.Gender.Female
                : Dynastream.Fit.Gender.Male,
        };

    private static GarminWeightScaleDTO CreateWeightScaleDto(GarminCompositionPayload payload) =>
        new()
        {
            TimeStamp = payload.MeasuredAt.UtcDateTime,
            Weight = payload.WeightKg,
            PercentFat = payload.FatPercentage,
            PercentHydration = payload.HydrationPercentage,
            BoneMass = payload.BoneMassKg,
            MuscleMass = payload.LeanSoftMassKg,
            VisceralFatRating = payload.VisceralFatRating,
            VisceralFatMass = payload.VisceralFatMassKg,
            PhysiqueRating = payload.PhysiqueRating,
            MetabolicAge = payload.MetabolicAge,
            BodyMassIndex = payload.Bmi,
        };

    private async Task<GarminApiResponse> UploadToGarminCloud(GarminBodyCompositionRequest request)
    {
        var result = new GarminApiResponse();
        try
        {
            var dataAsString = JsonConvert.SerializeObject(
                request,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var content = new StringContent(dataAsString, Encoding.UTF8, "application/json");

            var response = await PostAsync("/upload", content);

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                var message = await reader.ReadToEndAsync();
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<GarminExternalApiResponse>(message);

                    result.AccessToken = apiResponse?.UploadResult?.AccessToken;
                    result.TokenSecret = apiResponse?.UploadResult?.TokenSecret;

                    if (apiResponse?.UploadResult?.AuthStatus == YetAnotherGarminConnectClient.Dto.AuthStatus.MFARedirected)
                    {
                        result.Message = "Please provide MFA/2FA Code";
                        result.MFARequested = true;
                        result.ExternalApiClientId = apiResponse.ClientId;
                        return result;
                    }
                }

                result.ExternalApiClientId = null;
                result.IsSuccess = response.IsSuccessStatusCode;
                result.Message = message;

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