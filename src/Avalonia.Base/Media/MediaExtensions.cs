using System;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Provides extension methods for Avalonia media.
    /// </summary>
    public static class MediaExtensions
    {
        /// <summary>
        /// Calculates scaling based on a <see cref="Stretch"/> value.
        /// </summary>
        /// <param name="stretch">The stretch mode.</param>
        /// <param name="destinationSize">The size of the destination viewport.</param>
        /// <param name="sourceSize">The size of the source.</param>
        /// <param name="stretchDirection">The stretch direction.</param>
        /// <returns>A vector with the X and Y scaling factors.</returns>
        public static Vector CalculateScaling(
            this Stretch stretch,
            Size destinationSize,
            Size sourceSize,
            StretchDirection stretchDirection = StretchDirection.Both)
        {
            var scaleX = 1.0;
            var scaleY = 1.0;

            bool isConstrainedWidth = !double.IsPositiveInfinity(destinationSize.Width);
            bool isConstrainedHeight = !double.IsPositiveInfinity(destinationSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                 && (isConstrainedWidth || isConstrainedHeight))
            {
                // Compute scaling factors for both axes
                scaleX = MathUtilities.IsZero(sourceSize.Width) ? 0.0 : destinationSize.Width / sourceSize.Width;
                scaleY = MathUtilities.IsZero(sourceSize.Height) ? 0.0 : destinationSize.Height / sourceSize.Height;

                if (!isConstrainedWidth)
                {
                    scaleX = scaleY;
                }
                else if (!isConstrainedHeight)
                {
                    scaleY = scaleX;
                }
                else
                {
                    // If not preserving aspect ratio, then just apply transform to fit
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            // Find minimum scale that we use for both axes
                            double minscale = scaleX < scaleY ? scaleX : scaleY;
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill:
                            // Find maximum scale that we use for both axes
                            double maxscale = scaleX > scaleY ? scaleX : scaleY;
                            scaleX = scaleY = maxscale;
                            break;

                        case Stretch.Fill:
                            // We already computed the fill scale factors above, so just use them
                            break;
                    }
                }

                // Apply stretch direction by bounding scales.
                // In the uniform case, scaleX=scaleY, so this sort of clamping will maintain aspect ratio
                // In the uniform fill case, we have the same result too.
                // In the fill case, note that we change aspect ratio, but that is okay
                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0)
                            scaleX = 1.0;
                        if (scaleY < 1.0)
                            scaleY = 1.0;
                        break;

                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0)
                            scaleX = 1.0;
                        if (scaleY > 1.0)
                            scaleY = 1.0;
                        break;

                    case StretchDirection.Both:
                        break;

                    default:
                        break;
                }
            }

            return new Vector(scaleX, scaleY);
        }

        /// <summary>
        /// Calculates a scaled size based on a <see cref="Stretch"/> value.
        /// </summary>
        /// <param name="stretch">The stretch mode.</param>
        /// <param name="destinationSize">The size of the destination viewport.</param>
        /// <param name="sourceSize">The size of the source.</param>
        /// <param name="stretchDirection">The stretch direction.</param>
        /// <returns>The size of the stretched source.</returns>
        public static Size CalculateSize(
            this Stretch stretch,
            Size destinationSize,
            Size sourceSize,
            StretchDirection stretchDirection = StretchDirection.Both)
        {
            return sourceSize * stretch.CalculateScaling(destinationSize, sourceSize, stretchDirection);
        }
    }
}
