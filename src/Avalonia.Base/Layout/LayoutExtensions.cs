using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Extension methods for layout types.
    /// </summary>
    public static class LayoutExtensions
    {
        /// <summary>
        /// Aligns a rect in a constraining rect according to horizontal and vertical alignment
        /// settings.
        /// </summary>
        /// <param name="rect">The rect to align.</param>
        /// <param name="constraint">The constraining rect.</param>
        /// <param name="horizontalAlignment">The horizontal alignment.</param>
        /// <param name="verticalAlignment">The vertical alignment.</param>
        /// <returns></returns>
        public static Rect Align(
            this Rect rect,
            Rect constraint,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment)
        {
            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    rect = rect.WithX((constraint.Width - rect.Width) / 2);
                    break;
                case HorizontalAlignment.Right:
                    rect = rect.WithX(constraint.Width - rect.Width);
                    break;
                case HorizontalAlignment.Stretch:
                    rect = new Rect(
                        0,
                        rect.Y,
                        Math.Max(constraint.Width, rect.Width),
                        rect.Height);
                    break;
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Center:
                    rect = rect.WithY((constraint.Height - rect.Height) / 2);
                    break;
                case VerticalAlignment.Bottom:
                    rect = rect.WithY(constraint.Height - rect.Height);
                    break;
                case VerticalAlignment.Stretch:
                    rect = new Rect(
                        rect.X,
                        0,
                        rect.Width,
                        Math.Max(constraint.Height, rect.Height));
                    break;
            }

            return rect;
        }
    }
}
