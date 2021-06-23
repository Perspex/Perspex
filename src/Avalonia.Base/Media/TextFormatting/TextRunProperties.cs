using System;
using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Provides a set of properties, such as typeface or foreground brush, that can be applied to a TextRun object. This is an abstract class.
    /// </summary>
    /// <remarks>
    /// The text layout client provides a concrete implementation of this abstract class.
    /// This enables the client to implement text run properties in a way that corresponds with the associated formatting store.
    /// </remarks>
    public abstract class TextRunProperties : IEquatable<TextRunProperties>
    {
        /// <summary>
        /// Run typeface
        /// </summary>
        public abstract Typeface Typeface { get; }

        /// <summary>
        /// Em size of font used to format and display text
        /// </summary>
        public abstract double FontRenderingEmSize { get; }

        ///<summary>
        /// Run TextDecorations. 
        ///</summary>
        public abstract TextDecorationCollection TextDecorations { get; }

        /// <summary>
        /// Brush used to fill text.
        /// </summary>
        public abstract IBrush ForegroundBrush { get; }

        /// <summary>
        /// Brush used to paint background of run.
        /// </summary>
        public abstract IBrush BackgroundBrush { get; }

        /// <summary>
        /// Run text culture.
        /// </summary>
        public abstract CultureInfo CultureInfo { get; }

        /// <summary>
        /// Run vertical box alignment
        /// </summary>
        public abstract BaselineAlignment BaselineAlignment { get; }

        public bool Equals(TextRunProperties other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Typeface.Equals(other.Typeface) &&
                   FontRenderingEmSize.Equals(other.FontRenderingEmSize)
                   && Equals(TextDecorations, other.TextDecorations) &&
                   Equals(ForegroundBrush, other.ForegroundBrush) &&
                   Equals(BackgroundBrush, other.BackgroundBrush) &&
                   Equals(CultureInfo, other.CultureInfo);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is TextRunProperties other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Typeface.GetHashCode();
                hashCode = (hashCode * 397) ^ FontRenderingEmSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (TextDecorations != null ? TextDecorations.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ForegroundBrush != null ? ForegroundBrush.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BackgroundBrush != null ? BackgroundBrush.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CultureInfo != null ? CultureInfo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TextRunProperties left, TextRunProperties right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextRunProperties left, TextRunProperties right)
        {
            return !Equals(left, right);
        }
    }
}
