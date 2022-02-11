namespace MiScaleDecoder.Contracts
{
    public struct FatPercentageScale
    {
        public int Min { get; set; }
        
        public int Max { get; set; }
        
        public double[] Female { get; set; }
        
        public double[] Male { get; set; }
    }
}