using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Styling
{
    /// <summary>
    /// Extension methods for <see cref="Selector"/>.
    /// </summary>
    public static class Selectors
    {
        /// <summary>
        /// Returns a selector which matches a previous selector's child.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Child(this Selector previous)
        {
            return new ChildSelector(previous);
        }

        /// <summary>
        /// Returns a selector which matches a control's style class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name of the style class.</param>
        /// <returns>The selector.</returns>
        public static Selector Class(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(name));

            var tac = previous as TypeNameAndClassSelector;

            if (tac != null)
            {
                tac.Classes.Add(name);
                return tac;
            }
            else
            {
                return TypeNameAndClassSelector.ForClass(previous, name);
            }
        }

        /// <summary>
        /// Returns a selector which matches a descendant of a previous selector.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Descendant(this Selector previous)
        {
            return new DescendantSelector(previous);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector Is(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            return TypeNameAndClassSelector.Is(previous, type);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Is<T>(this Selector previous) where T : IStyleable
        {
            return previous.Is(typeof(T));
        }

        /// <summary>
        /// Returns a selector which matches a control's Name.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name.</param>
        /// <returns>The selector.</returns>
        public static Selector Name(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(name));

            var tac = previous as TypeNameAndClassSelector;

            if (tac != null)
            {
                tac.Name = name;
                return tac;
            }
            else
            {
                return TypeNameAndClassSelector.ForName(previous, name);
            }
        }

        /// <summary>
        /// Returns a selector which inverts the results of selector argument.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        /// <returns>The selector.</returns>
        public static Selector Not(this Selector previous, Func<Selector, Selector> argument)
        {
            return new NotSelector(previous, argument(null));
        }
        
        /// <summary>
        /// Returns a selector which inverts the results of selector argument.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        /// <returns>The selector.</returns>
        public static Selector Not(this Selector previous, Selector argument)
        {
            return new NotSelector(previous, argument);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            return TypeNameAndClassSelector.OfType(previous, type);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType<T>(this Selector previous) where T : IStyleable
        {
            return previous.OfType(typeof(T));
        }

        /// <summary>
        /// Returns a selector which ORs selectors.
        /// </summary>
        /// <param name="selectors">The selectors to be OR'd.</param>
        /// <returns>The selector.</returns>
        public static Selector Or(params Selector[] selectors)
        {
            return new OrSelector(selectors);
        }

        /// <summary>
        /// Returns a selector which ORs selectors.
        /// </summary>
        /// <param name="selectors">The selectors to be OR'd.</param>
        /// <returns>The selector.</returns>
        public static Selector Or(IReadOnlyList<Selector> selectors)
        {
            return new OrSelector(selectors);
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals<T>(this Selector previous, AvaloniaProperty<T> property, object value)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return new PropertyEqualsSelector(previous, property, value);
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals(this Selector previous, AvaloniaProperty property, object value)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return new PropertyEqualsSelector(previous, property, value);
        }

        /// <summary>
        /// Returns a selector which enters a lookless control's template.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Template(this Selector previous)
        {
            return new TemplateSelector(previous);
        }
    }
}
