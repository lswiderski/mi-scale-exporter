using System;
using System.Threading.Tasks;
using MiScaleExporter.Models;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace MiScaleExporter.Services;

public interface IGarminService
{
    Task<GarminApiResponse> UploadAsync(BodyComposition bodyComposition, CredentialsData credencials);
    Task<GarminFitFileCreationResult> GenerateFitFileAsync(BodyComposition bodyComposition);
}