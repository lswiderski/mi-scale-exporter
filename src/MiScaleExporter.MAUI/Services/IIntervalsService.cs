using System;
using System.Threading.Tasks;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services;

public interface IIntervalsService
{
    Task<IntervalsUploadResult> UploadAsync(DateTime date, double weightKg, double bodyFat);
}
