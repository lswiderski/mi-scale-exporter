using Newtonsoft.Json;

namespace MiScaleExporter.Models;

public record IntervalsWellness
{
    [JsonProperty("weight", NullValueHandling = NullValueHandling.Ignore)]
    public double? Weight { get; set; }

    [JsonProperty("bodyFat", NullValueHandling = NullValueHandling.Ignore)]
    public double? BodyFat { get; set; }
}
