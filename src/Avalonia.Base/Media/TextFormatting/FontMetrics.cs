namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A metric that holds information about font specific measurements.
    /// </summary>
    public readonly struct FontMetrics
    {
        public FontMetrics(Typeface typeface, double fontSize)
        {
            var glyphTypeface = typeface.GlyphTypeface;

            var scale = fontSize / glyphTypeface.DesignEmHeight;

            Ascent = glyphTypeface.Ascent * scale;

            Descent = glyphTypeface.Descent * scale;

            LineGap = glyphTypeface.LineGap * scale;

            LineHeight = Descent - Ascent + LineGap;

            UnderlineThickness = glyphTypeface.UnderlineThickness * scale;

            UnderlinePosition = glyphTypeface.UnderlinePosition * scale;

            StrikethroughThickness = glyphTypeface.StrikethroughThickness * scale;

            StrikethroughPosition = glyphTypeface.StrikethroughPosition * scale;
        }

        /// <summary>
        /// Gets the recommended distance above the baseline.
        /// </summary>
        public double Ascent { get; }

        /// <summary>
        /// Gets the recommended distance under the baseline.
        /// </summary>
        public double Descent { get; }

        /// <summary>
        /// Gets the recommended additional space between two lines of text.
        /// </summary>
        public double LineGap { get; }

        /// <summary>
        /// Gets the estimated line height.
        /// </summary>
        public double LineHeight { get; }

        /// <summary>
        /// Gets a value that indicates the thickness of the underline.
        /// </summary>
        public double UnderlineThickness { get; }

        /// <summary>
        /// Gets a value that indicates the distance of the underline from the baseline.
        /// </summary>
        public double UnderlinePosition { get; }

        /// <summary>
        /// Gets a value that indicates the thickness of the underline.
        /// </summary>
        public double StrikethroughThickness { get; }

        /// <summary>
        /// Gets a value that indicates the distance of the strikethrough from the baseline.
        /// </summary>
        public double StrikethroughPosition { get; }
    }
}
