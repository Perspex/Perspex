using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Controls
{
    public class ExperimentalAcrylicBorder : Decorator
    {
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<ExperimentalAcrylicBorder>();

        public static readonly StyledProperty<ExperimentalAcrylicMaterial> MaterialProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBorder, ExperimentalAcrylicMaterial>(nameof(Material));

        private readonly BorderRenderHelper _borderRenderHelper = new BorderRenderHelper();

        private IDisposable _subscription;

        static ExperimentalAcrylicBorder()
        {
            AffectsRender<ExperimentalAcrylicBorder>(
                MaterialProperty,
                CornerRadiusProperty);
        }


        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public ExperimentalAcrylicMaterial Material
        {
            get => GetValue(MaterialProperty);
            set => SetValue(MaterialProperty, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var tl = (e.Root as TopLevel);

            _subscription = tl.GetObservable(TopLevel.ActualTransparencyLevelProperty)
                .Subscribe(x =>
                {
                    switch (x)
                    {
                        case WindowTransparencyLevel.Transparent:
                        case WindowTransparencyLevel.None:
                            Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.TransparentLevel;
                            break;

                        case WindowTransparencyLevel.Blur:
                            Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.BlurLevel;
                            break;

                        case WindowTransparencyLevel.AcrylicBlur:
                            Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.AcrylicBlurLevel;
                            break;
                    }
                });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _subscription?.Dispose();
        }

        public override void Render(DrawingContext context)
        {
            if (context.PlatformImpl is IDrawingContextWithAcrylicLikeSupport idc)
            {
                var cornerRadius = CornerRadius;

                idc.DrawRectangle(
                    Material,
                    new RoundedRect(
                        new Rect(Bounds.Size),
                        cornerRadius.TopLeft, cornerRadius.TopRight,
                        cornerRadius.BottomRight, cornerRadius.BottomLeft));
            }
            else
            {
                _borderRenderHelper.Render(context, Bounds.Size, new Thickness(), CornerRadius, new ImmutableSolidColorBrush(Material.FallbackColor), null, default);
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding);
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding);
        }
    }
}
