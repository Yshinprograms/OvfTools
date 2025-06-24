// ColorGenerator.cs

using netDxf;
using System;

namespace OvfAnnotator {
    /// <summary>
    /// Generates a sequence of high-contrast colors using an interleaved,
    /// staggered hue selection method for maximum visual separation.
    /// </summary>
    public class ColorGenerator {
        private int _blockIndex;
        private readonly double _saturation;
        private readonly double _value;

        // The angular separation for our interleaved color wheels.
        // 72 degrees is a great choice (360 / 5).
        private const double HUE_INCREMENT = 72.0;

        public ColorGenerator() {
            _blockIndex = 0;
            _saturation = 0.95; // A tiny bit more saturation for vibrant colors
            _value = 1.0;
        }

        /// <summary>
        /// Gets the next high-contrast color in the sequence.
        /// </summary>
        public AciColor GetNextColor() {
            double hue;

            // This is your brilliant algorithm in code!
            // We treat even and odd blocks differently.
            if (_blockIndex % 2 == 0) {
                // EVEN blocks (0, 2, 4...) start at hue 0 and increment.
                hue = (Math.Floor(_blockIndex / 2.0) * HUE_INCREMENT);
            } else {
                // ODD blocks (1, 3, 5...) start at hue 180 and increment.
                hue = (180.0 + Math.Floor(_blockIndex / 2.0) * HUE_INCREMENT);
            }

            // Ensure hue wraps around the 360-degree circle.
            hue %= 360.0;

            (byte r, byte g, byte b) = HsvToRgb(hue, _saturation, _value);
            var aciColor = new AciColor(r, g, b);

            // Increment the index for the next call.
            _blockIndex++;

            return aciColor;
        }

        private (byte, byte, byte) HsvToRgb(double h, double s, double v) {
            int i;
            double f, p, q, t;
            double r, g, b;

            if (s == 0) { r = g = b = v; return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255)); }

            h /= 60;
            i = (int)Math.Floor(h);
            f = h - i;
            p = v * (1 - s);
            q = v * (1 - s * f);
            t = v * (1 - s * (1 - f));

            switch (i) {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}