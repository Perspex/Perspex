using Avalonia.Media.Imaging;

namespace Avalonia.Media
{ 
    public class RenderOptions
    {
        /// <summary>
        /// Defines the <see cref="BitmapInterpolationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<BitmapInterpolationMode> BitmapInterpolationModeProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, AvaloniaObject, BitmapInterpolationMode>(
                "BitmapInterpolationMode", 
                BitmapInterpolationMode.MediumQuality,
                inherits: true);

        /// <summary>
        /// Gets the value of the BitmapInterpolationMode attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static BitmapInterpolationMode GetBitmapInterpolationMode(AvaloniaObject element)
        {
            return element.GetValue(BitmapInterpolationModeProperty);
        }

        /// <summary>
        /// Sets the value of the BitmapInterpolationMode attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetBitmapInterpolationMode(AvaloniaObject element, BitmapInterpolationMode value)
        {
            element.SetValue(BitmapInterpolationModeProperty, value);
        }
    }
}
