using Avalonia.Media;

namespace Avalonia.Rendering.Utilities
{
    public class TileBrushCalculator
    {
        private readonly Size _imageSize;
        private readonly Rect _drawRect;

        public bool IsValid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileBrushCalculator"/> class.
        /// </summary>
        /// <param name="brush">The brush to be rendered.</param>
        /// <param name="contentSize">The size of the content of the tile brush.</param>
        /// <param name="targetSize">The size of the control to which the brush is being rendered.</param>
        public TileBrushCalculator(ITileBrush brush, Size contentSize, Size targetSize)
            : this(
                  brush.TileMode,
                  brush.Stretch,
                  brush.AlignmentX,
                  brush.AlignmentY,
                  brush.SourceRect,
                  brush.DestinationRect,
                  contentSize,
                  targetSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileBrushCalculator"/> class.
        /// </summary>
        /// <param name="tileMode">The brush's tile mode.</param>
        /// <param name="stretch">The brush's stretch.</param>
        /// <param name="alignmentX">The brush's horizontal alignment.</param>
        /// <param name="alignmentY">The brush's vertical alignment.</param>
        /// <param name="sourceRect">The brush's source rect</param>
        /// <param name="destinationRect">The brush's destination rect.</param>
        /// <param name="contentSize">The size of the content of the tile brush.</param>
        /// <param name="targetSize">The size of the control to which the brush is being rendered.</param>
        public TileBrushCalculator(
            TileMode tileMode,
            Stretch stretch,
            AlignmentX alignmentX,
            AlignmentY alignmentY,
            RelativeRect sourceRect,
            RelativeRect destinationRect,
            Size contentSize,
            Size targetSize)
        {
            _imageSize = contentSize;

            SourceRect = sourceRect.ToPixels(_imageSize);
            DestinationRect = destinationRect.ToPixels(targetSize);

            var scale = stretch.CalculateScaling(DestinationRect.Size, SourceRect.Size);
            var translate = CalculateTranslate(alignmentX, alignmentY, SourceRect, DestinationRect, scale);

            IntermediateSize = tileMode == TileMode.None ? targetSize : DestinationRect.Size;
            IntermediateTransform = CalculateIntermediateTransform(
                tileMode,
                SourceRect,
                DestinationRect,
                scale,
                translate,
                out _drawRect);
        }

        /// <summary>
        /// Gets the rectangle on the destination control to which content should be rendered.
        /// </summary>
        /// <remarks>
        /// If <see cref="TileMode"/> of the brush is repeating then this is describes rectangle
        /// of a single repeat of the tiled content.
        /// </remarks>
        public Rect DestinationRect { get; }

        /// <summary>
        /// Gets the clip rectangle on the intermediate image with which the brush content should be
        /// drawn when <see cref="NeedsIntermediate"/> is true.
        /// </summary>
        public Rect IntermediateClip => _drawRect;

        /// <summary>
        /// Gets the size of the intermediate image that should be created when
        /// <see cref="NeedsIntermediate"/> is true.
        /// </summary>
        public Size IntermediateSize { get; }

        /// <summary>
        /// Gets the transform to be used when rendering to the intermediate image when
        /// <see cref="NeedsIntermediate"/> is true.
        /// </summary>
        public Matrix IntermediateTransform { get; }

        /// <summary>
        /// Gets a value indicating whether an intermediate image should be created in order to
        /// render the tile brush.
        /// </summary>
        /// <remarks>
        /// Intermediate images are required when a brush's <see cref="TileMode"/> is not repeating
        /// but the source and destination aspect ratios are unequal, as all of the currently
        /// supported rendering backends do not support non-tiled image brushes.
        /// </remarks>
        public bool NeedsIntermediate
        {
            get
            {
                if (IntermediateTransform != Matrix.Identity)
                    return true;
                if (SourceRect.Position != default(Point))
                    return true;
                if (SourceRect.Size.AspectRatio == _imageSize.AspectRatio)
                    return false;
                if (SourceRect.Width != _imageSize.Width ||
                    SourceRect.Height != _imageSize.Height)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Gets the area of the source content to be rendered.
        /// </summary>
        public Rect SourceRect { get; }

        public static Vector CalculateTranslate(
            AlignmentX alignmentX,
            AlignmentY alignmentY,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale)
        {
            var x = 0.0;
            var y = 0.0;
            var size = sourceRect.Size * scale;

            switch (alignmentX)
            {
                case AlignmentX.Center:
                    x += (destinationRect.Width - size.Width) / 2;
                    break;
                case AlignmentX.Right:
                    x += destinationRect.Width - size.Width;
                    break;
            }

            switch (alignmentY)
            {
                case AlignmentY.Center:
                    y += (destinationRect.Height - size.Height) / 2;
                    break;
                case AlignmentY.Bottom:
                    y += destinationRect.Height - size.Height;
                    break;
            }

            return new Vector(x, y);
        }

        public static Matrix CalculateIntermediateTransform(
            TileMode tileMode,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale,
            Vector translate,
            out Rect drawRect)
        {
            var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                            Matrix.CreateScale(scale) *
                            Matrix.CreateTranslation(translate);
            Rect dr;

            if (tileMode == TileMode.None)
            {
                dr = destinationRect;
                transform *= Matrix.CreateTranslation(destinationRect.Position);
            }
            else
            {
                dr = new Rect(destinationRect.Size);
            }

            drawRect = dr;

            return transform;
        }
    }
}
