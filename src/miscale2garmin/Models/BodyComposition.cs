using System;
using System.Collections.Generic;
using System.Text;

namespace miscale2garmin.Models
{
    public class BodyComposition
    {
        public double BMI { get; set; }
        public double Weight { get; set; }
        public double IdealWeight { get; set; }
        public double MetabolicAge { get; set; }
        public double ProteinPercentage { get; set; }
        public double BMR { get; set; }
        public double Fat { get; set; }
        public double MuscleMass { get; set; }
        public double BoneMass { get; set; }
        public double VisceralFat { get; set; }
        public int BodyType { get; set; }
        public double WaterPercentage { get; set; }
        public bool IsValid { get; set; }
    }
}
