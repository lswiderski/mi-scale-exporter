using Newtonsoft.Json;

namespace MiScaleExporter.Models;

public class IntervalsAthlete
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("sex")]
    public string Sex { get; set; }

    [JsonProperty("height")]
    public double? Height { get; set; }

    [JsonProperty("height_units")]
    public string HeightUnits { get; set; }

    [JsonProperty("icu_date_of_birth")]
    public string DateOfBirth { get; set; }
}
