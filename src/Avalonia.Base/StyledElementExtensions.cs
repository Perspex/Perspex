using System;
using Avalonia.Data;

namespace Avalonia
{
    public static class StyledElementExtensions
    {
        public static IDisposable BindClass(this IStyledElement target, string className, IBinding source, object anchor) =>
            ClassBindingManager.Bind(target, className, source, anchor);
    }
}
