namespace MiScaleExporter.Models;

public record GarminBodyCompositionRequest
{
    public long TimeStamp { get; set; }
    public double Weight { get; set; }
    public double PercentFat { get; set; }
    public double PercentHydration { get; set; }
    public double BoneMass { get; set; }
    public double MuscleMass { get; set; }
    public double VisceralFatRating{ get; set; }
    public int PhysiqueRating { get; set; }
    public double MetabolicAge { get; set; }
    public double BodyMassIndex { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ClientID { get; set; }
    public string MFACode { get; set; }
    public string AccessToken { get; set; }
    public string TokenSecret { get; set; }
}