using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class ImageDrawingTests : TestBase
    {
        public ImageDrawingTests()
            : base(@"Media\ImageDrawing")
        {
        }

        private string BitmapPath
        {
            get { return System.IO.Path.Combine(OutputPath, "github_icon.png"); }
        }

        [Fact]
        public async Task ImageDrawing_Fill()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = new DrawingImage
                    {
                        Drawing = new ImageDrawing
                        {
                            ImageSource = new Bitmap(BitmapPath),
                            Rect = new Rect(0, 0, 200, 200),
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageDrawing_BottomRight()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = new DrawingImage
                    {
                        Drawing = new DrawingGroup
                        {
                            Children =
                            {
                                new GeometryDrawing
                                {
                                    Geometry = StreamGeometry.Parse("m0,0 l200,200"),
                                    Brush = Brushes.Black,
                                },
                                new ImageDrawing
                                {
                                    ImageSource = new Bitmap(BitmapPath),
                                    Rect = new Rect(100, 100, 100, 100),
                                }
                            }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
