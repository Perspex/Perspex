using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Data
{
    /// <summary>
    /// Describes the type of a <see cref="BindingValue{T}"/>.
    /// </summary>
    [Flags]
    public enum BindingValueType
    {
        /// <summary>
        /// An unset value: the target property will revert to its unbound state until a new
        /// binding value is produced.
        /// </summary>
        UnsetValue = 0,

        /// <summary>
        /// Do nothing: the binding value will be ignored.
        /// </summary>
        DoNothing = 1,

        /// <summary>
        /// A simple value.
        /// </summary>
        Value = 2 | HasValue,

        /// <summary>
        /// A binding error, such as a missing source property.
        /// </summary>
        BindingError = 3 | HasError,

        /// <summary>
        /// A data validation error.
        /// </summary>
        DataValidationError = 4 | HasError,

        /// <summary>
        /// A binding error with a fallback value.
        /// </summary>
        BindingErrorWithFallback = BindingError | HasValue,

        /// <summary>
        /// A data validation error with a fallback value.
        /// </summary>
        DataValidationErrorWithFallback = DataValidationError | HasValue,

        TypeMask = 0x00ff,
        HasValue = 0x0100,
        HasError = 0x0200,
    }

    /// <summary>
    /// A value passed into a binding.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <remarks>
    /// The avalonia binding system is typed, and as such additional state is stored in this
    /// structure. A binding value can be in a number of states, described by the
    /// <see cref="Type"/> property:
    /// 
    /// - <see cref="BindingValueType.Value"/>: a simple value
    /// - <see cref="BindingValueType.UnsetValue"/>: the target property will revert to its unbound
    ///   state until a new binding value is produced. Represented by
    ///   <see cref="AvaloniaProperty.UnsetValue"/> in an untyped context
    /// - <see cref="BindingValueType.DoNothing"/>: the binding value will be ignored. Represented
    ///   by <see cref="BindingOperations.DoNothing"/> in an untyped context
    /// - <see cref="BindingValueType.BindingError"/>: a binding error, such as a missing source
    ///   property, with an optional fallback value
    /// - <see cref="BindingValueType.DataValidationError"/>: a data validation error, with an
    ///   optional fallback value
    ///   
    /// To create a new binding value you can:
    /// 
    /// - For a simple value, call the <see cref="BindingValue{T}"/> constructor or use an implicit
    ///   conversion from <typeparamref name="T"/>
    /// - For an unset value, use <see cref="Unset"/> or simply `default`
    /// - For other types, call one of the static factory methods
    /// </remarks>
    public readonly struct BindingValue<T>
    {
        [AllowNull] private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingValue{T}"/> struct with a type of
        /// <see cref="BindingValueType.Value"/>
        /// </summary>
        /// <param name="value">The value.</param>
        public BindingValue([AllowNull] T value)
        {
            ValidateValue(value);
            _value = value;
            Type = BindingValueType.Value;
            Error = null;
        }

        private BindingValue(BindingValueType type, [AllowNull] T value, Exception? error)
        {
            _value = value;
            Type = type;
            Error = error;
        }

        /// <summary>
        /// Gets a value indicating whether the binding value represents either a binding or data
        /// validation error.
        /// </summary>
        public bool HasError => Type.HasAllFlags(BindingValueType.HasError);

        /// <summary>
        /// Gets a value indicating whether the binding value has a value.
        /// </summary>
        public bool HasValue => Type.HasAllFlags(BindingValueType.HasValue);

        /// <summary>
        /// Gets the type of the binding value.
        /// </summary>
        public BindingValueType Type { get; }

        /// <summary>
        /// Gets the binding value or fallback value.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="HasValue"/> is false.
        /// </exception>
        public T Value => HasValue ? _value : throw new InvalidOperationException("BindingValue has no value.");

        /// <summary>
        /// Gets the binding or data validation error.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Converts the binding value to an <see cref="Optional{T}"/>.
        /// </summary>
        /// <returns></returns>
        public Optional<T> ToOptional() => HasValue ? new Optional<T>(_value) : default;

        /// <inheritdoc/>
        public override string ToString() => HasError ? $"Error: {Error!.Message}" : _value?.ToString() ?? "(null)";

        /// <summary>
        /// Converts the value to untyped representation, using <see cref="AvaloniaProperty.UnsetValue"/>,
        /// <see cref="BindingOperations.DoNothing"/> and <see cref="BindingNotification"/> where
        /// appropriate.
        /// </summary>
        /// <returns>The untyped representation of the binding value.</returns>
        public object? ToUntyped()
        {
            return Type switch
            {
                BindingValueType.UnsetValue => AvaloniaProperty.UnsetValue,
                BindingValueType.DoNothing => BindingOperations.DoNothing,
                BindingValueType.Value => _value,
                BindingValueType.BindingError =>
                    new BindingNotification(Error, BindingErrorType.Error),
                BindingValueType.BindingErrorWithFallback =>
                    new BindingNotification(Error, BindingErrorType.Error, Value),
                BindingValueType.DataValidationError =>
                    new BindingNotification(Error, BindingErrorType.DataValidationError),
                BindingValueType.DataValidationErrorWithFallback =>
                    new BindingNotification(Error, BindingErrorType.DataValidationError, Value),
                _ => throw new NotSupportedException("Invalid BindingValueType."),
            };
        }

        /// <summary>
        /// Returns a new binding value with the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>The new binding value.</returns>
        /// <exception cref="InvalidOperationException">
        /// The binding type is <see cref="BindingValueType.UnsetValue"/> or
        /// <see cref="BindingValueType.DoNothing"/>.
        /// </exception>
        public BindingValue<T> WithValue([AllowNull] T value)
        {
            if (Type == BindingValueType.DoNothing)
            {
                throw new InvalidOperationException("Cannot add value to DoNothing binding value.");
            }

            var type = Type == BindingValueType.UnsetValue ? BindingValueType.Value : Type;
            return new BindingValue<T>(type | BindingValueType.HasValue, value, Error);
        }

        /// <summary>
        /// Gets the value of the binding value if present, otherwise the default value.
        /// </summary>
        /// <returns>The value.</returns>
        [return: MaybeNull]
        public T GetValueOrDefault() => HasValue ? _value : default;

        /// <summary>
        /// Gets the value of the binding value if present, otherwise a default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value.</returns>
        public T GetValueOrDefault(T defaultValue) => HasValue ? _value : defaultValue;

        /// <summary>
        /// Gets the value if present, otherwise the default value.
        /// </summary>
        /// <returns>
        /// The value if present and of the correct type, `default(TResult)` if the value is
        /// not present or of an incorrect type.
        /// </returns>
        [return: MaybeNull]
        public TResult GetValueOrDefault<TResult>()
        {
            return HasValue ?
                _value is TResult result ? result : default
                : default;
        }

        /// <summary>
        /// Gets the value of the binding value if present, otherwise a default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// The value if present and of the correct type, `default(TResult)` if the value is
        /// present but not of the correct type or null, or <paramref name="defaultValue"/> if the
        /// value is not present.
        /// </returns>
        [return: MaybeNull]
        public TResult GetValueOrDefault<TResult>([AllowNull] TResult defaultValue)
        {
            return HasValue ?
                _value is TResult result ? result : default
                : defaultValue;
        }

        /// <summary>
        /// Creates a <see cref="BindingValue{T}"/> from an object, handling the special values
        /// <see cref="AvaloniaProperty.UnsetValue"/> and <see cref="BindingOperations.DoNothing"/>.
        /// </summary>
        /// <param name="value">The untyped value.</param>
        /// <returns>The typed binding value.</returns>
        public static BindingValue<T> FromUntyped(object? value)
        {
            return value switch
            {
                UnsetValueType _ => Unset,
                DoNothingType _ => DoNothing,
                BindingNotification n => n.ToBindingValue().Cast<T>(),
                _ => new BindingValue<T>((T)value)
            };
        }

        /// <summary>
        /// Creates a binding value from an instance of the underlying value type.
        /// </summary>
        /// <param name="value">The value.</param>
        public static implicit operator BindingValue<T>([AllowNull] T value) => new BindingValue<T>(value);

        /// <summary>
        /// Creates a binding value from an <see cref="Optional{T}"/>.
        /// </summary>
        /// <param name="optional">The optional value.</param>

        public static implicit operator BindingValue<T>(Optional<T> optional)
        {
            return optional.HasValue ? optional.Value : Unset;
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.UnsetValue"/>.
        /// </summary>
        public static BindingValue<T> Unset => new BindingValue<T>(BindingValueType.UnsetValue, default, null);

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.DoNothing"/>.
        /// </summary>
        public static BindingValue<T> DoNothing => new BindingValue<T>(BindingValueType.DoNothing, default, null);

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.BindingError"/>.
        /// </summary>
        /// <param name="e">The binding error.</param>
        public static BindingValue<T> BindingError(Exception e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(BindingValueType.BindingError, default, e);
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.BindingErrorWithFallback"/>.
        /// </summary>
        /// <param name="e">The binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public static BindingValue<T> BindingError(Exception e, T fallbackValue)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(BindingValueType.BindingErrorWithFallback, fallbackValue, e);
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.BindingError"/> or
        /// <see cref="BindingValueType.BindingErrorWithFallback"/>.
        /// </summary>
        /// <param name="e">The binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public static BindingValue<T> BindingError(Exception e, Optional<T> fallbackValue)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(
                fallbackValue.HasValue ?
                    BindingValueType.BindingErrorWithFallback :
                    BindingValueType.BindingError,
                fallbackValue.HasValue ? fallbackValue.Value : default,
                e);
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.DataValidationError"/>.
        /// </summary>
        /// <param name="e">The data validation error.</param>
        public static BindingValue<T> DataValidationError(Exception e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(BindingValueType.DataValidationError, default, e);
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.DataValidationErrorWithFallback"/>.
        /// </summary>
        /// <param name="e">The data validation error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public static BindingValue<T> DataValidationError(Exception e, T fallbackValue)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(BindingValueType.DataValidationErrorWithFallback, fallbackValue, e);
        }

        /// <summary>
        /// Returns a binding value with a type of <see cref="BindingValueType.DataValidationError"/> or
        /// <see cref="BindingValueType.DataValidationErrorWithFallback"/>.
        /// </summary>
        /// <param name="e">The binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public static BindingValue<T> DataValidationError(Exception e, Optional<T> fallbackValue)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            return new BindingValue<T>(
                fallbackValue.HasValue ?
                    BindingValueType.DataValidationErrorWithFallback :
                    BindingValueType.DataValidationError,
                fallbackValue.HasValue ? fallbackValue.Value : default,
                e);
        }

        [Conditional("DEBUG")]
        private static void ValidateValue([AllowNull] T value)
        {
            if (value is UnsetValueType)
            {
                throw new InvalidOperationException("AvaloniaValue.UnsetValue is not a valid value for BindingValue<>.");
            }

            if (value is DoNothingType)
            {
                throw new InvalidOperationException("BindingOperations.DoNothing is not a valid value for BindingValue<>.");
            }

            if (value is BindingValue<object>)
            {
                throw new InvalidOperationException("BindingValue<object> cannot be wrapped in a BindingValue<>.");
            }
        }
    }

    public static class BindingValueExtensions
    {
        /// <summary>
        /// Casts the type of a <see cref="BindingValue{T}"/> using only the C# cast operator.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The binding value.</param>
        /// <returns>The cast value.</returns>
        public static BindingValue<T> Cast<T>(this BindingValue<object> value)
        {
            return value.Type switch
            {
                BindingValueType.DoNothing => BindingValue<T>.DoNothing,
                BindingValueType.UnsetValue => BindingValue<T>.Unset,
                BindingValueType.Value => new BindingValue<T>((T)value.Value),
                BindingValueType.BindingError => BindingValue<T>.BindingError(value.Error!),
                BindingValueType.BindingErrorWithFallback => BindingValue<T>.BindingError(
                        value.Error!,
                        (T)value.Value),
                BindingValueType.DataValidationError => BindingValue<T>.DataValidationError(value.Error!),
                BindingValueType.DataValidationErrorWithFallback => BindingValue<T>.DataValidationError(
                        value.Error!,
                        (T)value.Value),
                _ => throw new NotSupportedException("Invalid BindingValue type."),
            };
        }

        /// <summary>
        /// Casts the type of a <see cref="BindingValue{T}"/> using the implicit conversions
        /// allowed by the C# language.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The binding value.</param>
        /// <returns>The cast value.</returns>
        /// <remarks>
        /// Note that this method uses reflection and as such may be slow.
        /// </remarks>
        public static BindingValue<T> Convert<T>(this BindingValue<object> value)
        {
            return value.Type switch
            {
                BindingValueType.DoNothing => BindingValue<T>.DoNothing,
                BindingValueType.UnsetValue => BindingValue<T>.Unset,
                BindingValueType.Value => new BindingValue<T>(TypeUtilities.ConvertImplicit<T>(value.Value)),
                BindingValueType.BindingError => BindingValue<T>.BindingError(value.Error!),
                BindingValueType.BindingErrorWithFallback => BindingValue<T>.BindingError(
                        value.Error!,
                        TypeUtilities.ConvertImplicit<T>(value.Value)),
                BindingValueType.DataValidationError => BindingValue<T>.DataValidationError(value.Error!),
                BindingValueType.DataValidationErrorWithFallback => BindingValue<T>.DataValidationError(
                        value.Error!,
                        TypeUtilities.ConvertImplicit<T>(value.Value)),
                _ => throw new NotSupportedException("Invalid BindingValue type."),
            };
        }
    }
}
