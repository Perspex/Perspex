using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class FullLayoutTests
    {
        [Fact]
        public void Grandchild_Size_Changed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Border border;
                TextBlock textBlock;

                var window = new Window()
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = border = new Border
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new Border
                        {
                            Child = textBlock = new TextBlock
                            {
                                Width = 400,
                                Height = 400,
                                Text = "Hello World!",
                            },
                        }
                    }
                };

                window.Show();

                Assert.Equal(new Size(400, 400), border.Bounds.Size);
                textBlock.Width = 200;
                window.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Size(200, 400), border.Bounds.Size);
            }
        }

        [Fact]
        public void Test_ScrollViewer_With_TextBlock()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                ScrollViewer scrollViewer;
                TextBlock textBlock;

                var window = new Window()
                {
                    Width = 800,
                    Height = 600,
                    Content = scrollViewer = new ScrollViewer
                    {
                        Width = 200,
                        Height = 200,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = textBlock = new TextBlock
                        {
                            Width = 400,
                            Height = 400,
                            Text = "Hello World!",
                        },
                    }
                };

                window.Resources["ScrollBarThickness"] = 10.0;

                window.Show();

                Assert.Equal(new Size(800, 600), window.Bounds.Size);
                Assert.Equal(new Size(200, 200), scrollViewer.Bounds.Size);
                Assert.Equal(new Point(300, 200), Position(scrollViewer));
                Assert.Equal(new Size(400, 400), textBlock.Bounds.Size);

                var scrollBars = scrollViewer.GetTemplateChildren().OfType<ScrollBar>().ToList();
                var presenters = scrollViewer.GetTemplateChildren().OfType<ScrollContentPresenter>().ToList();

                Assert.Equal(2, scrollBars.Count);
                Assert.Single(presenters);

                var presenter = presenters[0];
                Assert.Equal(new Size(190, 190), presenter.Bounds.Size);

                var horzScroll = scrollBars.Single(x => x.Orientation == Orientation.Horizontal);
                var vertScroll = scrollBars.Single(x => x.Orientation == Orientation.Vertical);

                Assert.True(horzScroll.IsVisible);
                Assert.True(vertScroll.IsVisible);
                Assert.Equal(new Size(190, 10), horzScroll.Bounds.Size);
                Assert.Equal(new Size(10, 190), vertScroll.Bounds.Size);
                Assert.Equal(new Point(0, 190), Position(horzScroll));
                Assert.Equal(new Point(190, 0), Position(vertScroll));
            }
        }

        private static Point Position(IVisual v)
        {
            return v.Bounds.Position;
        }
    }
}
