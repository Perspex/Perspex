using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Avalonia.Diagnostics.Converters
{
    internal class EnumToCheckedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return parameter;
            }

            return BindingOperations.DoNothing;
        }
    }
}
