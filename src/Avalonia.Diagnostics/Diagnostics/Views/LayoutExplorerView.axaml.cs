using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    internal class LayoutExplorerView : UserControl
    {
        private readonly ThicknessEditor _borderArea;
        private readonly ThicknessEditor _paddingArea;
        private readonly Rectangle _horizontalSizeBegin;
        private readonly Rectangle _horizontalSizeEnd;
        private readonly Rectangle _verticalSizeBegin;
        private readonly Rectangle _verticalSizeEnd;
        private readonly Grid _layoutRoot;
        private readonly Border _horizontalSize;
        private readonly Border _verticalSize;
        private readonly Border _contentArea;

        public LayoutExplorerView()
        {
            InitializeComponent();

            _borderArea = this.FindControl<ThicknessEditor>("BorderArea");
            _paddingArea = this.FindControl<ThicknessEditor>("PaddingArea");

            _horizontalSizeBegin = this.FindControl<Rectangle>("HorizontalSizeBegin");
            _horizontalSizeEnd = this.FindControl<Rectangle>("HorizontalSizeEnd");
            _verticalSizeBegin = this.FindControl<Rectangle>("VerticalSizeBegin");
            _verticalSizeEnd = this.FindControl<Rectangle>("VerticalSizeEnd");

            _horizontalSize = this.FindControl<Border>("HorizontalSize");
            _verticalSize = this.FindControl<Border>("VerticalSize");

            _contentArea = this.FindControl<Border>("ContentArea");

            _layoutRoot = this.FindControl<Grid>("LayoutRoot");

            void SubscribeToBounds(Visual visual)
            {
                visual.GetPropertyChangedObservable(TransformedBoundsProperty)
                    .Subscribe(UpdateSizeGuidelines);
            }

            SubscribeToBounds(_borderArea);
            SubscribeToBounds(_paddingArea);
            SubscribeToBounds(_contentArea);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UpdateSizeGuidelines(AvaloniaPropertyChangedEventArgs e)
        {
            void UpdateGuidelines(Visual area)
            {
                if (area.TransformedBounds is TransformedBounds bounds)
                {
                    // Horizontal guideline
                    {
                        var sizeArea = TranslateToRoot((_horizontalSize.TransformedBounds ?? default).Bounds.BottomLeft,
                            _horizontalSize);

                        var start = TranslateToRoot(bounds.Bounds.BottomLeft, area);

                        SetPosition(_horizontalSizeBegin, start);

                        var end = TranslateToRoot(bounds.Bounds.BottomRight, area);

                        SetPosition(_horizontalSizeEnd, end.WithX(end.X - 1));

                        var height = sizeArea.Y - start.Y + 2;

                        _horizontalSizeBegin.Height = height;
                        _horizontalSizeEnd.Height = height;
                    }

                    // Vertical guideline
                    {
                        var sizeArea = TranslateToRoot((_verticalSize.TransformedBounds ?? default).Bounds.TopRight, _verticalSize);

                        var start = TranslateToRoot(bounds.Bounds.TopRight, area);

                        SetPosition(_verticalSizeBegin, start);

                        var end = TranslateToRoot(bounds.Bounds.BottomRight, area);

                        SetPosition(_verticalSizeEnd, end.WithY(end.Y - 1));

                        var width = sizeArea.X - start.X + 2;

                        _verticalSizeBegin.Width = width;
                        _verticalSizeEnd.Width = width;
                    }
                }
            }

            Point TranslateToRoot(Point point, IVisual from)
            {
                return from.TranslatePoint(point, _layoutRoot) ?? default;
            }

            static void SetPosition(Rectangle rect, Point start)
            {
                Canvas.SetLeft(rect, start.X);
                Canvas.SetTop(rect, start.Y);
            }

            if (_borderArea.IsPresent)
            {
                UpdateGuidelines(_borderArea);
            }
            else if (_paddingArea.IsPresent)
            {
                UpdateGuidelines(_paddingArea);
            }
            else
            {
                UpdateGuidelines(_contentArea);
            }
        }
    }
}
