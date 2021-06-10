using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class TreeNodeCollection : IAvaloniaReadOnlyList<TreeNode>, IDisposable
    {
        private AvaloniaList<TreeNode>? _inner;

        public TreeNodeCollection(TreeNode owner) => Owner = owner;

        public TreeNode this[int index] => EnsureInitialized()[index];

        public int Count => EnsureInitialized().Count;

        protected TreeNode Owner { get; }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => EnsureInitialized().CollectionChanged += value;
            remove => EnsureInitialized().CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => EnsureInitialized().PropertyChanged += value;
            remove => EnsureInitialized().PropertyChanged -= value;
        }

        public virtual void Dispose()
        {
            if (_inner is object)
            {
                foreach (var node in _inner)
                {
                    node.Dispose();
                }
            }
        }

        public IEnumerator<TreeNode> GetEnumerator()
        {
            return EnsureInitialized().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract void Initialize(AvaloniaList<TreeNode> nodes);

        private AvaloniaList<TreeNode> EnsureInitialized()
        {
            if (_inner is null)
            {
                _inner = new AvaloniaList<TreeNode>();
                Initialize(_inner);
            }
            return _inner;
        }
    }
}
