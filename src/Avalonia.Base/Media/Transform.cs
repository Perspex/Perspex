using System;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a transform on an <see cref="IVisual"/>.
    /// </summary>
    public abstract class Transform : Animatable, IMutableTransform
    {
        static Transform()
        {
            Animation.Animation.RegisterAnimator<TransformAnimator>(prop =>
                typeof(ITransform).IsAssignableFrom(prop.OwnerType));
        }

        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public abstract Matrix Value { get; }

        /// <summary>
        /// Parses a <see cref="Transform"/> string.
        /// </summary>
        /// <param name="s">Six comma-delimited double values that describe the new <see cref="Transform"/>. For details check <see cref="Matrix.Parse(string)"/> </param>
        /// <returns>The <see cref="Transform"/>.</returns>
        public static Transform Parse(string s)
        {
            return new MatrixTransform(Matrix.Parse(s));
        }

        /// <summary>
        /// Raises the <see cref="Changed"/> event.
        /// </summary>
        protected void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns a String representing this transform matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
