using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    internal class CustomHitTestBorder : Border, ICustomHitTest
    {
        public bool HitTest(Point point)
        {
            // Move hit testing window halfway to the left
            return Bounds
                .WithX(Bounds.X - Bounds.Width / 2)  
                .Contains(point);
        }
    }
}
