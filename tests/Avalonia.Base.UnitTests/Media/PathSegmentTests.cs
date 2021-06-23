using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class PathSegmentTests
    {
        [Fact]
        public void PathSegment_Triggers_Invalidation_On_Property_Change()
        {
            var targetSegment = new ArcSegment()
            {
                Size = new Size(10, 10),
                Point = new Point(5, 5)
            };

            var target = new PathGeometry
            {
                Figures = new PathFigures
                {
                    new PathFigure { IsClosed = false, Segments = new PathSegments { targetSegment } }
                }
            };
            
            var changed = false;

            target.Changed += (s, e) => changed = true;

            targetSegment.Size = new Size(20, 20);

            Assert.True(changed);
        }
    }
}
