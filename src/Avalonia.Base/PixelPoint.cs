using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Represents a point in device pixels.
    /// </summary>
    public readonly struct PixelPoint : IEquatable<PixelPoint>
    {
        /// <summary>
        /// A point representing 0,0.
        /// </summary>
        public static readonly PixelPoint Origin = new PixelPoint(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelPoint"/> structure.
        /// </summary>
        /// <param name="x">The X co-ordinate.</param>
        /// <param name="y">The Y co-ordinate.</param>
        public PixelPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the X co-ordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y co-ordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Checks for equality between two <see cref="PixelPoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are equal; otherwise false.</returns>
        public static bool operator ==(PixelPoint left, PixelPoint right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for inequality between two <see cref="PixelPoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are unequal; otherwise false.</returns>
        public static bool operator !=(PixelPoint left, PixelPoint right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// Converts the <see cref="Point"/> to a <see cref="Vector"/>.
        /// </summary>
        /// <param name="p">The point.</param>
        public static implicit operator PixelVector(PixelPoint p)
        {
            return new PixelVector(p.X, p.Y);
        }
        
        /// <summary>
        /// Adds two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>A point that is the result of the addition.</returns>
        public static PixelPoint operator +(PixelPoint a, PixelPoint b)
        {
            return new PixelPoint(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Adds a vector to a point.
        /// </summary>
        /// <param name="a">The point.</param>
        /// <param name="b">The vector.</param>
        /// <returns>A point that is the result of the addition.</returns>
        public static PixelPoint operator +(PixelPoint a, PixelVector b)
        {
            return new PixelPoint(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Subtracts two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>A point that is the result of the subtraction.</returns>
        public static PixelPoint operator -(PixelPoint a, PixelPoint b)
        {
            return new PixelPoint(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Subtracts a vector from a point.
        /// </summary>
        /// <param name="a">The point.</param>
        /// <param name="b">The vector.</param>
        /// <returns>A point that is the result of the subtraction.</returns>
        public static PixelPoint operator -(PixelPoint a, PixelVector b)
        {
            return new PixelPoint(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Parses a <see cref="PixelPoint"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="PixelPoint"/>.</returns>
        public static PixelPoint Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PixelPoint."))
            {
                return new PixelPoint(
                    tokenizer.ReadInt32(),
                    tokenizer.ReadInt32());
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the point is equal to the other given point.
        /// </summary>
        /// <param name="other">The other point to test equality against.</param>
        /// <returns>True if this point is equal to other; False otherwise.</returns>
        public bool Equals(PixelPoint other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return X == other.X && Y == other.Y;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Checks for equality between a point and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a point that equals the current point.
        /// </returns>
        public override bool Equals(object obj) => obj is PixelPoint other && Equals(other);

        /// <summary>
        /// Returns a hash code for a <see cref="PixelPoint"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + X.GetHashCode();
                hash = (hash * 23) + Y.GetHashCode();
                return hash;
            }
        }
        
        /// <summary>
        /// Returns a new <see cref="PixelPoint"/> with the same Y co-ordinate and the specified X co-ordinate.
        /// </summary>
        /// <param name="x">The X co-ordinate.</param>
        /// <returns>The new <see cref="PixelPoint"/>.</returns>
        public PixelPoint WithX(int x) => new PixelPoint(x, Y);

        /// <summary>
        /// Returns a new <see cref="PixelPoint"/> with the same X co-ordinate and the specified Y co-ordinate.
        /// </summary>
        /// <param name="y">The Y co-ordinate.</param>
        /// <returns>The new <see cref="PixelPoint"/>.</returns>
        public PixelPoint WithY(int y) => new PixelPoint(X, y);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPoint(double scale) => new Point(X / scale, Y / scale);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPoint(Vector scale) => new Point(X / scale.X, Y / scale.Y);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPointWithDpi(double dpi) => ToPoint(dpi / 96);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPointWithDpi(Vector dpi) => ToPoint(new Vector(dpi.X / 96, dpi.Y / 96));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPoint(Point point, double scale) => new PixelPoint(
            (int)(point.X * scale),
            (int)(point.Y * scale));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPoint(Point point, Vector scale) => new PixelPoint(
            (int)(point.X * scale.X),
            (int)(point.Y * scale.Y));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPointWithDpi(Point point, double dpi) => FromPoint(point, dpi / 96);

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPointWithDpi(Point point, Vector dpi) => FromPoint(point, new Vector(dpi.X / 96, dpi.Y / 96));

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>The string representation of the point.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", X, Y);
        }
    }
}
