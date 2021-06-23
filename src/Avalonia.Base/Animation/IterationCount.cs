using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines the valid modes for a <see cref="IterationCount"/>.
    /// </summary>
    public enum IterationType
    {
        Many,
        Infinite
    }

    /// <summary>
    /// Determines the number of iterations of an animation.
    /// Also defines its repeat behavior. 
    /// </summary>
    [TypeConverter(typeof(IterationCountTypeConverter))]
    public struct IterationCount : IEquatable<IterationCount>
    {
        private readonly IterationType _type;
        private readonly ulong _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="IterationCount"/> struct.
        /// </summary>
        /// <param name="value">The number of iterations of an animation.</param>
        public IterationCount(ulong value)
            : this(value, IterationType.Many)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IterationCount"/> struct.
        /// </summary>
        /// <param name="value">The size of the IterationCount.</param>
        /// <param name="type">The unit of the IterationCount.</param>
        public IterationCount(ulong value, IterationType type)
        {
            if (type > IterationType.Infinite)
            {
                throw new ArgumentException("Invalid value", nameof(type));
            }

            _type = type;
            _value = value;
        }

        /// <summary>
        /// Gets an instance of <see cref="IterationCount"/> that indicates that an animation
        /// should repeat forever.
        /// </summary>
        public static IterationCount Infinite => new IterationCount(0, IterationType.Infinite);

        /// <summary>
        /// Gets the unit of the <see cref="IterationCount"/>.
        /// </summary>
        public IterationType RepeatType => _type;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="IterationCount"/> is set to Infinite.
        /// </summary>
        public bool IsInfinite => _type == IterationType.Infinite;

        /// <summary>
        /// Gets the number of repeat iterations.
        /// </summary>
        public ulong Value => _value;

        /// <summary>
        /// Compares two IterationCount structures for equality.
        /// </summary>
        /// <param name="a">The first IterationCount.</param>
        /// <param name="b">The second IterationCount.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public static bool operator ==(IterationCount a, IterationCount b)
        {
            return (a.IsInfinite && b.IsInfinite)
                || (a._value == b._value && a._type == b._type);
        }

        /// <summary>
        /// Compares two IterationCount structures for inequality.
        /// </summary>
        /// <param name="rc1">The first IterationCount.</param>
        /// <param name="rc2">The first IterationCount.</param>
        /// <returns>True if the structures are unequal, otherwise false.</returns>
        public static bool operator !=(IterationCount rc1, IterationCount rc2)
        {
            return !(rc1 == rc2);
        }

        /// <summary>
        /// Determines whether the <see cref="IterationCount"/> is equal to the specified object.
        /// </summary>
        /// <param name="o">The object with which to test equality.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (!(o is IterationCount))
            {
                return false;
            }

            return this == (IterationCount)o;
        }

        /// <summary>
        /// Compares two IterationCount structures for equality.
        /// </summary>
        /// <param name="IterationCount">The structure with which to test equality.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public bool Equals(IterationCount IterationCount)
        {
            return this == IterationCount;
        }

        /// <summary>
        /// Gets a hash code for the IterationCount.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode() ^ _type.GetHashCode();
        }

        /// <summary>
        /// Gets a string representation of the <see cref="IterationCount"/>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            if (IsInfinite)
            {
                return "Infinite";
            }

            string s = _value.ToString();
            return s;
        }

        /// <summary>
        /// Parses a string to return a <see cref="IterationCount"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="IterationCount"/>.</returns>
        public static IterationCount Parse(string s)
        {
            s = s.ToUpperInvariant().Trim();

            if (s.EndsWith("INFINITE"))
            {
                return Infinite;
            }
            else
            {
                if (s.StartsWith("-"))
                    throw new InvalidCastException("IterationCount can't be a negative number.");

                var value = ulong.Parse(s, CultureInfo.InvariantCulture);

                return new IterationCount(value);
            }
        }
    }
}
