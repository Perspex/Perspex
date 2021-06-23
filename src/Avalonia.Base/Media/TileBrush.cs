using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how a <see cref="TileBrush"/> is tiled.
    /// </summary>
    public enum TileMode
    {
        /// <summary>
        /// A single repeat of the content will be displayed.
        /// </summary>
        None,

        /// <summary>
        /// The content will be repeated horizontally, with alternate tiles mirrored.
        /// </summary>
        FlipX,

        /// <summary>
        /// The content will be repeated vertically, with alternate tiles mirrored.
        /// </summary>
        FlipY,

        /// <summary>
        /// The content will be repeated horizontally and vertically, with alternate tiles mirrored.
        /// </summary>
        FlipXY,

        /// <summary>
        /// The content will be repeated.
        /// </summary>
        Tile
    }

    /// <summary>
    /// Base class for brushes which display repeating images.
    /// </summary>
    public abstract class TileBrush : Brush, ITileBrush
    {
        /// <summary>
        /// Defines the <see cref="AlignmentX"/> property.
        /// </summary>
        public static readonly StyledProperty<AlignmentX> AlignmentXProperty =
            AvaloniaProperty.Register<TileBrush, AlignmentX>(nameof(AlignmentX), AlignmentX.Center);

        /// <summary>
        /// Defines the <see cref="AlignmentY"/> property.
        /// </summary>
        public static readonly StyledProperty<AlignmentY> AlignmentYProperty =
            AvaloniaProperty.Register<TileBrush, AlignmentY>(nameof(AlignmentY), AlignmentY.Center);

        /// <summary>
        /// Defines the <see cref="DestinationRect"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativeRect> DestinationRectProperty =
            AvaloniaProperty.Register<TileBrush, RelativeRect>(nameof(DestinationRect), RelativeRect.Fill);

        /// <summary>
        /// Defines the <see cref="SourceRect"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativeRect> SourceRectProperty =
            AvaloniaProperty.Register<TileBrush, RelativeRect>(nameof(SourceRect), RelativeRect.Fill);

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<TileBrush, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="TileMode"/> property.
        /// </summary>
        public static readonly StyledProperty<TileMode> TileModeProperty =
            AvaloniaProperty.Register<TileBrush, TileMode>(nameof(TileMode));

        static TileBrush()
        {
            AffectsRender<TileBrush>(
                AlignmentXProperty,
                AlignmentYProperty,
                DestinationRectProperty,
                SourceRectProperty,
                StretchProperty,
                TileModeProperty);
            RenderOptions.BitmapInterpolationModeProperty.OverrideDefaultValue<TileBrush>(BitmapInterpolationMode.Default);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of a tile in the destination.
        /// </summary>
        public AlignmentX AlignmentX
        {
            get { return GetValue(AlignmentXProperty); }
            set { SetValue(AlignmentXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of a tile in the destination.
        /// </summary>
        public AlignmentY AlignmentY
        {
            get { return GetValue(AlignmentYProperty); }
            set { SetValue(AlignmentYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the rectangle on the destination in which to paint a tile.
        /// </summary>
        public RelativeRect DestinationRect
        {
            get { return GetValue(DestinationRectProperty); }
            set { SetValue(DestinationRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the rectangle of the source image that will be displayed.
        /// </summary>
        public RelativeRect SourceRect
        {
            get { return GetValue(SourceRectProperty); }
            set { SetValue(SourceRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value controlling how the source rectangle will be stretched to fill
        /// the destination rect.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush's tile mode.
        /// </summary>
        public TileMode TileMode
        {
            get { return (TileMode)GetValue(TileModeProperty); }
            set { SetValue(TileModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the bitmap interpolation mode.
        /// </summary>
        /// <value>
        /// The bitmap interpolation mode.
        /// </value>
        public BitmapInterpolationMode BitmapInterpolationMode
        {
            get { return RenderOptions.GetBitmapInterpolationMode(this); }
            set { RenderOptions.SetBitmapInterpolationMode(this, value); }
        }
    }
}
