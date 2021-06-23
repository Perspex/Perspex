// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System.Collections.Generic;

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents the base class for layout context types that do not support virtualization.
    /// </summary>
    public abstract class NonVirtualizingLayoutContext : LayoutContext
    {
        private VirtualizingLayoutContext _contextAdapter;

        /// <summary>
        /// Gets the collection of child controls from the container that provides the context.
        /// </summary>
        public IReadOnlyList<ILayoutable> Children => ChildrenCore;

        /// <summary>
        /// Implements the behavior for getting the return value of <see cref="Children"/> in a
        /// derived or custom <see cref="NonVirtualizingLayoutContext"/>.
        /// </summary>
        protected abstract IReadOnlyList<ILayoutable> ChildrenCore { get; }

        internal VirtualizingLayoutContext GetVirtualizingContextAdapter() =>
            _contextAdapter ?? (_contextAdapter = new LayoutContextAdapter(this));
    }
}
