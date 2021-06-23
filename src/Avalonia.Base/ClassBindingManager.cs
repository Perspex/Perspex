using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia
{
    internal static class ClassBindingManager
    {
        private static readonly Dictionary<string, AvaloniaProperty> s_RegisteredProperties =
            new Dictionary<string, AvaloniaProperty>();
        
        public static IDisposable Bind(IStyledElement target, string className, IBinding source, object anchor)
        {
            if (!s_RegisteredProperties.TryGetValue(className, out var prop))
                s_RegisteredProperties[className] = prop = RegisterClassProxyProperty(className);
            return target.Bind(prop, source, anchor);
        }

        private static AvaloniaProperty RegisterClassProxyProperty(string className)
        {
            var prop = AvaloniaProperty.Register<StyledElement, bool>("__AvaloniaReserved::Classes::" + className);
            prop.Changed.Subscribe(args =>
            {
                var classes = ((IStyledElement)args.Sender).Classes;
                classes.Set(className, args.NewValue.GetValueOrDefault());
            });
            
            return prop;
        }
    }
}
