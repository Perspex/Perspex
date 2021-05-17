using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Viewbox is used to scale single child to fit in the available space.
    /// </summary>
    /// <seealso cref="Avalonia.Controls.Decorator" />
    public class Viewbox : Decorator
    {
        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<Viewbox, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

        static Viewbox()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Viewbox>(true);
            AffectsMeasure<Viewbox>(StretchProperty, StretchDirectionProperty);
        }

        /// <summary>
        /// Gets or sets the stretch mode, 
        /// which determines how child fits into the available space.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction contents will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;

            if (child != null)
            {
                child.Measure(Size.Infinity);

                var childSize = child.DesiredSize;

                var size = Stretch.CalculateSize(availableSize, childSize, StretchDirection);

                return size.Constrain(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;

            if (child != null)
            {
                var childSize = child.DesiredSize;
                var scale = Stretch.CalculateScaling(finalSize, childSize, StretchDirection);

                // TODO: Viewbox should have another decorator as a child so we won't affect other render transforms.
                var scaleTransform = child.RenderTransform as ScaleTransform;

                if (scaleTransform == null)
                {
                    child.RenderTransform = scaleTransform = new ScaleTransform(scale.X, scale.Y);
                    child.RenderTransformOrigin = RelativePoint.TopLeft;
                }

                scaleTransform.ScaleX = scale.X;
                scaleTransform.ScaleY = scale.Y;

                child.Arrange(new Rect(childSize));

                return childSize * scale;
            }

            return new Size();
        }
    }
}
