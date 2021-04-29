using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace RenderDemo.Pages
{
    public class FormattedTextPage : UserControl
    {
        public FormattedTextPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext context)
        {
            const string testString = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor";

            // Create the initial formatted text string.
            var formattedText = new FormattedText(
                testString,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                32,
                Brushes.Black) { MaxTextWidth = 300, MaxTextHeight = 240 };

            // Set a maximum width and height. If the text overflows these values, an ellipsis "..." appears.

            // Use a larger font size beginning at the first (zero-based) character and continuing for 5 characters.
            // The font size is calculated in terms of points -- not as device-independent pixels.
            formattedText.SetFontSize(36 * (96.0 / 72.0), 0, 5);

            // Use a Bold font weight beginning at the 6th character and continuing for 11 characters.
            formattedText.SetFontWeight(FontWeight.Bold, 6, 11);

            var gradient = new LinearGradientBrush
            {
                GradientStops =
                    new GradientStops { new GradientStop(Colors.Orange, 0), new GradientStop(Colors.Teal, 1) },
                StartPoint = new RelativePoint(0,0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0,1, RelativeUnit.Relative)
            };

            // Use a linear gradient brush beginning at the 6th character and continuing for 11 characters.
            formattedText.SetForegroundBrush(gradient, 6, 11);

            // Use an Italic font style beginning at the 28th character and continuing for 28 characters.
            formattedText.SetFontStyle(FontStyle.Italic, 28, 28);

            context.DrawText(formattedText, new Point(10, 0));
        }
    }
}
