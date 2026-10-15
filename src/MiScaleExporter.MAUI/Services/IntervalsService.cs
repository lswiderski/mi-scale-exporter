using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MiScaleExporter.Models;
using Newtonsoft.Json;

namespace MiScaleExporter.Services;

public class IntervalsService : IIntervalsService
{
    private const string BaseAddress = "https://intervals.icu";
    private static readonly string AthleteId = "0";

    private readonly HttpClient _httpClient;
    private readonly ILogService _logService;

    public IntervalsService(ILogService logService)
    {
        _logService = logService;

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            _httpClient = new HttpClient(new Xamarin.Android.Net.AndroidMessageHandler())
            {
                Timeout = TimeSpan.FromMinutes(5),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                DefaultRequestVersion = HttpVersion.Version11,
            };
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
            };
        }
    }

    public async Task<IntervalsUploadResult> UploadAsync(DateTime date, double weightKg, double bodyFat)
    {
        var result = new IntervalsUploadResult();

        try
        {
            var apiKey = await SecureStorage.GetAsync(PreferencesKeys.IntervalsApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                result.Message = "Intervals.icu API key is not set. Please add it in Settings.";
                return result;
            }

            var wellness = new IntervalsWellness
            {
                Weight = Math.Round(weightKg, 2),
                BodyFat = Math.Round(bodyFat, 2),
            };

            var dataAsString = JsonConvert.SerializeObject(wellness);
            var content = new StringContent(dataAsString, Encoding.UTF8, "application/json");

            var requestUri = $"{BaseAddress}/api/v1/athlete/{AthleteId}/wellness/{date:yyyy-MM-dd}";
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"API_KEY:{apiKey}")));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);
            var message = await response.Content.ReadAsStringAsync();

            result.IsSuccess = response.IsSuccessStatusCode;
            result.Message = response.IsSuccessStatusCode
                ? "Uploaded"
                : string.IsNullOrWhiteSpace(message) ? response.ReasonPhrase : message;

            return result;
        }
        catch (Exception ex)
        {
            _logService.LogError(ex.Message);
            result.IsSuccess = false;
            result.Message = ex.Message;
            return result;
        }
    }
}
