using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics
{
    public static class DevTools
    {
        private static readonly Dictionary<TopLevel, Window> s_open = new Dictionary<TopLevel, Window>();

        public static IDisposable Attach(TopLevel root, KeyGesture gesture)
        {
            return Attach(root, new DevToolsOptions()
            {
                Gesture = gesture,
            });
        }

        public static IDisposable Attach(TopLevel root, DevToolsOptions options)
        {
            void PreviewKeyDown(object? sender, KeyEventArgs e)
            {
                if (options.Gesture.Matches(e))
                {
                    Open(root, options);
                }
            }

            return root.AddDisposableHandler(
                InputElement.KeyDownEvent,
                PreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        public static IDisposable Open(TopLevel root) => Open(root, new DevToolsOptions());

        public static IDisposable Open(TopLevel root, DevToolsOptions options)
        {
            if (s_open.TryGetValue(root, out var window))
            {
                window.Activate();
            }
            else
            {
                window = new MainWindow
                {
                    Root = root,
                    Width = options.Size.Width,
                    Height = options.Size.Height,
                };

                window.Closed += DevToolsClosed;
                s_open.Add(root, window);

                if (options.ShowAsChildWindow && root is Window inspectedWindow)
                {
                    window.Show(inspectedWindow);
                }
                else
                {
                    window.Show();
                }
            }

            return Disposable.Create(() => window?.Close());
        }

        private static void DevToolsClosed(object? sender, EventArgs e)
        {
            var window = (MainWindow)sender!;
            s_open.Remove(window.Root!);
            window.Closed -= DevToolsClosed;
        }
    }
}
