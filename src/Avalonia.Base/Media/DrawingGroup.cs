using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public class DrawingGroup : Drawing
    {
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<DrawingGroup, double>(nameof(Opacity), 1);

        public static readonly StyledProperty<Transform> TransformProperty =
            AvaloniaProperty.Register<DrawingGroup, Transform>(nameof(Transform));

        public double Opacity
        {
            get => GetValue(OpacityProperty);
            set => SetValue(OpacityProperty, value);
        }

        public Transform Transform
        {
            get => GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        [Content]
        public AvaloniaList<Drawing> Children { get; } = new AvaloniaList<Drawing>();

        public override void Draw(DrawingContext context)
        {
            using (context.PushPreTransform(Transform?.Value ?? Matrix.Identity))
            using (context.PushOpacity(Opacity))
            {
                foreach (var drawing in Children)
                {
                    drawing.Draw(context);
                }
            }
        }

        public override Rect GetBounds()
        {
            var rect = new Rect();

            foreach (var drawing in Children)
            {
                rect = rect.Union(drawing.GetBounds());
            }

            if (Transform != null)
            {
                rect = rect.TransformToAABB(Transform.Value);
            }

            return rect;
        }
    }
}
