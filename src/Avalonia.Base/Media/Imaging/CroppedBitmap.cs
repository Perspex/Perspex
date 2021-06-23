using System;
using Avalonia.Media.Imaging;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Crops a Bitmap.
    /// </summary>
    public class CroppedBitmap : AvaloniaObject, IImage, IAffectsRender, IDisposable
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage> SourceProperty =
            AvaloniaProperty.Register<CroppedBitmap, IImage>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="SourceRect"/> property.
        /// </summary>
        public static readonly StyledProperty<PixelRect> SourceRectProperty =
            AvaloniaProperty.Register<CroppedBitmap, PixelRect>(nameof(SourceRect));

        public event EventHandler Invalidated;

        static CroppedBitmap()
        {
            SourceRectProperty.Changed.AddClassHandler<CroppedBitmap>((x, e) => x.SourceRectChanged(e));
            SourceProperty.Changed.AddClassHandler<CroppedBitmap>((x, e) => x.SourceChanged(e));
        }

        /// <summary>
        /// Gets or sets the source for the bitmap.
        /// </summary>
        public IImage Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the rectangular area that the bitmap is cropped to.
        /// </summary>
        public PixelRect SourceRect
        {
            get => GetValue(SourceRectProperty);
            set => SetValue(SourceRectProperty, value);
        }

        public CroppedBitmap()
        {
            Source = null;
            SourceRect = default;
        }

        public CroppedBitmap(IImage source, PixelRect sourceRect)
        {
            Source = source;
            SourceRect = sourceRect;
        }

        private void SourceChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;
            if (!(e.NewValue is IBitmap))
                throw new ArgumentException("Only IBitmap supported as source");
            Invalidated?.Invoke(this, e);
        }

        private void SourceRectChanged(AvaloniaPropertyChangedEventArgs e) => Invalidated?.Invoke(this, e);

        public virtual void Dispose()
        {
            (Source as IBitmap)?.Dispose();
        }

        public Size Size {
            get
            {
                if (Source == null)
                    return Size.Empty;
                if (SourceRect.IsEmpty)
                    return Source.Size;
                return SourceRect.Size.ToSizeWithDpi((Source as IBitmap).Dpi);
            }
        }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            if (Source == null)
                return;
            var topLeft = SourceRect.TopLeft.ToPointWithDpi((Source as IBitmap).Dpi);
            Source.Draw(context, sourceRect.Translate(new Vector(topLeft.X, topLeft.Y)), destRect, bitmapInterpolationMode);           
        }
    }
}
