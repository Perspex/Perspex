using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation.Easings
{
    public class EasingTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Easing.Parse((string)value);
        }
    }
}
