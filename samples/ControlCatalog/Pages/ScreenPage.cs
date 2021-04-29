using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace ControlCatalog.Pages
{
    public class ScreenPage : UserControl
    {
        private double _leftMost;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Window w = (Window)VisualRoot;
            w.PositionChanged += (sender, args) => InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!(VisualRoot is Window w))
            {
                return;
            }
            var screens = w.Screens.All;
            var scaling = ((IRenderRoot)w).RenderScaling;

            var drawBrush = Brushes.Black;
            Pen p = new Pen(drawBrush);
            if (screens != null)
                foreach (Screen screen in screens)
                {
                    if (screen.Bounds.X / 10f < _leftMost)
                    {
                        _leftMost = screen.Bounds.X / 10f;
                        InvalidateVisual();
                        return;
                    }

                    Rect boundsRect = new Rect(screen.Bounds.X / 10f + Math.Abs(_leftMost), screen.Bounds.Y / 10f, screen.Bounds.Width / 10f,
                                      screen.Bounds.Height / 10f);
                    Rect workingAreaRect = new Rect(screen.WorkingArea.X / 10f + Math.Abs(_leftMost), screen.WorkingArea.Y / 10f, screen.WorkingArea.Width / 10f,
                                       screen.WorkingArea.Height / 10f);
                    
                    context.DrawRectangle(p, boundsRect);
                    context.DrawRectangle(p, workingAreaRect);


                    var formattedText = CreateFormattedText($"Bounds: {screen.Bounds.Width}:{screen.Bounds.Height}");
                    context.DrawText(formattedText, boundsRect.Position.WithY(boundsRect.Size.Height));

                    formattedText =
                        CreateFormattedText($"WorkArea: {screen.WorkingArea.Width}:{screen.WorkingArea.Height}");
                    context.DrawText(formattedText, boundsRect.Position.WithY(boundsRect.Size.Height + 20));

                    formattedText = CreateFormattedText($"Scaling: {screen.PixelDensity * 100}%");
                    context.DrawText(formattedText, boundsRect.Position.WithY(boundsRect.Size.Height + 40));

                    formattedText = CreateFormattedText($"Primary: {screen.Primary}");
                    context.DrawText(formattedText, boundsRect.Position.WithY(boundsRect.Size.Height + 60));

                    formattedText =
                        CreateFormattedText(
                            $"Current: {screen.Equals(w.Screens.ScreenFromBounds(new PixelRect(w.Position, PixelSize.FromSize(w.Bounds.Size, scaling))))}");
                    context.DrawText(formattedText, boundsRect.Position.WithY(boundsRect.Size.Height + 80));
                }

            context.DrawRectangle(p, new Rect(w.Position.X / 10f + Math.Abs(_leftMost), w.Position.Y / 10f, w.Bounds.Width / 10, w.Bounds.Height / 10));
        }

        private FormattedText CreateFormattedText(string textToFormat)
        {
            return new FormattedText(textToFormat, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 12, Brushes.Green);
        }
    }
}
