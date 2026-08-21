using System;

namespace MiScaleExporter.Models
{
    public class Weight
    {
        public DateTime Date { get; set; }

        public float WeightKg { get; set; }

        public float Height { get; set; }

        public float BMI { get; set; }

        public float BodyFat { get; set; }

        public float BodyWater { get; set; }

        public float BoneMass { get; set; }

        public int MetabolicAge { get; set; }

        public float MuscleMass { get; set; }

        public float ProteinMass { get; set; }

        public int VisceralFat { get; set; }

        public int BasalMetabolism { get; set; }

        public int BodyScore { get; set; }

        public int HeartRate { get; set; }

        public float SkeletalMuscleMass { get; set; }

        public string Source { get; set; }

        public string User { get; set; }
    }
}
