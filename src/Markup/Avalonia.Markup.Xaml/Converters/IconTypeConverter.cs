﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    using Portable.Xaml.ComponentModel;
	using System.ComponentModel;

    public class IconTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var path = value as string;
            if (path != null)
            {
                return CreateIconFromPath(context, path);
            }

            var bitmap = value as IBitmap;
            if (bitmap != null)
            {
                return new WindowIcon(bitmap);
            }

            throw new NotSupportedException();
        }

        private WindowIcon CreateIconFromPath(ITypeDescriptorContext context, string path)
        {
            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";

            switch (scheme)
            {
                case "file":
                    return new WindowIcon(path);

                default:
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    return new WindowIcon(assets.Open(uri, context.GetBaseUri()));
            }
        }
    }
}
