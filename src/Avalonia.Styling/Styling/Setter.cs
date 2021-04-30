using System;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Metadata;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A setter for a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="AvaloniaProperty"/> value on a
    /// <see cref="AvaloniaObject"/> depending on a condition.
    /// </remarks>
    public class Setter : ISetter, IAnimationSetter, IAvaloniaPropertyVisitor<Setter.SetterVisitorData>
    {
        private object? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        public Setter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The property value.</param>
        public Setter(AvaloniaProperty property, object value)
        {
            Property = property;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the property to set.
        /// </summary>
        public AvaloniaProperty? Property { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [Content]
        [AssignBinding]
        [DependsOn(nameof(Property))]
        public object? Value
        {
            get => _value;
            set
            {
                (value as ISetterValue)?.Initialize(this);
                _value = value;
            }
        }

        public ISetterInstance Instance(IStyleable target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            if (Property is null)
            {
                throw new InvalidOperationException("Setter.Property must be set.");
            }

            var data = new SetterVisitorData
            {
                target = target,
                value = Value,
            };

            Property.Accept(this, ref data);
            return data.result!;
        }

        void IAvaloniaPropertyVisitor<SetterVisitorData>.Visit<T>(
            StyledPropertyBase<T> property,
            ref SetterVisitorData data)
        {
            if (data.value is IBinding binding)
            {
                data.result = new PropertySetterBindingInstance<T>(
                    data.target,
                    property,
                    binding);
            }
            else if (data.value is ITemplate template && !typeof(ITemplate).IsAssignableFrom(property.PropertyType))
            {
                data.result = new PropertySetterLazyInstance<T>(
                    data.target,
                    property,
                    () => (T)template.Build());
            }
            else
            {
                data.result = new PropertySetterInstance<T>(
                    data.target,
                    property,
#pragma warning disable CS8604 // Possible null reference argument.
                    (T)data.value);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }

        void IAvaloniaPropertyVisitor<SetterVisitorData>.Visit<T>(
            DirectPropertyBase<T> property,
            ref SetterVisitorData data)
        {
            if (data.value is IBinding binding)
            {
                data.result = new PropertySetterBindingInstance<T>(
                    data.target,
                    property,
                    binding);
            }
            else if (data.value is ITemplate template && !typeof(ITemplate).IsAssignableFrom(property.PropertyType))
            {
                data.result = new PropertySetterLazyInstance<T>(
                    data.target,
                    property,
                    () => (T)template.Build());
            }
            else
            {
                data.result = new PropertySetterInstance<T>(
                    data.target,
                    property,
#pragma warning disable CS8604 // Possible null reference argument.
                    (T)data.value);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }

        private struct SetterVisitorData
        {
            public IStyleable target;
            public object? value;
            public ISetterInstance? result;
        }
    }
}
