using System;
using System.Threading.Tasks;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services;

public interface IGarminService
{
    Task<GarminApiResponse> UploadAsync(BodyComposition bodyComposition, DateTime time, string email, string password);
}