using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Defines a rectangle that may be defined relative to a containing element.
    /// </summary>
    public readonly struct RelativeRect : IEquatable<RelativeRect>
    {
        private static readonly char[] PercentChar = { '%' };

        /// <summary>
        /// A rectangle that represents 100% of an area.
        /// </summary>
        public static readonly RelativeRect Fill = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative);

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(double x, double y, double width, double height, RelativeUnit unit)
        {
            Rect = new Rect(x, y, width, height);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Rect rect, RelativeUnit unit)
        {
            Rect = rect;
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Size size, RelativeUnit unit)
        {
            Rect = new Rect(size);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point position, Size size, RelativeUnit unit)
        {
            Rect = new Rect(position, size);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point topLeft, Point bottomRight, RelativeUnit unit)
        {
            Rect = new Rect(topLeft, bottomRight);
            Unit = unit;
        }

        /// <summary>
        /// Gets the unit of the rectangle.
        /// </summary>
        public RelativeUnit Unit { get; }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// Checks for equality between two <see cref="RelativeRect"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are equal; otherwise false.</returns>
        public static bool operator ==(RelativeRect left, RelativeRect right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for inequality between two <see cref="RelativeRect"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are unequal; otherwise false.</returns>
        public static bool operator !=(RelativeRect left, RelativeRect right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks if the <see cref="RelativeRect"/> equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object obj) => obj is RelativeRect other && Equals(other);

        /// <summary>
        /// Checks if the <see cref="RelativeRect"/> equals another rectangle.
        /// </summary>
        /// <param name="p">The other rectangle.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public bool Equals(RelativeRect p)
        {
            return Unit == p.Unit && Rect == p.Rect;
        }

        /// <summary>
        /// Gets a hashcode for a <see cref="RelativeRect"/>.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Unit * 397) ^ Rect.GetHashCode();
            }
        }

        /// <summary>
        /// Converts a <see cref="RelativeRect"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Rect ToPixels(Size size)
        {
            return Unit == RelativeUnit.Absolute ?
                Rect :
                new Rect(
                    Rect.X * size.Width,
                    Rect.Y * size.Height,
                    Rect.Width * size.Width,
                    Rect.Height * size.Height);
        }

        /// <summary>
        /// Parses a <see cref="RelativeRect"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed <see cref="RelativeRect"/>.</returns>
        public static RelativeRect Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, exceptionMessage: "Invalid RelativeRect."))
            {
                var x = tokenizer.ReadString();
                var y = tokenizer.ReadString();
                var width = tokenizer.ReadString();
                var height = tokenizer.ReadString();

                var unit = RelativeUnit.Absolute;
                var scale = 1.0;

                var xRelative = x.EndsWith("%", StringComparison.Ordinal);
                var yRelative = y.EndsWith("%", StringComparison.Ordinal);
                var widthRelative = width.EndsWith("%", StringComparison.Ordinal);
                var heightRelative = height.EndsWith("%", StringComparison.Ordinal);

                if (xRelative && yRelative && widthRelative && heightRelative)
                {
                    x = x.TrimEnd(PercentChar);
                    y = y.TrimEnd(PercentChar);
                    width = width.TrimEnd(PercentChar);
                    height = height.TrimEnd(PercentChar);

                    unit = RelativeUnit.Relative;
                    scale = 0.01;
                }
                else if (xRelative || yRelative || widthRelative || heightRelative)
                {
                    throw new FormatException("If one coordinate is relative, all must be.");
                }

                return new RelativeRect(
                    double.Parse(x, CultureInfo.InvariantCulture) * scale,
                    double.Parse(y, CultureInfo.InvariantCulture) * scale,
                    double.Parse(width, CultureInfo.InvariantCulture) * scale,
                    double.Parse(height, CultureInfo.InvariantCulture) * scale,
                    unit);
            }
        }
    }
}
