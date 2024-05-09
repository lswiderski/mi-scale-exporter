using YetAnotherGarminConnectClient.Dto;

namespace MiScaleExporter.Models
{
    public record GarminExternalApiResponse
    {
        public string ClientId { get; set; }
        public GarminUploadResult UploadResult { get; set; }
    }
}
