using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Layout
{
    public class VirtualLayoutContextAdapter : NonVirtualizingLayoutContext
    {
        private readonly VirtualizingLayoutContext _virtualizingContext;
        private ChildrenCollection _children;

        public VirtualLayoutContextAdapter(VirtualizingLayoutContext virtualizingContext)
        {
            _virtualizingContext = virtualizingContext;
        }

        protected override object LayoutStateCore
        {
            get => _virtualizingContext.LayoutState;
            set => _virtualizingContext.LayoutState = value;
        }

        protected override IReadOnlyList<ILayoutable> ChildrenCore =>
            _children ?? (_children = new ChildrenCollection(_virtualizingContext));

        private class ChildrenCollection : IReadOnlyList<ILayoutable>
        {
            private readonly VirtualizingLayoutContext _context;
            public ChildrenCollection(VirtualizingLayoutContext context) => _context = context;
            public ILayoutable this[int index] => _context.GetOrCreateElementAt(index);
            public int Count => _context.ItemCount;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<ILayoutable> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }
        }
    }
}
