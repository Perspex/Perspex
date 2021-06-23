using System;
using Avalonia.Metadata;
using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// An <see cref="IImage"/> that uses a <see cref="Drawing"/> for content.
    /// </summary>
    public class DrawingImage : AvaloniaObject, IImage, IAffectsRender
    {
        /// <summary>
        /// Defines the <see cref="Drawing"/> property.
        /// </summary>
        public static readonly StyledProperty<Drawing> DrawingProperty =
            AvaloniaProperty.Register<DrawingImage, Drawing>(nameof(Drawing));

        /// <inheritdoc/>
        public event EventHandler Invalidated;

        /// <summary>
        /// Gets or sets the drawing content.
        /// </summary>
        [Content]
        public Drawing Drawing
        {
            get => GetValue(DrawingProperty);
            set => SetValue(DrawingProperty, value);
        }

        /// <inheritdoc/>
        public Size Size => Drawing?.GetBounds().Size ?? default;

        /// <inheritdoc/>
        void IImage.Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode)
        {
            var drawing = Drawing;

            if (drawing == null)
            {
                return;
            }

            var bounds = drawing.GetBounds();
            var scale = Matrix.CreateScale(
                destRect.Width / sourceRect.Width,
                destRect.Height / sourceRect.Height);
            var translate = Matrix.CreateTranslation(
                -sourceRect.X + destRect.X - bounds.X,
                -sourceRect.Y + destRect.Y - bounds.Y);

            using (context.PushClip(destRect))
            using (context.PushPreTransform(translate * scale))
            {
                Drawing?.Draw(context);
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DrawingProperty)
            {
                RaiseInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);
    }
}
