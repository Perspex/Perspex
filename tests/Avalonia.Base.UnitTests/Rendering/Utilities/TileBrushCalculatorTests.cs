using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.Utilities
{
    public class TileBrushCalculatorTests
    {
        [Fact]
        public void NoTile_Fill_1x()
        {
            var result = new TileBrushCalculator(
                TileMode.None,
                Stretch.Fill,
                AlignmentX.Center,
                AlignmentY.Center,
                RelativeRect.Fill,
                RelativeRect.Fill,
                new Size(100, 100),
                new Size(100, 100));

            Assert.False(result.NeedsIntermediate);
            Assert.Equal(new Rect(0, 0, 100, 100), result.SourceRect);
            Assert.Equal(new Rect(0, 0, 100, 100), result.DestinationRect);
        }

        [Fact]
        public void NoTile_Fill_2x()
        {
            var result = new TileBrushCalculator(
                TileMode.None,
                Stretch.Fill,
                AlignmentX.Center,
                AlignmentY.Center,
                RelativeRect.Fill,
                RelativeRect.Fill,
                new Size(100, 100),
                new Size(200, 200));

            // TODO: This doesn't need an intermediate render target.
            Assert.True(result.NeedsIntermediate);
            Assert.Equal(new Rect(0, 0, 100, 100), result.SourceRect);
            Assert.Equal(new Rect(0, 0, 200, 200), result.DestinationRect);
        }

        [Fact]
        public void NoTile_Uniform_CenterHoriz()
        {
            var result = new TileBrushCalculator(
                TileMode.None,
                Stretch.Uniform,
                AlignmentX.Center,
                AlignmentY.Center,
                RelativeRect.Fill,
                RelativeRect.Fill,
                new Size(100, 100),
                new Size(200, 100));

            Assert.True(result.NeedsIntermediate);
            Assert.Equal(new Rect(0, 0, 100, 100), result.SourceRect);
            Assert.Equal(new Rect(0, 0, 200, 100), result.DestinationRect);
            Assert.Equal(new Size(200, 100), result.IntermediateSize);
            Assert.Equal(Matrix.CreateTranslation(50, 0), result.IntermediateTransform);
        }

        [Fact]
        public void NoTile_Uniform_CenterVert()
        {
            var result = new TileBrushCalculator(
                TileMode.None,
                Stretch.Uniform,
                AlignmentX.Center,
                AlignmentY.Center,
                RelativeRect.Fill,
                RelativeRect.Fill,
                new Size(100, 100),
                new Size(100, 200));

            Assert.True(result.NeedsIntermediate);
            Assert.Equal(new Rect(0, 0, 100, 100), result.SourceRect);
            Assert.Equal(new Rect(0, 0, 100, 200), result.DestinationRect);
            Assert.Equal(new Size(100, 200), result.IntermediateSize);
            Assert.Equal(Matrix.CreateTranslation(0, 50), result.IntermediateTransform);
        }

        [Fact]
        public void NoTile_NoStretch_BottomRightQuarterDest()
        {
            var result = new TileBrushCalculator(
                TileMode.None,
                Stretch.None,
                AlignmentX.Center,
                AlignmentY.Center,
                RelativeRect.Fill,
                new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                new Size(800, 800),
                new Size(400, 400));

            Assert.True(result.NeedsIntermediate);
            Assert.Equal(new Rect(0, 0, 800, 800), result.SourceRect);
            Assert.Equal(new Rect(200, 200, 200, 200), result.DestinationRect);
            Assert.Equal(new Size(400, 400), result.IntermediateSize);
            Assert.Equal(new Rect(200, 200, 200, 200), result.IntermediateClip);
            Assert.Equal(Matrix.CreateTranslation(-100, -100), result.IntermediateTransform);
        }

        [Fact]
        public void Tile_NoStretch_BottomRightQuarterSource_CenterQuarterDest()
        {
            var result = new TileBrushCalculator(
                TileMode.Tile,
                Stretch.None,
                AlignmentX.Center,
                AlignmentY.Center,
                new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
                new Size(800, 800),
                new Size(400, 400));

            var b = new VisualBrush
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
                SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                DestinationRect = new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
                Visual = new Border { Width = 400, Height = 400 },
            };

            Assert.True(result.NeedsIntermediate);
            Assert.Equal(new Rect(400, 400, 400, 400), result.SourceRect);
            Assert.Equal(new Rect(100, 100, 200, 200), result.DestinationRect);
            Assert.Equal(new Size(200, 200), result.IntermediateSize);
            Assert.Equal(new Rect(0, 0, 200, 200), result.IntermediateClip);
            Assert.Equal(Matrix.CreateTranslation(-500, -500), result.IntermediateTransform);
        }
    }
}
