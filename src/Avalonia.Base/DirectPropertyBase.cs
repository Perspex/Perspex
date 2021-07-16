using System;
using Avalonia.Data;
using Avalonia.Reactive;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Base class for direct properties.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Whereas <see cref="DirectProperty{TOwner, TValue}"/> is typed on the owner type, this base
    /// class provides a non-owner-typed interface to a direct property.
    /// </remarks>
    public abstract class DirectPropertyBase<TValue> : AvaloniaProperty<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        protected DirectPropertyBase(
            string name,
            Type ownerType,
            AvaloniaPropertyMetadata metadata)
            : base(name, ownerType, metadata)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        [Obsolete("Use constructor with DirectPropertyBase<TValue> instead.", true)]
        protected DirectPropertyBase(
            AvaloniaProperty source,
            Type ownerType,
            AvaloniaPropertyMetadata metadata)
            : this(source as DirectPropertyBase<TValue> ?? throw new InvalidOperationException(), ownerType, metadata)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        protected DirectPropertyBase(
            DirectPropertyBase<TValue> source,
            Type ownerType,
            AvaloniaPropertyMetadata metadata)
            : base(source, ownerType, metadata)
        {
        }

        /// <summary>
        /// Gets the type that registered the property.
        /// </summary>
        public abstract Type Owner { get; }

        /// <summary>
        /// Gets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The property value.</returns>
        internal abstract TValue InvokeGetter(IAvaloniaObject instance);

        /// <summary>
        /// Sets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        internal abstract void InvokeSetter(IAvaloniaObject instance, BindingValue<TValue> value);

        /// <summary>
        /// Gets the unset value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The unset value.</returns>
        public TValue GetUnsetValue(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            return GetMetadata(type).UnsetValue;
        }

        /// <summary>
        /// Gets the property metadata for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The property metadata.
        /// </returns>
        public new DirectPropertyMetadata<TValue> GetMetadata(Type type)
        {
            return (DirectPropertyMetadata<TValue>)base.GetMetadata(type);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(DirectPropertyMetadata<TValue> metadata) where T : IAvaloniaObject
        {
            base.OverrideMetadata(typeof(T), metadata);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, DirectPropertyMetadata<TValue> metadata)
        {
            base.OverrideMetadata(type, metadata);
        }

        /// <inheritdoc/>
        public override void Accept<TData>(IAvaloniaPropertyVisitor<TData> visitor, ref TData data)
        {
            visitor.Visit(this, ref data);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(IAvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(IAvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        internal override object? RouteGetBaseValue(IAvaloniaObject o, BindingPriority maxPriority)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override IDisposable? RouteSetValue(
            IAvaloniaObject o,
            object value,
            BindingPriority priority)
        {
            var v = TryConvert(value);

            if (v.HasValue)
            {
                o.SetValue<TValue>(this, (TValue)v.Value);
            }
            else if (v.Type == BindingValueType.UnsetValue)
            {
                o.ClearValue(this);
            }
            else if (v.HasError)
            {
                throw v.Error!;
            }

            return null;
        }

        /// <inheritdoc/>
        internal override IDisposable RouteBind(
            IAvaloniaObject o,
            IObservable<BindingValue<object>> source,
            BindingPriority priority)
        {
            var adapter = TypedBindingAdapter<TValue>.Create(o, this, source);
            return o.Bind<TValue>(this, adapter);
        }

        internal override void RouteInheritanceParentChanged(AvaloniaObject o, IAvaloniaObject oldParent)
        {
            throw new NotSupportedException("Direct properties do not support inheritance.");
        }
    }
}
