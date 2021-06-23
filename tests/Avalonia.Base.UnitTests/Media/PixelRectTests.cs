using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class PixelRectTests
    {
        [Fact]
        public void FromRect_Snaps_To_Device_Pixels()
        {
            var rect = new Rect(189, 189, 26, 164);
            var result = PixelRect.FromRect(rect, 1.5);

            Assert.Equal(new PixelRect(283, 283, 40, 247), result);
        }

        [Fact]
        public void FromRect_Vector_Snaps_To_Device_Pixels()
        {
            var rect = new Rect(189, 189, 26, 164);
            var result = PixelRect.FromRect(rect, new Vector(1.5, 1.5));

            Assert.Equal(new PixelRect(283, 283, 40, 247), result);
        }

        [Fact]
        public void FromRectWithDpi_Snaps_To_Device_Pixels()
        {
            var rect = new Rect(189, 189, 26, 164);
            var result = PixelRect.FromRectWithDpi(rect, 144);

            Assert.Equal(new PixelRect(283, 283, 40, 247), result);
        }

        [Fact]
        public void FromRectWithDpi_Vector_Snaps_To_Device_Pixels()
        {
            var rect = new Rect(189, 189, 26, 164);
            var result = PixelRect.FromRectWithDpi(rect, new Vector(144, 144));

            Assert.Equal(new PixelRect(283, 283, 40, 247), result);
        }
    }
}
