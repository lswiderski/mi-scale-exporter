using System.Collections.Generic;

namespace MiScaleDecoder.Contracts
{
    public struct MuscleMassScale
    {
        public IDictionary<Sex,double> Min { get; set; }
        public double[] Female { get; set; }
        public double[] Male { get; set; }
    }
}