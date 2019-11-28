using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Input;
using System.Collections.Generic;

namespace ControlCatalog
{
    public class DecoratedWindow : Window
    {
        public DecoratedWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        void SetupSide(string name, StandardCursorType cursor)
        {
            var ctl = this.FindControl<Control>(name);
            ctl.Cursor = new Cursor(cursor);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var windowRegions = new Dictionary<IInputElement, WindowRegion>()
            {
                { this.FindControl<Control>("TitleBar"), WindowRegion.TitleBar },
                { this.FindControl<Control>("Left"), WindowRegion.LeftBorder },
                { this.FindControl<Control>("Right"), WindowRegion.RightBorder },
                { this.FindControl<Control>("Top"), WindowRegion.TopBorder },
                { this.FindControl<Control>("Bottom"), WindowRegion.BottomBorder },
                { this.FindControl<Control>("TopLeft"), WindowRegion.TopLeftCorner },
                { this.FindControl<Control>("TopRight"), WindowRegion.TopRightCorner },
                { this.FindControl<Control>("BottomLeft"), WindowRegion.BottomLeftCorner },
                { this.FindControl<Control>("BottomRight"), WindowRegion.BottomRightCorner },
            };

            PlatformImpl.ClassifyWindowRegion = point =>
            {
                var element = this.InputHitTest(point);
                if (element != null && windowRegions.TryGetValue(element, out WindowRegion region))
                {
                    return region;
                }

                return WindowRegion.ClientArea;
            };

            SetupSide("Left", StandardCursorType.LeftSide);
            SetupSide("Right", StandardCursorType.RightSide);
            SetupSide("Top", StandardCursorType.TopSide);
            SetupSide("Bottom", StandardCursorType.BottomSide);
            SetupSide("TopLeft", StandardCursorType.TopLeftCorner);
            SetupSide("TopRight", StandardCursorType.TopRightCorner);
            SetupSide("BottomLeft", StandardCursorType.BottomLeftCorner);
            SetupSide("BottomRight", StandardCursorType.BottomRightCorner);
            this.FindControl<Button>("MinimizeButton").Click += delegate { this.WindowState = WindowState.Minimized; };
            this.FindControl<Button>("MaximizeButton").Click += delegate
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
            this.FindControl<Button>("CloseButton").Click += delegate
            {
                Close();
            };
        }
    }
}
