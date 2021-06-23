using Avalonia.Animation.Animators;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public class SolidColorBrush : Brush, ISolidColorBrush
    {
        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<SolidColorBrush, Color>(nameof(Color));

        static SolidColorBrush()
        {
            BaseBrushAnimator.RegisterBrushAnimator<ISolidColorBrushAnimator>(match => typeof(ISolidColorBrush).IsAssignableFrom(match));
            AffectsRender<SolidColorBrush>(ColorProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
        /// </summary>
        public SolidColorBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        public SolidColorBrush(Color color, double opacity = 1)
        {
            Color = color;
            Opacity = opacity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public SolidColorBrush(uint color)
            : this(Color.FromUInt32(color))
        {
        }

        /// <summary>
        /// Gets or sets the color of the brush.
        /// </summary>
        public Color Color
        {
            get { return GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// Parses a brush string.
        /// </summary>
        /// <param name="s">The brush string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        /// <remarks>
        /// Whereas <see cref="Brush.Parse(string)"/> may return an immutable solid color brush,
        /// this method always returns a mutable <see cref="SolidColorBrush"/>.
        /// </remarks>
        public static new SolidColorBrush Parse(string s)
        {
            var brush = (ISolidColorBrush)Brush.Parse(s);
            return brush is SolidColorBrush solid ? solid : new SolidColorBrush(brush.Color);
        }

        /// <summary>
        /// Returns a string representation of the brush.
        /// </summary>
        /// <returns>A string representation of the brush.</returns>
        public override string ToString()
        {
            return Color.ToString();
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableSolidColorBrush(this);
        }
    }
}
