using System;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Layout
{
    /// <summary>
    /// Provides helper methods needed for layout.
    /// </summary>
    public static class LayoutHelper
    {
        /// <summary>
        /// Epsilon value used for certain layout calculations.
        /// Based on the value in WPF LayoutDoubleUtil.
        /// </summary>
        public static double LayoutEpsilon { get; } = 0.00000153;

        /// <summary>
        /// Calculates a control's size based on its <see cref="ILayoutable.Width"/>,
        /// <see cref="ILayoutable.Height"/>, <see cref="ILayoutable.MinWidth"/>,
        /// <see cref="ILayoutable.MaxWidth"/>, <see cref="ILayoutable.MinHeight"/> and
        /// <see cref="ILayoutable.MaxHeight"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="constraints">The space available for the control.</param>
        /// <returns>The control's size.</returns>
        public static Size ApplyLayoutConstraints(ILayoutable control, Size constraints)
        {
            var minmax = new MinMax(control);

            return new Size(
                MathUtilities.Clamp(constraints.Width, minmax.MinWidth, minmax.MaxWidth),
                MathUtilities.Clamp(constraints.Height, minmax.MinHeight, minmax.MaxHeight));
        }

        public static Size MeasureChild(ILayoutable control, Size availableSize, Thickness padding,
            Thickness borderThickness)
        {
            return MeasureChild(control, availableSize, padding + borderThickness);
        }

        public static Size MeasureChild(ILayoutable control, Size availableSize, Thickness padding)
        {
            if (control != null)
            {
                control.Measure(availableSize.Deflate(padding));
                return control.DesiredSize.Inflate(padding);
            }

            return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
        }

        public static Size ArrangeChild(ILayoutable child, Size availableSize, Thickness padding, Thickness borderThickness)
        {
            return ArrangeChild(child, availableSize, padding + borderThickness);
        }

        public static Size ArrangeChild(ILayoutable child, Size availableSize, Thickness padding)
        {
            child?.Arrange(new Rect(availableSize).Deflate(padding));

            return availableSize;
        }

        /// <summary>
        /// Invalidates measure for given control and all visual children recursively.
        /// </summary>
        public static void InvalidateSelfAndChildrenMeasure(ILayoutable control)
        {
            void InnerInvalidateMeasure(IVisual target)
            {
                if (target is ILayoutable targetLayoutable)
                {
                    targetLayoutable.InvalidateMeasure();
                }

                var visualChildren = target.VisualChildren;
                var visualChildrenCount = visualChildren.Count;

                for (int i = 0; i < visualChildrenCount; i++)
                {
                    IVisual child = visualChildren[i];

                    InnerInvalidateMeasure(child);
                }
            }

            InnerInvalidateMeasure(control);
        }

        /// <summary>
        /// Obtains layout scale of the given control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <exception cref="Exception">Thrown when control has no root or returned layout scaling is invalid.</exception>
        public static double GetLayoutScale(ILayoutable control)
        {
            var visualRoot = control.VisualRoot;
            
            var result = (visualRoot as ILayoutRoot)?.LayoutScaling ?? 1.0;

            if (result == 0 || double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new Exception($"Invalid LayoutScaling returned from {visualRoot.GetType()}");
            }

            return result;
        }

        /// <summary>
        /// Rounds a size to integer values for layout purposes, compensating for high DPI screen
        /// coordinates.
        /// </summary>
        /// <param name="size">Input size.</param>
        /// <param name="dpiScaleX">DPI along x-dimension.</param>
        /// <param name="dpiScaleY">DPI along y-dimension.</param>
        /// <returns>Value of size that will be rounded under screen DPI.</returns>
        /// <remarks>
        /// This is a layout helper method. It takes DPI into account and also does not return
        /// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
        /// associated with the UseLayoutRounding property and should not be used as a general rounding
        /// utility.
        /// </remarks>
        public static Size RoundLayoutSize(Size size, double dpiScaleX, double dpiScaleY)
        {
            return new Size(RoundLayoutValue(size.Width, dpiScaleX), RoundLayoutValue(size.Height, dpiScaleY));
        }

        /// <summary>
        /// Calculates the value to be used for layout rounding at high DPI.
        /// </summary>
        /// <param name="value">Input value to be rounded.</param>
        /// <param name="dpiScale">Ratio of screen's DPI to layout DPI</param>
        /// <returns>Adjusted value that will produce layout rounding on screen at high dpi.</returns>
        /// <remarks>
        /// This is a layout helper method. It takes DPI into account and also does not return
        /// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
        /// associated with the UseLayoutRounding property and should not be used as a general rounding
        /// utility.
        /// </remarks>
        public static double RoundLayoutValue(double value, double dpiScale)
        {
            double newValue;

            // If DPI == 1, don't use DPI-aware rounding.
            if (!MathUtilities.IsOne(dpiScale))
            {
                newValue = Math.Round(value * dpiScale) / dpiScale;

                // If rounding produces a value unacceptable to layout (NaN, Infinity or MaxValue),
                // use the original value.
                if (double.IsNaN(newValue) ||
                    double.IsInfinity(newValue) ||
                    MathUtilities.AreClose(newValue, double.MaxValue))
                {
                    newValue = value;
                }
            }
            else
            {
                newValue = Math.Round(value);
            }

            return newValue;
        }


        /// <summary>
        /// Calculates the min and max height for a control. Ported from WPF.
        /// </summary>
        private readonly struct MinMax
        {
            public MinMax(ILayoutable e)
            {
                MaxHeight = e.MaxHeight;
                MinHeight = e.MinHeight;
                double l = e.Height;

                double height = (double.IsNaN(l) ? double.PositiveInfinity : l);
                MaxHeight = Math.Max(Math.Min(height, MaxHeight), MinHeight);

                height = (double.IsNaN(l) ? 0 : l);
                MinHeight = Math.Max(Math.Min(MaxHeight, height), MinHeight);

                MaxWidth = e.MaxWidth;
                MinWidth = e.MinWidth;
                l = e.Width;

                double width = (double.IsNaN(l) ? double.PositiveInfinity : l);
                MaxWidth = Math.Max(Math.Min(width, MaxWidth), MinWidth);

                width = (double.IsNaN(l) ? 0 : l);
                MinWidth = Math.Max(Math.Min(MaxWidth, width), MinWidth);
            }

            public double MinWidth { get; }
            public double MaxWidth { get; }
            public double MinHeight { get; }
            public double MaxHeight { get; }
        }
    }
}
