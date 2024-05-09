using YetAnotherGarminConnectClient.Dto;

namespace MiScaleExporter.Models
{
    public record GarminUploadResult
    {
        public bool IsSuccess { get; set; }
        public long UploadId { get; set; }
        public IList<string> Logs { get; set; }
        public IList<string> ErrorLogs { get; set; }
        public AuthStatus AuthStatus { get; set; }
        public bool MFACodeRequested { get; set; }
        public string? AccessToken { get; set; }
        public string? TokenSecret { get; set; }
    }
}
