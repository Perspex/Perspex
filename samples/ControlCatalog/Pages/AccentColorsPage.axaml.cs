using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class AccentColorsPage : UserControl
    {
        public AccentColorsPage()
        {
            InitializeComponent();

            _ = ((IResourceHost)this)
                .GetResourceObservable("SystemAccentColor", color =>
                {
                    if (color == AvaloniaProperty.UnsetValue)
                    {
                        return Brushes.Black;
                    }

                    var c = (Color)color;
                    var isDark = ((5 * c.G) + (2 * c.R) + c.B) <= 8 * 128;
                    return isDark ? Brushes.White : Brushes.Black;
                })
                .Subscribe(brush => this.Find<Grid>("ColorBordersGrid").SetValue(TextBlock.ForegroundProperty, brush));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
