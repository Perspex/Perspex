using System;
using System.Globalization;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A class that is responsible for text shaping.
    /// </summary>
    public class TextShaper
    {
        private readonly ITextShaperImpl _platformImpl;

        public TextShaper(ITextShaperImpl platformImpl)
        {
            _platformImpl = platformImpl;
        }

        /// <summary>
        /// Gets the current text shaper.
        /// </summary>
        public static TextShaper Current
        {
            get
            {
                var current = AvaloniaLocator.Current.GetService<TextShaper>();

                if (current != null)
                {
                    return current;
                }

                var textShaperImpl = AvaloniaLocator.Current.GetService<ITextShaperImpl>();

                if (textShaperImpl == null)
                    throw new InvalidOperationException("No text shaper implementation was registered.");

                current = new TextShaper(textShaperImpl);

                AvaloniaLocator.CurrentMutable.Bind<TextShaper>().ToConstant(current);

                return current;
            }
        }

        /// <inheritdoc cref="ITextShaperImpl.ShapeText"/>
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize,
            CultureInfo culture)
        {
            return _platformImpl.ShapeText(text, typeface, fontRenderingEmSize, culture);
        }
    }
}
