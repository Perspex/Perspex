using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;

namespace Avalonia.Layout
{
    public class NonVirtualizingStackLayout : NonVirtualizingLayout
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackLayout.OrientationProperty.AddOwner<NonVirtualizingStackLayout>();

        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty =
            StackLayout.SpacingProperty.AddOwner<NonVirtualizingStackLayout>();

        /// <summary>
        /// Gets or sets the axis along which items are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the axis along which items are laid out.
        /// The default is Vertical.
        /// </value>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets a uniform distance (in pixels) between stacked items. It is applied in the
        /// direction of the StackLayout's Orientation.
        /// </summary>
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        protected internal override Size MeasureOverride(
            NonVirtualizingLayoutContext context,
            Size availableSize)
        {
            var extentU = 0.0;
            var extentV = 0.0;
            var childCount = context.Children.Count;
            var isVertical = Orientation == Orientation.Vertical;
            var spacing = Spacing;
            var constraint = isVertical ?
                availableSize.WithHeight(double.PositiveInfinity) :
                availableSize.WithWidth(double.PositiveInfinity);

            for (var i = 0; i < childCount; ++i)
            {
                var element = context.Children[i];

                if (!element.IsVisible)
                {
                    continue;
                }

                element.Measure(constraint);
                
                if (isVertical)
                {
                    extentU += element.DesiredSize.Height;
                    extentV = Math.Max(extentV, element.DesiredSize.Width);
                }
                else
                {
                    extentU += element.DesiredSize.Width;
                    extentV = Math.Max(extentV, element.DesiredSize.Height);
                }

                if (i < childCount - 1)
                {
                    extentU += spacing;
                }
            }

            return isVertical ? new Size(extentV, extentU) : new Size(extentU, extentV);
        }

        protected internal override Size ArrangeOverride(
            NonVirtualizingLayoutContext context,
            Size finalSize)
        {
            var u = 0.0;
            var childCount = context.Children.Count;
            var isVertical = Orientation == Orientation.Vertical;
            var spacing = Spacing;
            var bounds = new Rect();

            for (var i = 0; i < childCount; ++i)
            {
                var element = context.Children[i];

                if (!element.IsVisible)
                {
                    continue;
                }

                bounds = isVertical ?
                    LayoutVertical(element, u, finalSize) :
                    LayoutHorizontal(element, u, finalSize);
                element.Arrange(bounds);
                u = (isVertical ? bounds.Bottom : bounds.Right) + spacing;
            }

            return new Size(
                Math.Max(finalSize.Width, bounds.Width),
                Math.Max(finalSize.Height, bounds.Height));
        }

        private static Rect LayoutVertical(ILayoutable element, double y, Size constraint)
        {
            var x = 0.0;
            var width = element.DesiredSize.Width;

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    x += (constraint.Width - element.DesiredSize.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    x += constraint.Width - element.DesiredSize.Width;
                    break;
                case HorizontalAlignment.Stretch:
                    width = constraint.Width;
                    break;
            }

            return new Rect(x, y, width, element.DesiredSize.Height);
        }

        private static Rect LayoutHorizontal(ILayoutable element, double x, Size constraint)
        {
            var y = 0.0;
            var height = element.DesiredSize.Height;

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    y += (constraint.Height - element.DesiredSize.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    y += constraint.Height - element.DesiredSize.Height;
                    break;
                case VerticalAlignment.Stretch:
                    height = constraint.Height;
                    break;
            }

            return new Rect(x, y, element.DesiredSize.Width, height);
        }
    }
}
