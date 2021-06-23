using System;

namespace Avalonia
{
    public struct RoundedRect
    {
        public bool Equals(RoundedRect other)
        {
            return Rect.Equals(other.Rect) && RadiiTopLeft.Equals(other.RadiiTopLeft) && RadiiTopRight.Equals(other.RadiiTopRight) && RadiiBottomLeft.Equals(other.RadiiBottomLeft) && RadiiBottomRight.Equals(other.RadiiBottomRight);
        }

        public override bool Equals(object obj)
        {
            return obj is RoundedRect other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Rect.GetHashCode();
                hashCode = (hashCode * 397) ^ RadiiTopLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ RadiiTopRight.GetHashCode();
                hashCode = (hashCode * 397) ^ RadiiBottomLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ RadiiBottomRight.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RoundedRect left, RoundedRect right) => left.Equals(right);

        public static bool operator !=(RoundedRect left, RoundedRect right) => !left.Equals(right);

        public Rect Rect { get; }
        public Vector RadiiTopLeft { get; }
        public Vector RadiiTopRight { get; }
        public Vector RadiiBottomLeft { get; }
        public Vector RadiiBottomRight { get; }
        
        public RoundedRect(Rect rect, Vector radiiTopLeft, Vector radiiTopRight, Vector radiiBottomRight, Vector radiiBottomLeft)
        {
            Rect = rect;
            RadiiTopLeft = radiiTopLeft;
            RadiiTopRight = radiiTopRight;
            RadiiBottomRight = radiiBottomRight;
            RadiiBottomLeft = radiiBottomLeft;
        }

        public RoundedRect(Rect rect, double radiusTopLeft, double radiusTopRight, double radiusBottomRight,
            double radiusBottomLeft)
            : this(rect,
                new Vector(radiusTopLeft, radiusTopLeft),
                new Vector(radiusTopRight, radiusTopRight),
                new Vector(radiusBottomRight, radiusBottomRight),
                new Vector(radiusBottomLeft, radiusBottomLeft)
            )
        {
            
        }

        public RoundedRect(Rect rect, Vector radii) : this(rect, radii, radii, radii, radii) 
        {
            
        }

        public RoundedRect(Rect rect, double radiusX, double radiusY) : this(rect, new Vector(radiusX, radiusY))
        {
            
        }

        public RoundedRect(Rect rect, double radius) : this(rect, radius, radius)
        {
            
        }

        public RoundedRect(Rect rect) : this(rect, 0)
        {
            
        }

        public RoundedRect(in Rect bounds, in CornerRadius radius) : this(bounds,
            radius.TopLeft, radius.TopRight,
            radius.BottomRight, radius.BottomLeft)
        {

        }

        public static implicit operator RoundedRect(Rect r) => new RoundedRect(r);

        public bool IsRounded => RadiiTopLeft != default || RadiiTopRight != default || RadiiBottomRight != default ||
                                 RadiiBottomLeft != default;

        public bool IsUniform =>
            RadiiTopLeft.Equals(RadiiTopRight) &&
            RadiiTopLeft.Equals(RadiiBottomRight) &&
            RadiiTopLeft.Equals(RadiiBottomLeft);

        public RoundedRect Inflate(double dx, double dy)
        {
            return Deflate(-dx, -dy);
        }
        
        public unsafe RoundedRect Deflate(double dx, double dy)
        {
            if (!IsRounded)
                return new RoundedRect(Rect.Deflate(new Thickness(dx, dy)));
            
            // Ported from SKRRect
            var left = Rect.X + dx;
            var top = Rect.Y + dy;
            var right = left + Rect.Width - dx * 2;
            var bottom = top + Rect.Height - dy * 2;
            var radii = stackalloc Vector[4];
            radii[0] = RadiiTopLeft;
            radii[1] = RadiiTopRight;
            radii[2] = RadiiBottomRight;
            radii[3] = RadiiBottomLeft;
            
            bool degenerate = false;
            if (right <= left) {
                degenerate = true;
                left = right = (left + right)*0.5;
            }
            if (bottom <= top) {
                degenerate = true;
                top = bottom = (top + bottom) * 0.5;
            }
            if (degenerate)
            {
                return new RoundedRect(new Rect(left, top, right - left, bottom - top));
            }

            for (var c = 0; c < 4; c++)
            {
                var rx = Math.Max(0, radii[c].X - dx);
                var ry = Math.Max(0, radii[c].Y - dy);
                if (rx == 0 || ry == 0)
                    radii[c] = default;
                else
                    radii[c] = new Vector(rx, ry);
            }

            return new RoundedRect(new Rect(left, top, right - left, bottom - top),
                radii[0], radii[1], radii[2], radii[3]);
        }
    }
}
