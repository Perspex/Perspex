﻿namespace Avalonia.Markup.Xaml
{
    public class AvaloniaXamlLoader :
#if OMNIXAML
        AvaloniaXamlLoaderOmniXaml
#else
        AvaloniaXamlLoaderPortableXaml
#endif
    {
        public static object Parse(string xaml)
                => new AvaloniaXamlLoader().Load(xaml);

        public static T Parse<T>(string xaml)
                     => (T)Parse(xaml);
    }
}