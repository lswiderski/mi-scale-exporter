using System;
using Microsoft.Maui.Graphics;

namespace MiScaleExporter.MAUI.Controls
{
    /// <summary>
    /// Draws a 4-segment body-composition donut (Fat / Muscle / Bone / Other) using pure
    /// Microsoft.Maui.Graphics. Only the ratios of the four values matter for arc sizing;
    /// the center text is supplied independently (already in the user's unit).
    /// </summary>
    public class CompositionDonutDrawable : IDrawable
    {
        // Fixed segment colors — kept identical to the XAML legend swatches.
        private static readonly Color FatColor = Color.FromArgb("#F4A23B");
        private static readonly Color MuscleColor = Color.FromArgb("#3FB6A8");
        private static readonly Color BoneColor = Color.FromArgb("#6C8AE4");
        private static readonly Color OtherColor = Color.FromArgb("#B0B6BE");

        public double Fat { get; set; }
        public double Muscle { get; set; }
        public double Bone { get; set; }
        public double Other { get; set; }

        public string CenterText { get; set; } = string.Empty;
        public string CenterSubText { get; set; } = string.Empty;

        public Color TextColor { get; set; } = Color.FromArgb("#202124");

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            double total = Safe(Fat) + Safe(Muscle) + Safe(Bone) + Safe(Other);
            if (total <= 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
                return;

            canvas.Antialias = true;
            canvas.SaveState();

            float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            float cx = dirtyRect.Center.X;
            float cy = dirtyRect.Center.Y;

            float ringThickness = size * 0.20f;
            // Radius leaves a small margin so the stroke is not clipped at the edge.
            float radius = (size - ringThickness) / 2f - 2f;
            if (radius <= 0)
            {
                canvas.RestoreState();
                return;
            }

            var arcRect = new RectF(cx - radius, cy - radius, radius * 2f, radius * 2f);

            var segments = new (double Value, Color Color)[]
            {
                (Safe(Fat), FatColor),
                (Safe(Muscle), MuscleColor),
                (Safe(Bone), BoneColor),
                (Safe(Other), OtherColor),
            };

            int activeCount = 0;
            foreach (var s in segments)
                if (s.Value > 0) activeCount++;

            // Gap between adjacent segments (degrees), centered on each boundary.
            // With butt caps the drawn arc equals its sweep exactly, so the gap stays visible.
            const float gapDeg = 2.5f;
            bool useGap = activeCount > 1;

            canvas.StrokeSize = ringThickness;
            canvas.StrokeLineCap = LineCap.Butt;

            // Maui.Graphics angles: 0 = 3 o'clock, counter-clockwise positive.
            // We want to start at 12 o'clock (90) and sweep clockwise (decreasing angle).
            float cursor = 90f;
            foreach (var s in segments)
            {
                if (s.Value <= 0)
                    continue;

                // Full proportional segment; the cursor always advances by this so the
                // ring stays a true 360° breakdown.
                float segmentSweep = (float)(s.Value / total) * 360f;

                // Carve the gap out of the drawn portion (gap/2 inset on each side).
                // Clamp to a tiny positive sweep so a thin segment still renders.
                float drawnSweep = useGap ? segmentSweep - gapDeg : segmentSweep;
                if (drawnSweep <= 0f)
                    drawnSweep = 0.5f;

                float gapHalf = (segmentSweep - drawnSweep) / 2f;
                float startAngle = cursor - gapHalf;
                float endAngle = startAngle - drawnSweep;

                canvas.StrokeColor = s.Color;
                canvas.DrawArc(arcRect, startAngle, endAngle, true, false);

                cursor -= segmentSweep;
            }

            // Center labels. The big number sits slightly above center when a unit follows.
            canvas.FontColor = TextColor;
            bool hasSub = !string.IsNullOrEmpty(CenterSubText);
            float numShift = hasSub ? size * 0.06f : 0f;

            if (!string.IsNullOrEmpty(CenterText))
            {
                canvas.FontSize = size * 0.20f;
                canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                canvas.DrawString(
                    CenterText,
                    cx - radius, cy - radius - numShift,
                    radius * 2f, radius * 2f,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center,
                    TextFlow.ClipBounds);
            }

            if (hasSub)
            {
                canvas.FontSize = size * 0.10f;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                float subOffset = size * 0.17f;
                canvas.DrawString(
                    CenterSubText,
                    cx - radius, cy - radius + subOffset,
                    radius * 2f, radius * 2f,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center,
                    TextFlow.ClipBounds);
            }

            canvas.RestoreState();
        }

        private static double Safe(double v) => double.IsNaN(v) || double.IsInfinity(v) || v < 0 ? 0 : v;
    }
}
