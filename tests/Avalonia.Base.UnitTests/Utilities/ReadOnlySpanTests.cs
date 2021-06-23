using System.Linq;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class ReadOnlySpanTests
    {
        [Fact]
        public void Should_Skip()
        {
            var buffer = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var slice = new ReadOnlySlice<int>(buffer);

            var skipped = slice.Skip(2);

            var expected = buffer.Skip(2);

            Assert.Equal(expected, skipped);
        }

        [Fact]
        public void Should_Take()
        {
            var buffer = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var slice = new ReadOnlySlice<int>(buffer);

            var taken = slice.Take(8);

            var expected = buffer.Take(8);

            Assert.Equal(expected, taken);
        }
    }
}
