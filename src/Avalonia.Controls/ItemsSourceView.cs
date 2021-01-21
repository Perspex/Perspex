// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a standardized view of the supported interactions between a given ItemsSource
    /// object and an <see cref="ItemsRepeater"/> control.
    /// </summary>
    /// <remarks>
    /// Components written to work with ItemsRepeater should consume the
    /// <see cref="ItemsRepeater.Items"/> via ItemsSourceView since this provides a normalized
    /// view of the Items. That way, each component does not need to know if the source is an
    /// IEnumerable, an IList, or something else.
    /// </remarks>
    public class ItemsSourceView : INotifyCollectionChanged,
        IDisposable,
        IReadOnlyList<object?>,
        ICollectionChangedListener
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public static ItemsSourceView Empty { get; } = new ItemsSourceView(Array.Empty<object?>());

        private readonly IList? _list;
        private readonly IReadOnlyList<object?>? _readOnlyList;
        private readonly INotifyCollectionChanged? _incc;
        private NotifyCollectionChangedEventHandler? _collectionChanged;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            if (source is ItemsSourceView)
            {
                throw new InvalidOperationException("Cannot wrap an ItemsSourceView in another.");
            }

            if (source is IList list)
            {
                _list = list;
                _incc = source as INotifyCollectionChanged;
            }
            else if (source is IReadOnlyList<object?> readOnlyList)
            {
                _readOnlyList = readOnlyList;
                _incc = source as INotifyCollectionChanged;
            }
            else if (source is IEnumerable<object?> objectEnumerable)
            {
                _list = new List<object?>(objectEnumerable);
            }
            else
            {
                _list = new List<object?>(source.Cast<object?>());
            }
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => _list?.Count ?? _readOnlyList!.Count;

        /// <summary>
        /// Gets a value that indicates whether the items source can provide a unique key for each item.
        /// </summary>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public bool HasKeyIndexMapping => false;

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? this[int index] => GetAt(index);

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                if (_incc is object)
                {
                    if (_collectionChanged is null)
                    {
                        CollectionChangedEventManager.Instance.AddListener(_incc, this);
                    }

                    _collectionChanged += value;
                }
            }
            remove
            {
                if (_incc is object && _collectionChanged is object)
                {
                    _collectionChanged -= value;

                    if (_collectionChanged is null && _incc is object)
                    {
                        CollectionChangedEventManager.Instance.RemoveListener(_incc, this);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_collectionChanged is object && _incc is object)
            {
                CollectionChangedEventManager.Instance.RemoveListener(_incc, this);
            }

            _collectionChanged = null;
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? GetAt(int index) => _readOnlyList is object ? _readOnlyList[index] : _list![index];

        /// <summary>
        /// Gets the index of the specified item in the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index of the item if -1 if not present.</returns>
        public int IndexOf(object? item) => _readOnlyList is object ?
            _readOnlyList.IndexOf(item) : _list!.IndexOf(item);

        /// <summary>
        /// Gets an <see cref="ItemsSourceView"/> for the source items, or null if
        /// <paramref name="items"/> is null.
        /// </summary>
        /// <param name="items">The source items.</param>
        public static ItemsSourceView? GetOrCreate(IEnumerable? items)
        {
            if (items is ItemsSourceView isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return null;
            }
            else
            {
                return new ItemsSourceView(items);
            }
        }

        /// <summary>
        /// Gets an <see cref="ItemsSourceView"/> for the source items, or <see cref="Empty"/> if
        /// <paramref name="items"/> is null.
        /// </summary>
        /// <param name="items">The source items.</param>
        public static ItemsSourceView GetOrCreateOrEmpty(IEnumerable? items)
        {
            if (items is ItemsSourceView isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return Empty;
            }
            else
            {
                return new ItemsSourceView(items);
            }
        }

        /// <summary>
        /// Retrieves the index of the item that has the specified unique identifier (key).
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The key</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public string KeyFromIndex(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the unique identifier (key) for the item at the specified index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The index.</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public int IndexFromKey(string key)
        {
            throw new NotImplementedException();
        }

        internal void AddListener(ICollectionChangedListener listener)
        {
            if (_list is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.AddListener(incc, listener);
            }
        }

        internal void RemoveListener(ICollectionChangedListener listener)
        {
            if (_list is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.RemoveListener(incc, listener);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => _readOnlyList?.GetEnumerator() ?? _list!.GetEnumerator();
        IEnumerator<object?> IEnumerable<object?>.GetEnumerator() => GetEnumerator();

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            // Raise CollectionChanged after all listeners who subscribed via AddListener have had
            // chance to handle the event in PreChanged and Changed.
            _collectionChanged?.Invoke(this, e);
        }

        public struct Enumerator : IEnumerator<object?>
        {
            private IEnumerator _innerEnumerator;
            public Enumerator(IEnumerable inner) => _innerEnumerator = inner.GetEnumerator();
            public object Current => _innerEnumerator.Current;
            object IEnumerator.Current => Current;
            public void Dispose() => (_innerEnumerator as IDisposable)?.Dispose();
            public bool MoveNext() => _innerEnumerator.MoveNext();
            void IEnumerator.Reset() => _innerEnumerator.Reset();
        }
    }

    public class ItemsSourceView<T> : ItemsSourceView, IReadOnlyList<T>
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public new static ItemsSourceView<T> Empty { get; } = new ItemsSourceView<T>(Array.Empty<T>());

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable<T> source)
            : base(source)
        {
        }

        private ItemsSourceView(IEnumerable source)
            : base(source)
        {
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
#pragma warning disable CS8603
        public new T this[int index] => GetAt(index);
#pragma warning restore CS8603

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        [return: MaybeNull]
        public new T GetAt(int index) => (T)base.GetAt(index);

        public new IEnumerator<T> GetEnumerator() => ((IEnumerable)this).Cast<T>().GetEnumerator();

        /// <summary>
        /// Gets an <see cref="ItemsSourceView{T}"/> for the source items, or null if
        /// <paramref name="items"/> is null.
        /// </summary>
        /// <param name="items">The source items.</param>
        public static new ItemsSourceView<T>? GetOrCreate(IEnumerable? items)
        {
            if (items is ItemsSourceView<T> isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return null;
            }
            else
            {
                return new ItemsSourceView<T>(items);
            }
        }

        /// <summary>
        /// Gets an <see cref="ItemsSourceView{T}"/> for the source items, or <see cref="Empty"/> if
        /// <paramref name="items"/> is null.
        /// </summary>
        /// <param name="items">The source items.</param>
        public static new ItemsSourceView<T> GetOrCreateOrEmpty(IEnumerable? items)
        {
            if (items is ItemsSourceView<T> isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return Empty;
            }
            else
            {
                return new ItemsSourceView<T>(items);
            }
        }
    }
}
