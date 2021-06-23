using System;
using Avalonia.Layout;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Layout
{
    internal class LayoutTestRoot : TestRoot, ILayoutable
    {
        public bool Measured { get; set; }
        public bool Arranged { get; set; }
        public Func<ILayoutable, Size, Size> DoMeasureOverride { get; set; }
        public Func<ILayoutable, Size, Size> DoArrangeOverride { get; set; }

        void ILayoutable.Measure(Size availableSize)
        {
            Measured = true;
            Measure(availableSize);
        }

        void ILayoutable.Arrange(Rect rect)
        {
            Arranged = true;
            Arrange(rect);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
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
