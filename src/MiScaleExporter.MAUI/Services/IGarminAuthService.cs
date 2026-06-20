using System.Threading.Tasks;
using MiScaleExporter.Models;

namespace MiScaleExporter.Services;

public interface IGarminAuthService
{
    Task<GarminAuthResult> AuthenticateAsync(string email, string password);
    Task<GarminAuthResult> CompleteMfaAsync(string mfaCode);
}
