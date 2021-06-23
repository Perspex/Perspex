using Avalonia.Controls;
using Avalonia.Rendering;

namespace Avalonia.Base.UnitTests.Rendering
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
