using System;

namespace MiScaleExporter.Models
{
    /// <summary>
    /// A single paired calibration sample: what the scale reported vs. a trusted
    /// reference (e.g. a medical-grade body composition scan) at the same body state.
    /// Used to fit a linear correction trueFat = a * scaleFat + b.
    /// </summary>
    public class FatCalibrationPoint
    {
        public DateTime Date { get; set; }

        /// <summary>Body fat % the scale/app reported for this measurement.</summary>
        public double ScaleFat { get; set; }

        /// <summary>Reference (medical scan) body fat % for the same measurement.</summary>
        public double TrueFat { get; set; }
    }
}
