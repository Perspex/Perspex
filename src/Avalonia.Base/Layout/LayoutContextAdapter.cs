// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Layout
{
    internal class LayoutContextAdapter : VirtualizingLayoutContext
    {
        private readonly NonVirtualizingLayoutContext _nonVirtualizingContext;

        public LayoutContextAdapter(NonVirtualizingLayoutContext nonVirtualizingContext)
        {
            _nonVirtualizingContext = nonVirtualizingContext;
        }

        protected override object LayoutStateCore 
        { 
            get => _nonVirtualizingContext.LayoutState;
            set => _nonVirtualizingContext.LayoutState = value; 
        }

        protected override Point LayoutOriginCore 
        {
            get => default;
            set 
            { 
                if (value != default)
                {
                    throw new InvalidOperationException("LayoutOrigin must be at (0,0) when RealizationRect is infinite sized.");
                }
            }
        }

        protected override Rect RealizationRectCore() => new Rect(Size.Infinity);

        protected override int ItemCountCore() => _nonVirtualizingContext.Children.Count;
        protected override object GetItemAtCore(int index) => _nonVirtualizingContext.Children[index];
        protected override ILayoutable GetOrCreateElementAtCore(int index, ElementRealizationOptions options) =>
            _nonVirtualizingContext.Children[index];
        protected override void RecycleElementCore(ILayoutable element) { }
    }
}
