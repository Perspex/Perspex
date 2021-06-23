using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Represents a rectangle in device pixels.
    /// </summary>
    public readonly struct PixelRect : IEquatable<PixelRect>
    {
        /// <summary>
        /// An empty rectangle.
        /// </summary>
        public static readonly PixelRect Empty = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelRect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public PixelRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelRect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        public PixelRect(PixelSize size)
        {
            X = 0;
            Y = 0;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelRect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        public PixelRect(PixelPoint position, PixelSize size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelRect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        public PixelRect(PixelPoint topLeft, PixelPoint bottomRight)
        {
            X = topLeft.X;
            Y = topLeft.Y;
            Width = bottomRight.X - topLeft.X;
            Height = bottomRight.Y - topLeft.Y;
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the position of the rectangle.
        /// </summary>
        public PixelPoint Position => new PixelPoint(X, Y);

        /// <summary>
        /// Gets the size of the rectangle.
        /// </summary>
        public PixelSize Size => new PixelSize(Width, Height);

        /// <summary>
        /// Gets the right position of the rectangle.
        /// </summary>
        public int Right => X + Width;

        /// <summary>
        /// Gets the bottom position of the rectangle.
        /// </summary>
        public int Bottom => Y + Height;

        /// <summary>
        /// Gets the top left point of the rectangle.
        /// </summary>
        public PixelPoint TopLeft => new PixelPoint(X, Y);

        /// <summary>
        /// Gets the top right point of the rectangle.
        /// </summary>
        public PixelPoint TopRight => new PixelPoint(Right, Y);

        /// <summary>
        /// Gets the bottom left point of the rectangle.
        /// </summary>
        public PixelPoint BottomLeft => new PixelPoint(X, Bottom);

        /// <summary>
        /// Gets the bottom right point of the rectangle.
        /// </summary>
        public PixelPoint BottomRight => new PixelPoint(Right, Bottom);

        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public PixelPoint Center => new PixelPoint(X + (Width / 2), Y + (Height / 2));

        /// <summary>
        /// Gets a value that indicates whether the rectangle is empty.
        /// </summary>
        public bool IsEmpty => Width == 0 && Height == 0;

        /// <summary>
        /// Checks for equality between two <see cref="PixelRect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are equal; otherwise false.</returns>
        public static bool operator ==(PixelRect left, PixelRect right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for inequality between two <see cref="PixelRect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are unequal; otherwise false.</returns>
        public static bool operator !=(PixelRect left, PixelRect right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether a point in in the bounds of the rectangle.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>
        public bool Contains(PixelPoint p)
        {
            return p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;
        }

        /// <summary>
        /// Determines whether the rectangle fully contains another rectangle.
        /// </summary>
        /// <param name="r">The rectangle.</param>
        /// <returns>true if the rectangle is fully contained; otherwise false.</returns>
        public bool Contains(PixelRect r)
        {
            return Contains(r.TopLeft) && Contains(r.BottomRight);
        }

        /// <summary>
        /// Centers another rectangle in this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to center.</param>
        /// <returns>The centered rectangle.</returns>
        public PixelRect CenterRect(PixelRect rect)
        {
            return new PixelRect(
                X + ((Width - rect.Width) / 2),
                Y + ((Height - rect.Height) / 2),
                rect.Width,
                rect.Height);
        }

        /// <summary>
        /// Returns a boolean indicating whether the rect is equal to the other given rect.
        /// </summary>
        /// <param name="other">The other rect to test equality against.</param>
        /// <returns>True if this rect is equal to other; False otherwise.</returns>
        public bool Equals(PixelRect other)
        {
            return Position == other.Position && Size == other.Size;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given object is equal to this rectangle.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>True if the object is equal to this rectangle; false otherwise.</returns>
        public override bool Equals(object obj) => obj is PixelRect other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + X.GetHashCode();
                hash = (hash * 23) + Y.GetHashCode();
                hash = (hash * 23) + Width.GetHashCode();
                hash = (hash * 23) + Height.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Gets the intersection of two rectangles.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>The intersection.</returns>
        public PixelRect Intersect(PixelRect rect)
        {
            var newLeft = (rect.X > X) ? rect.X : X;
            var newTop = (rect.Y > Y) ? rect.Y : Y;
            var newRight = (rect.Right < Right) ? rect.Right : Right;
            var newBottom = (rect.Bottom < Bottom) ? rect.Bottom : Bottom;

            if ((newRight > newLeft) && (newBottom > newTop))
            {
                return new PixelRect(newLeft, newTop, newRight - newLeft, newBottom - newTop);
            }
            else
            {
                return Empty;
            }
        }

        /// <summary>
        /// Determines whether a rectangle intersects with this rectangle.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>
        /// True if the specified rectangle intersects with this one; otherwise false.
        /// </returns>
        public bool Intersects(PixelRect rect)
        {
            return (rect.X < Right) && (X < rect.Right) && (rect.Y < Bottom) && (Y < rect.Bottom);
        }
        
        /// <summary>
        /// Translates the rectangle by an offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>The translated rectangle.</returns>
        public PixelRect Translate(PixelVector offset)
        {
            return new PixelRect(Position + offset, Size);
        }

        /// <summary>
        /// Gets the union of two rectangles.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>The union.</returns>
        public PixelRect Union(PixelRect rect)
        {
            if (IsEmpty)
            {
                return rect;
            }
            else if (rect.IsEmpty)
            {
                return this;
            }
            else
            {
                var x1 = Math.Min(X, rect.X);
                var x2 = Math.Max(Right, rect.Right);
                var y1 = Math.Min(Y, rect.Y);
                var y2 = Math.Max(Bottom, rect.Bottom);

                return new PixelRect(new PixelPoint(x1, y1), new PixelPoint(x2, y2));
            }
        }

        /// <summary>
        /// Returns a new <see cref="PixelRect"/> with the specified X position.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <returns>The new <see cref="PixelRect"/>.</returns>
        public PixelRect WithX(int x)
        {
            return new PixelRect(x, Y, Width, Height);
        }

        /// <summary>
        /// Returns a new <see cref="PixelRect"/> with the specified Y position.
        /// </summary>
        /// <param name="y">The y position.</param>
        /// <returns>The new <see cref="PixelRect"/>.</returns>
        public PixelRect WithY(int y)
        {
            return new PixelRect(X, y, Width, Height);
        }

        /// <summary>
        /// Returns a new <see cref="PixelRect"/> with the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <returns>The new <see cref="PixelRect"/>.</returns>
        public PixelRect WithWidth(int width)
        {
            return new PixelRect(X, Y, width, Height);
        }

        /// <summary>
        /// Returns a new <see cref="PixelRect"/> with the specified height.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns>The new <see cref="PixelRect"/>.</returns>
        public PixelRect WithHeight(int height)
        {
            return new PixelRect(X, Y, Width, Height);
        }

        /// <summary>
        /// Converts the <see cref="PixelRect"/> to a device-independent <see cref="Rect"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent rect.</returns>
        public Rect ToRect(double scale) => new Rect(Position.ToPoint(scale), Size.ToSize(scale));

        /// <summary>
        /// Converts the <see cref="PixelRect"/> to a device-independent <see cref="Rect"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent rect.</returns>
        public Rect ToRect(Vector scale) => new Rect(Position.ToPoint(scale), Size.ToSize(scale));

        /// <summary>
        /// Converts the <see cref="PixelRect"/> to a device-independent <see cref="Rect"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent rect.</returns>
        public Rect ToRectWithDpi(double dpi) => new Rect(Position.ToPointWithDpi(dpi), Size.ToSizeWithDpi(dpi));

        /// <summary>
        /// Converts the <see cref="PixelRect"/> to a device-independent <see cref="Rect"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent rect.</returns>
        public Rect ToRectWithDpi(Vector dpi) => new Rect(Position.ToPointWithDpi(dpi), Size.ToSizeWithDpi(dpi));

        /// <summary>
        /// Converts a <see cref="Rect"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent rect.</returns>
        public static PixelRect FromRect(Rect rect, double scale) => new PixelRect(
            PixelPoint.FromPoint(rect.Position, scale),
            FromPointCeiling(rect.BottomRight, new Vector(scale, scale)));

        /// <summary>
        /// Converts a <see cref="Rect"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelRect FromRect(Rect rect, Vector scale) => new PixelRect(
            PixelPoint.FromPoint(rect.Position, scale),
            FromPointCeiling(rect.BottomRight, scale));

        /// <summary>
        /// Converts a <see cref="Rect"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelRect FromRectWithDpi(Rect rect, double dpi) => new PixelRect(
            PixelPoint.FromPointWithDpi(rect.Position, dpi),
            FromPointCeiling(rect.BottomRight, new Vector(dpi / 96, dpi / 96)));

        /// <summary>
        /// Converts a <see cref="Rect"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelRect FromRectWithDpi(Rect rect, Vector dpi) => new PixelRect(
            PixelPoint.FromPointWithDpi(rect.Position, dpi),
            FromPointCeiling(rect.BottomRight, dpi / 96));

        /// <summary>
        /// Returns the string representation of the rectangle.
        /// </summary>
        /// <returns>The string representation of the rectangle.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2}, {3}",
                X,
                Y,
                Width,
                Height);
        }

        /// <summary>
        /// Parses a <see cref="PixelRect"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed <see cref="PixelRect"/>.</returns>
        public static PixelRect Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PixelRect."))
            {
                return new PixelRect(
                    tokenizer.ReadInt32(),
                    tokenizer.ReadInt32(),
                    tokenizer.ReadInt32(),
                    tokenizer.ReadInt32()
                );
            }
        }

        private static PixelPoint FromPointCeiling(Point point, Vector scale)
        {
            return new PixelPoint(
                (int)Math.Ceiling(point.X * scale.X),
                (int)Math.Ceiling(point.Y * scale.Y));
        }
    }
}
