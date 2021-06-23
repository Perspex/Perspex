using System;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Represents the radii of a rectangle's corners.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct CornerRadius : IEquatable<CornerRadius>
    {
        static CornerRadius()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<CornerRadiusAnimator>(prop => typeof(CornerRadius).IsAssignableFrom(prop.PropertyType));
#endif
        }

        public CornerRadius(double uniformRadius)
        {
            TopLeft = TopRight = BottomLeft = BottomRight = uniformRadius;

        }
        public CornerRadius(double top, double bottom)
        {
            TopLeft = TopRight = top;
            BottomLeft = BottomRight = bottom;
        }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        /// <summary>
        /// Radius of the top left corner.
        /// </summary>
        public double TopLeft { get; }

        /// <summary>
        /// Radius of the top right corner.
        /// </summary>
        public double TopRight { get; }

        /// <summary>
        /// Radius of the bottom right corner.
        /// </summary>
        public double BottomRight { get; }

        /// <summary>
        /// Radius of the bottom left corner.
        /// </summary>
        public double BottomLeft { get; }

        /// <summary>
        /// Gets a value indicating whether all corner radii are set to 0.
        /// </summary>
        public bool IsEmpty => TopLeft.Equals(0) && IsUniform;

        /// <summary>
        /// Gets a value indicating whether all corner radii are equal.
        /// </summary>
        public bool IsUniform => TopLeft.Equals(TopRight) && BottomLeft.Equals(BottomRight) && TopRight.Equals(BottomRight);

        /// <summary>
        /// Returns a boolean indicating whether the corner radius is equal to the other given corner radius.
        /// </summary>
        /// <param name="other">The other corner radius to test equality against.</param>
        /// <returns>True if this corner radius is equal to other; False otherwise.</returns>
        public bool Equals(CornerRadius other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return TopLeft == other.TopLeft &&
                   
                   TopRight == other.TopRight &&
                   BottomRight == other.BottomRight &&
                   BottomLeft == other.BottomLeft;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this corner radius instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this corner radius; False otherwise.</returns>
        public override bool Equals(object obj) => obj is CornerRadius other && Equals(other);

        public override int GetHashCode()
        {
            return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomLeft.GetHashCode() ^ BottomRight.GetHashCode();
        }

        public override string ToString()
        {
            return $"{TopLeft},{TopRight},{BottomRight},{BottomLeft}";
        }

        public static CornerRadius Parse(string s)
        {
            const string exceptionMessage = "Invalid CornerRadius.";

            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage))
            {
                if (tokenizer.TryReadDouble(out var a))
                {
                    if (tokenizer.TryReadDouble(out var b))
                    {
                        if (tokenizer.TryReadDouble(out var c))
                        {
                            return new CornerRadius(a, b, c, tokenizer.ReadDouble());
                        }

                        return new CornerRadius(a, b);
                    }

                    return new CornerRadius(a);
                }

                throw new FormatException(exceptionMessage);
            }
        }

        public static bool operator ==(CornerRadius left, CornerRadius right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CornerRadius left, CornerRadius right)
        {
            return !(left == right);
        }
    }
}
