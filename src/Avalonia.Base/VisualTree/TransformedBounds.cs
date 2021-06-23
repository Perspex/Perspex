using System;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Holds information about the bounds of a control, together with a transform and a clip.
    /// </summary>
    public readonly struct TransformedBounds : IEquatable<TransformedBounds>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformedBounds"/> struct.
        /// </summary>
        /// <param name="bounds">The control's bounds.</param>
        /// <param name="clip">The control's clip rectangle.</param>
        /// <param name="transform">The control's transform.</param>
        public TransformedBounds(Rect bounds, Rect clip, Matrix transform)
        {
            Bounds = bounds;
            Clip = clip;
            Transform = transform;
        }

        /// <summary>
        /// Gets the control's bounds in its local coordinate space.
        /// </summary>
        public Rect Bounds { get; }

        /// <summary>
        /// Gets the control's clip rectangle in global coordinate space.
        /// </summary>
        public Rect Clip { get; }

        /// <summary>
        /// Gets the transform from local to global coordinate space.
        /// </summary>
        public Matrix Transform { get; }

        public bool Contains(Point point)
        {
            if (Transform.HasInverse)
            {
                Point trPoint = point * Transform.Invert();

                return Bounds.Contains(trPoint);
            }
            else
            {
                return Bounds.Contains(point);
            }
        }

        public bool Equals(TransformedBounds other)
        {
            return Bounds == other.Bounds && Clip == other.Clip && Transform == other.Transform;
        }

        public override bool Equals(object obj) => obj is TransformedBounds other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Bounds.GetHashCode();
                hashCode = (hashCode * 397) ^ Clip.GetHashCode();
                hashCode = (hashCode * 397) ^ Transform.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TransformedBounds left, TransformedBounds right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransformedBounds left, TransformedBounds right)
        {
            return !left.Equals(right);
        }

        public override string ToString() => $"Bounds: {Bounds} Clip: {Clip} Transform {Transform}";
    }
}
