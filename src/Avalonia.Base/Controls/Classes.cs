using System;
using System.Collections.Generic;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds a collection of style classes for an <see cref="IStyledElement"/>.
    /// </summary>
    /// <remarks>
    /// Similar to CSS, each control may have any number of styling classes applied.
    /// </remarks>
    public class Classes : AvaloniaList<string>, IPseudoClasses
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        public Classes()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        /// <param name="items">The initial items.</param>
        public Classes(IEnumerable<string> items)
            : base(items)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        /// <param name="items">The initial items.</param>
        public Classes(params string[] items)
            : base(items)
        {            
        }

        /// <summary>
        /// Parses a classes string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="Classes"/>.</returns>
        public static Classes Parse(string s) => new Classes(s.Split(' '));

        /// <summary>
        /// Adds a style class to the collection.
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void Add(string name)
        {
            ThrowIfPseudoclass(name, "added");

            if (!Contains(name))
            {
                base.Add(name);
            }
        }

        /// <summary>
        /// Adds a style classes to the collection.
        /// </summary>
        /// <param name="names">The class names.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void AddRange(IEnumerable<string> names)
        {
            var c = new List<string>();

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "added");

                if (!Contains(name))
                {
                    c.Add(name);
                }
            }

            base.AddRange(c);
        }

        /// <summary>
        /// Removes all non-pseudoclasses from the collection.
        /// </summary>
        public override void Clear()
        {
            for (var i = Count - 1; i >= 0; --i)
            {
                if (!this[i].StartsWith(":"))
                {
                    RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Inserts a style class into the collection.
        /// </summary>
        /// <param name="index">The index to insert the class at.</param>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void Insert(int index, string name)
        {
            ThrowIfPseudoclass(name, "added");

            if (!Contains(name))
            {
                base.Insert(index, name);
            }
        }

        /// <summary>
        /// Inserts style classes into the collection.
        /// </summary>
        /// <param name="index">The index to insert the class at.</param>
        /// <param name="names">The class names.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void InsertRange(int index, IEnumerable<string> names)
        {
            List<string>? toInsert = null;

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "added");

                if (!Contains(name))
                {
                    toInsert ??= new List<string>();

                    toInsert.Add(name);
                }
            }

            if (toInsert != null)
            {
                base.InsertRange(index, toInsert);
            }
        }

        /// <summary>
        /// Removes a style class from the collection.
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override bool Remove(string name)
        {
            ThrowIfPseudoclass(name, "removed");
            return base.Remove(name);
        }

        /// <summary>
        /// Removes style classes from the collection.
        /// </summary>
        /// <param name="names">The class name.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void RemoveAll(IEnumerable<string> names)
        {
            List<string>? toRemove = null;

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "removed");

                toRemove ??= new List<string>();

                toRemove.Add(name);
            }

            if (toRemove != null)
            {
                base.RemoveAll(toRemove);
            }
        }

        /// <summary>
        /// Removes a style class from the collection.
        /// </summary>
        /// <param name="index">The index of the class in the collection.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void RemoveAt(int index)
        {
            var name = this[index];
            ThrowIfPseudoclass(name, "removed");
            base.RemoveAt(index);
        }

        /// <summary>
        /// Removes style classes from the collection.
        /// </summary>
        /// <param name="index">The first index to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public override void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
        }

        /// <summary>
        /// Removes all non-pseudoclasses in the collection and adds a new set.
        /// </summary>
        /// <param name="source">The new contents of the collection.</param>
        public void Replace(IList<string> source)
        {
            List<string>? toRemove = null;

            foreach (var name in source)
            {
                ThrowIfPseudoclass(name, "added");
            }

            foreach (var name in this)
            {
                if (!name.StartsWith(":"))
                {
                    toRemove ??= new List<string>();

                    toRemove.Add(name);
                }
            }

            if (toRemove != null)
            {
                base.RemoveAll(toRemove);
            }

            base.AddRange(source);
        }

        /// <inheritdoc/>
        void IPseudoClasses.Add(string name)
        {
            if (!Contains(name))
            {
                base.Add(name);
            }
        }

        /// <inheritdoc/>
        bool IPseudoClasses.Remove(string name)
        {
            return base.Remove(name);
        }

        private void ThrowIfPseudoclass(string name, string operation)
        {
            if (name.StartsWith(":"))
            {
                throw new ArgumentException(
                    $"The pseudoclass '{name}' may only be {operation} by the control itself.");
            }
        }

        /// <summary>
        /// Adds a or removes a  style class to/from the collection.
        /// </summary>
        /// <param name="name">The class names.</param>
        /// <param name="value">If true adds the class, if false, removes it.</param>
        /// <remarks>
        /// Only standard classes may be added or removed via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="StyledElement.PseudoClasses"/>
        /// property.
        /// </remarks>
        public void Set(string name, bool value)
        {
            if (value)
            {
                if (!Contains(name))
                    Add(name);
            }
            else
                Remove(name);
        }
    }
}
