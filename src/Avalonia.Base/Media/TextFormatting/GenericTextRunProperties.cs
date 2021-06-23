using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    public class GenericTextRunProperties : TextRunProperties
    {
        private const double DefaultFontRenderingEmSize = 12;

        public GenericTextRunProperties(Typeface typeface, double fontRenderingEmSize = DefaultFontRenderingEmSize,
            TextDecorationCollection textDecorations = null, IBrush foregroundBrush = null,
            IBrush backgroundBrush = null, BaselineAlignment baselineAlignment = BaselineAlignment.Baseline,
            CultureInfo cultureInfo = null)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            TextDecorations = textDecorations;
            ForegroundBrush = foregroundBrush;
            BackgroundBrush = backgroundBrush;
            BaselineAlignment = baselineAlignment;
            CultureInfo = cultureInfo;
        }

        /// <inheritdoc />
        public override Typeface Typeface { get; }

        /// <inheritdoc />
        public override double FontRenderingEmSize { get; }

        /// <inheritdoc />
        public override TextDecorationCollection TextDecorations { get; }

        /// <inheritdoc />
        public override IBrush ForegroundBrush { get; }

        /// <inheritdoc />
        public override IBrush BackgroundBrush { get; }

        /// <inheritdoc />
        public override BaselineAlignment BaselineAlignment { get; }

        /// <inheritdoc />
        public override CultureInfo CultureInfo { get; }
    }
}
