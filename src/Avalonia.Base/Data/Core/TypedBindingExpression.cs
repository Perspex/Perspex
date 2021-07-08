using System;
using System.Linq.Expressions;
using Avalonia.Data.Core.Parsers;

#nullable enable

namespace Avalonia.Data.Core
{
    /// <summary>
    /// Provides factory methods for creating <see cref="TypedBindingExpression{TIn, TOut}"/>
    /// objects from C# lambda expressions.
    /// </summary>
    public static class TypedBindingExpression
    {
        public static TypedBindingExpression<TIn, TOut> OneWay<TIn, TOut>(
            TIn root,
            Expression<Func<TIn, TOut>> read,
            Optional<TOut> fallbackValue = default)
                where TIn : class
        {
            return OneWay(new Single<TIn>(root), read, fallbackValue);
        }

        public static TypedBindingExpression<TIn, TOut> OneWay<TIn, TOut>(
            IObservable<TIn> root,
            Expression<Func<TIn, TOut>> read,
            Optional<TOut> fallbackValue = default)
                where TIn : class
        {
            return new TypedBindingExpression<TIn, TOut>(
                root,
                read.Compile(),
                null,
                ExpressionChainVisitor<TIn>.Build(read),
                fallbackValue);
        }

        public static TypedBindingExpression<TIn, TConverted> OneWay<TIn, TOut, TConverted>(
            TIn root,
            Expression<Func<TIn, TOut>> read,
            Func<TOut, TConverted> convert,
            Optional<TConverted> fallbackValue = default)
                where TIn : class
        {
            return OneWay(new Single<TIn>(root), read, convert, fallbackValue);
        }

        public static TypedBindingExpression<TIn, TConverted> OneWay<TIn, TOut, TConverted>(
            IObservable<TIn> root,
            Expression<Func<TIn, TOut>> read,
            Func<TOut, TConverted> convert,
            Optional<TConverted> fallbackValue = default)
                where TIn : class
        {
            var compiledRead = read.Compile();

            return new TypedBindingExpression<TIn, TConverted>(
                root,
                x => convert(compiledRead(x)),
                null,
                ExpressionChainVisitor<TIn>.Build(read),
                fallbackValue);
        }

        public static TypedBindingExpression<TIn, TOut> TwoWay<TIn, TOut>(
            TIn root,
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write,
            Optional<TOut> fallbackValue = default)
                where TIn : class
        {
            return TwoWay(new Single<TIn>(root), read, write, fallbackValue);
        }

        public static TypedBindingExpression<TIn, TOut> TwoWay<TIn, TOut>(
            IObservable<TIn> root,
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write,
            Optional<TOut> fallbackValue = default)
                where TIn : class
        {
            return new TypedBindingExpression<TIn, TOut>(
                root,
                read.Compile(),
                write,
                ExpressionChainVisitor<TIn>.Build(read),
                fallbackValue);
        }

        public static TypedBindingExpression<TIn, TConverted> TwoWay<TIn, TOut, TConverted>(
            TIn root,
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write,
            Func<TOut, TConverted> convert,
            Func<TConverted, TOut> convertBack,
            Optional<TConverted> fallbackValue = default)
                where TIn : class
        {
            return TwoWay(new Single<TIn>(root), read, write, convert, convertBack, fallbackValue);
        }

        public static TypedBindingExpression<TIn, TConverted> TwoWay<TIn, TOut, TConverted>(
            IObservable<TIn> root,
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write,
            Func<TOut, TConverted> convert,
            Func<TConverted, TOut> convertBack,
            Optional<TConverted> fallbackValue = default)
                where TIn : class
        {
            var compiledRead = read.Compile();

            return new TypedBindingExpression<TIn, TConverted>(
                root,
                x => convert(compiledRead(x)),
                (o, v) => write(o, convertBack(v)),
                ExpressionChainVisitor<TIn>.Build(read),
                fallbackValue);
        }

        private class Single<T> : IObservable<T>, IDisposable where T : class
        {
            private WeakReference<T> _value;

            public Single(T value) => _value = new WeakReference<T>(value);

            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (_value.TryGetTarget(out var value))
                {
                    observer.OnNext(value);
                }
                else
                {
                    observer.OnNext(default);
                }

                return this;
            }

            public void Dispose() { }
        }
    }
}
