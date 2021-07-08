using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Subjects;
using Avalonia.Reactive;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Data.Core
{
    /// <summary>
    /// A binding expression which uses delegates to read and write a bound value.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <remarks>
    /// A <see cref="TypedBindingExpression{TIn, TOut}"/> represents a typed binding which has been
    /// instantiated on an object.
    /// </remarks>
    public class TypedBindingExpression<TIn, TOut> : LightweightObservableBase<BindingValue<TOut>>,
        ISubject<BindingValue<TOut>>,
        IDescription
            where TIn : class
    {
        private readonly IObservable<TIn> _rootSource;
        private readonly Func<TIn, TOut> _read;
        private readonly Action<TIn, TOut>? _write;
        private readonly Link[]? _chain;
        private readonly Optional<TOut> _fallbackValue;
        private IDisposable? _rootSourceSubsciption;
        private WeakReference<TIn>? _root;
        private Flags _flags;
        private int _publishCount;

        public TypedBindingExpression(
            IObservable<TIn> root,
            Func<TIn, TOut> read,
            Action<TIn, TOut>? write,
            Func<TIn, object>[] links,
            Optional<TOut> fallbackValue)
        {
            _rootSource = root ?? throw new ArgumentNullException(nameof(root));
            _read = read;
            _write = write;
            _fallbackValue = fallbackValue;

            if (links != null)
            {
                _chain = new Link[links.Length];

                for (var i = 0; i < links.Length; ++i)
                {
                    _chain[i] = new Link(links[i]);
                }
            }
        }

        public string Description => "TODO";

        public void OnNext(BindingValue<TOut> value)
        {
            if (value.HasValue &&
                _write is object &&
                _root is object &&
                _root.TryGetTarget(out var root))
            {
                try
                {
                    var c = _publishCount;
                    if ((_flags & Flags.Initialized) != 0)
                        _write.Invoke(root, value.Value);
                    if (_publishCount == c)
                        PublishValue();
                }
                catch
                {
                    PublishValue();
                }
            }
        }

        void IObserver<BindingValue<TOut>>.OnCompleted()
        {
        }

        void IObserver<BindingValue<TOut>>.OnError(Exception error)
        {
        }

        protected override void Initialize()
        {
            _flags &= ~Flags.RootHasFired;
            _rootSourceSubsciption = _rootSource.Subscribe(RootChanged);
            _flags |= Flags.Initialized;
        }

        protected override void Deinitialize()
        {
            StopListeningToChain(0);
            _rootSourceSubsciption?.Dispose();
            _rootSourceSubsciption = null;
            _flags &= ~Flags.Initialized;
        }

        protected override void Subscribed(IObserver<BindingValue<TOut>> observer, bool first)
        {
            // If this is the first subscription, `Initialize()` will have run which will
            // already have produced a value.
            if (!first)
            {
                var result = GetResult();

                if (result.HasValue)
                {
                    observer.OnNext(result.Value);
                }
            }
        }

        private void RootChanged(TIn value)
        {
            _root = new WeakReference<TIn>(value);
            _flags |= Flags.RootHasFired;
            StopListeningToChain(0);
            ListenToChain(0);
            PublishValue();
        }

        private void ListenToChain(int from)
        {
            if (_chain != null && _root != null && _root.TryGetTarget(out var root))
            {
                object? last = null;

                try
                {
                    for (var i = from; i < _chain.Length; ++i)
                    {
                        var o = _chain[i].Eval(root);

                        if (o != last)
                        {
                            _chain[i].Value = new WeakReference<object>(o);

                            if (SubscribeToChanges(o))
                            {
                                last = o;
                            }
                        }
                    }
                }
                catch
                {
                    // Broken expression chain.
                }
                finally
                {
                    _flags |= Flags.ListeningToChain;
                }
            }
        }

        private void StopListeningToChain(int from)
        {
            if ((_flags & Flags.ListeningToChain) == 0)
                return;

            if (_chain != null && _root != null && _root.TryGetTarget(out _))
            {
                for (var i = from; i < _chain.Length; ++i)
                {
                    var link = _chain[i];

                    if (link.Value is object && link.Value.TryGetTarget(out var o))
                    {
                        UnsubscribeToChanges(o);
                    }
                }
            }

            _flags &= ~Flags.ListeningToChain;
        }

        private bool SubscribeToChanges(object o)
        {
            if (o is null)
            {
                return false;
            }

            var result = false;

            if (o is IAvaloniaObject ao)
            {
                WeakEventHandlerManager.Subscribe<IAvaloniaObject, AvaloniaPropertyChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    ao,
                    nameof(ao.PropertyChanged),
                    ChainPropertyChanged);
                result |= true;
            }
            else if (o is INotifyPropertyChanged inpc)
            {
                WeakEventHandlerManager.Subscribe<INotifyPropertyChanged, PropertyChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    ChainPropertyChanged);
                result |= true;
            }

            if (o is INotifyCollectionChanged incc)
            {
                WeakEventHandlerManager.Subscribe<INotifyCollectionChanged, NotifyCollectionChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    incc,
                    nameof(incc.CollectionChanged),
                    ChainCollectionChanged);
                result |= true;
            }

            return result;
        }

        private void UnsubscribeToChanges(object o)
        {
            if (o is null)
            {
                return;
            }

            if (o is IAvaloniaObject ao)
            {
                WeakEventHandlerManager.Unsubscribe<AvaloniaPropertyChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    ao,
                    nameof(ao.PropertyChanged),
                    ChainPropertyChanged);
            }
            else if (o is INotifyPropertyChanged inpc)
            {
                WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    ChainPropertyChanged);
            }

            if (o is INotifyCollectionChanged incc)
            {
                WeakEventHandlerManager.Unsubscribe<NotifyCollectionChangedEventArgs, TypedBindingExpression<TIn, TOut>>(
                    incc,
                    nameof(incc.CollectionChanged),
                    ChainCollectionChanged);
            }
        }

        private BindingValue<TOut>? GetResult()
        {
            if (_root != null && _root.TryGetTarget(out var root))
            {
                try
                {
                    var value = _read(root);
                    return new BindingValue<TOut>(value);
                }
                catch (Exception e)
                {
                    return BindingValue<TOut>.BindingError(e, _fallbackValue);
                }
            }
            else if ((_flags & Flags.RootHasFired) != 0)
            {
                return BindingValue<TOut>.BindingError(new NullReferenceException(), _fallbackValue);
            }
            else
            {
                return null;
            }
        }

        private void PublishValue()
        {
            var result = GetResult();

            if (result.HasValue)
            {
                unchecked
                { ++_publishCount; }
                PublishNext(result.Value);
            }
        }

        private int ChainIndexOf(object o)
        {
            if (_chain != null)
            {
                for (var i = 0; i < _chain.Length; ++i)
                {
                    var link = _chain[i];

                    if (link.Value != null &&
                        link.Value.TryGetTarget(out var q) &&
                        ReferenceEquals(o, q))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void ChainPropertyChanged(object sender)
        {
            var index = ChainIndexOf(sender);

            if (index != -1)
            {
                StopListeningToChain(index);
                ListenToChain(index);
            }

            PublishValue();
        }

        private void ChainPropertyChanged(object sender, PropertyChangedEventArgs e) => ChainPropertyChanged(sender);
        private void ChainPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e) => ChainPropertyChanged(sender);
        private void ChainCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => ChainPropertyChanged(sender);

        private struct Link
        {
            public Link(Func<TIn, object> eval)
            {
                Eval = eval;
                Value = null;
            }

            public Func<TIn, object> Eval;
            public WeakReference<object>? Value;
        }

        [Flags]
        private enum Flags
        {
            Initialized = 0x01,
            RootHasFired = 0x02,
            ListeningToChain = 0x04,
        }
    }
}
