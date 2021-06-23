using System;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Base.UnitTests.Layout
{
    internal class LayoutTestControl : Decorator
    {
        public bool Measured { get; set; }
        public bool Arranged { get; set; }
        public Func<ILayoutable, Size, Size> DoMeasureOverride { get; set; }
        public Func<ILayoutable, Size, Size> DoArrangeOverride { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            Measured = true;
            return DoMeasureOverride != null ?
                DoMeasureOverride(this, availableSize) :
                base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Arranged = true;
            return DoArrangeOverride != null ?
                DoArrangeOverride(this, finalSize) :
                base.ArrangeOverride(finalSize);
        }
    }
}
