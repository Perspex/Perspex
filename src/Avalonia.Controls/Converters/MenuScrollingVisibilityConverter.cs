using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Utilities;

namespace Avalonia.Controls.Converters
{
    public class MenuScrollingVisibilityConverter : IMultiValueConverter
    {
        public static readonly MenuScrollingVisibilityConverter Instance = new MenuScrollingVisibilityConverter();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null ||
                values == null ||
                values.Count != 4 ||
                !(values[0] is ScrollBarVisibility visibility) ||
                !(values[1] is double offset) ||
                !(values[2] is double extent) ||
                !(values[3] is double viewport))
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (visibility == ScrollBarVisibility.Auto)
            {
                if (extent == viewport)
                {
                    return false;
                }

                double target;

                if (parameter is double d)
                {
                    target = d;
                }
                else if (parameter is string s)
                {
                    target = double.Parse(s, NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    return AvaloniaProperty.UnsetValue;
                }

                // Calculate the percent so that we can see if we are near the edge of the range
                double percent = MathUtilities.Clamp(offset * 100.0 / (extent - viewport), 0, 100);

                if (MathUtilities.AreClose(percent, target))
                {
                    // We are at the end of the range, so no need for this button to be shown
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
