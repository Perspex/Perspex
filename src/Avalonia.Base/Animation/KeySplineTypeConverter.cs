using System;
using System.ComponentModel;
using System.Globalization;

// Ported from WPF open-source code.
// https://github.com/dotnet/wpf/blob/ae1790531c3b993b56eba8b1f0dd395a3ed7de75/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/KeySpline.cs

namespace Avalonia.Animation
{
    /// <summary>
    /// Converts string values to <see cref="KeySpline"/> values
    /// </summary>
    public class KeySplineTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return KeySpline.Parse((string)value, CultureInfo.InvariantCulture);
        }
    }
}
