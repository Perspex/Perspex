using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics
{
    static class Convetions
    {
        public static string DefaultScreenshotsRoot =>
             System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create),
                "Screenshots");

        /// <summary>
        /// Return the path of the screenshot folder according to the rules indicated in issue <see href="https://github.com/AvaloniaUI/Avalonia/issues/4743">GH-4743</see>
        /// </summary>
        public static Func<IControl, string,string> DefaultScreenshotFileNameConvention = (control,screenshotRoot)  =>
        {
            IVisual root;
            if ((control.VisualRoot ?? control.GetVisualRoot()) is IVisual vr)
            {
                root = vr;
            }
            else
            {
                root = control;
            }
            var rootType = root.GetType();
            var windowName = rootType.Name;
            if (root is IControl rc && !string.IsNullOrWhiteSpace(rc.Name))
            {
                windowName = rc.Name;
            }

            var assembly = Assembly.GetEntryAssembly();
            var appName = Application.Current?.Name
                ?? assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? assembly.GetName().Name;
            var appVerions = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>().Version;
            var folder = System.IO.Path.Combine(screenshotRoot
                , appName
                , appVerions
                , windowName);

            return System.IO.Path.Combine(folder
                                , $"{DateTime.Now:yyyyMMddhhmmssfff}.png");
        };
    }
}
