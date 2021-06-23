using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    [DebuggerDisplay("Name = {FontFamily.Name}, Weight = {Weight}, Style = {Style}")]
    public readonly struct Typeface : IEquatable<Typeface>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface([NotNull] FontFamily fontFamily,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Font weight must be > 0.");
            }

            FontFamily = fontFamily;
            Style = style;
            Weight = weight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(string fontFamilyName,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
            : this(new FontFamily(fontFamilyName), style, weight)
        {
        }

        public static Typeface Default { get; } = new Typeface(FontFamily.Default);

        /// <summary>
        /// Gets the font family.
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        public FontWeight Weight { get; }

        /// <summary>
        /// Gets the glyph typeface.
        /// </summary>
        /// <value>
        /// The glyph typeface.
        /// </value>
        public GlyphTypeface GlyphTypeface => FontManager.Current.GetOrAddGlyphTypeface(this);

        public static bool operator !=(Typeface a, Typeface b)
        {
            return !(a == b);
        }

        public static bool operator ==(Typeface a, Typeface b)
        {
            return  a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            return obj is Typeface typeface && Equals(typeface);
        }

        public bool Equals(Typeface other)
        {
            return FontFamily == other.FontFamily && Style == other.Style && Weight == other.Weight;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FontFamily != null ? FontFamily.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Style;
                hashCode = (hashCode * 397) ^ (int)Weight;
                return hashCode;
            }
        }
    }
}
