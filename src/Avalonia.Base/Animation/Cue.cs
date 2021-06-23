using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation
{
    /// <summary>
    /// Determines the time index for a <see cref="KeyFrame"/>. 
    /// </summary>
    [TypeConverter(typeof(CueTypeConverter))]
    public readonly struct Cue : IEquatable<Cue>, IEquatable<double>
    {
        /// <summary>
        /// The normalized percent value, ranging from 0.0 to 1.0
        /// </summary>
        public double CueValue { get; }

        /// <summary>
        /// Sets a new <see cref="Cue"/> object.
        /// </summary>
        /// <param name="value"></param>
        public Cue(double value)
        {
            if (value <= 1 && value >= 0)
                CueValue = value;
            else
                throw new ArgumentException($"This cue object's value should be within or equal to 0.0 and 1.0");
        }

        /// <summary>
        /// Parses a string to a <see cref="Cue"/> object.
        /// </summary>
        public static Cue Parse(string value, CultureInfo culture)
        {
            string v = value;

            if (value.EndsWith("%"))
            {
                v = v.TrimEnd('%');
            }

            if (double.TryParse(v, NumberStyles.Float, culture, out double res))
            {
                return new Cue(res / 100d);
            }
            else
            {
                throw new FormatException($"Invalid Cue string \"{value}\"");
            }
        }

        /// <summary>
        /// Checks for equality between two <see cref="Cue"/>s.
        /// </summary>
        /// <param name="other">The second cue.</param>
        public bool Equals(Cue other)
        {
            return CueValue == other.CueValue;
        }

        /// <summary>
        /// Checks for equality between a <see cref="Cue"/>
        /// and a <see cref="double"/> value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(double other)
        {
            return CueValue == other;
        }
    }

    public class CueTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Cue.Parse((string)value, culture);
        }
    }
}