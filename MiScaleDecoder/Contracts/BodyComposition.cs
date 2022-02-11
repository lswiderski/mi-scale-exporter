namespace MiScaleDecoder.Contracts
{
    public class BodyComposition
    {
        public double BMI { get; set; }
        public double Weight { get; set; }
        public double IdealWeight { get; set; }
        public double MetabolicAge { get; set; }
        public double ProteinPercentage { get; set; }
        public double LBMCoefficient { get; set; }
        public double BMR { get; set; }
        public double Fat { get; set; }
        public double MuscleMass { get; set; }
        public double BoneMass { get; set; }
        public double VisceralFat { get; set; }
        public double Water { get; set; }
        public int BodyType { get; set; }
        
        public string BodyTypeName { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        
    }
}
