using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Extension methods for brush classes.
    /// </summary>
    public static class BrushExtensions
    {
        /// <summary>
        /// Converts a brush to an immutable brush.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>
        /// The result of calling <see cref="IMutableBrush.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="brush"/>.
        /// </returns>
        public static IBrush ToImmutable(this IBrush brush)
        {
            Contract.Requires<ArgumentNullException>(brush != null);

            return (brush as IMutableBrush)?.ToImmutable() ?? brush;
        }

        /// <summary>
        /// Converts a dash style to an immutable dash style.
        /// </summary>
        /// <param name="style">The dash style.</param>
        /// <returns>
        /// The result of calling <see cref="DashStyle.ToImmutable"/> if the style is mutable,
        /// otherwise <paramref name="style"/>.
        /// </returns>
        public static ImmutableDashStyle ToImmutable(this IDashStyle style)
        {
            Contract.Requires<ArgumentNullException>(style != null);

            return style as ImmutableDashStyle ?? ((DashStyle)style).ToImmutable();
        }

        /// <summary>
        /// Converts a pen to an immutable pen.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <returns>
        /// The result of calling <see cref="Pen.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="pen"/>.
        /// </returns>
        public static ImmutablePen ToImmutable(this IPen pen)
        {
            Contract.Requires<ArgumentNullException>(pen != null);

            return pen as ImmutablePen ?? ((Pen)pen).ToImmutable();
        }
    }
}
