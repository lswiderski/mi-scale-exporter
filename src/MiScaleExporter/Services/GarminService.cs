using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MiScaleExporter.Models;
using Newtonsoft.Json;

namespace MiScaleExporter.Services;

public class GarminService : IGarminService
{
    private HttpClient _httpClient;

    public GarminService()
    {
        _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5),
            BaseAddress = new Uri("http://192.168.1.17:4321")
        };
    }

    public async Task<GarminApiResponse> UploadAsync(BodyComposition bodyComposition, DateTime time, string email, string password)
    {
        var unixTime = ((DateTimeOffset) time).ToUnixTimeSeconds();
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
            //TODO log
            result.Message = ex.Message;
            return result;
        }
    }
    
    private async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
    {
       // var baseAddress = "http://192.168.1.17:4321";
        var response = await _httpClient.PostAsync($"{requestUri}", content);
        return response;
    }
}