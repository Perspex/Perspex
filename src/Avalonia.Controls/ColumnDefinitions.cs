using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="ColumnDefinition"/>s.
    /// </summary>
    public class ColumnDefinitions : DefinitionList<ColumnDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        public ColumnDefinitions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the column definitions.</param>
        public ColumnDefinitions(string s)
            : this()
        {
            AddRange(GridLength.ParseLengths(s).Select(x => new ColumnDefinition(x)));
        }

        public override string ToString()
        {
            return string.Join(",", this.Select(x => x.Width));
        }

        /// <summary>
        /// Parses a string representation of column definitions collection.
        /// </summary>
        /// <param name="s">The column definitions string.</param>
        /// <returns>The <see cref="ColumnDefinitions"/>.</returns>
        public static ColumnDefinitions Parse(string s) => new ColumnDefinitions(s);

        public override void UpdateParentGrid()
        {
            if (Parent is null) return;
            Parent.ColIsEmpty = this.Count == 0;
            Parent.RowOrColumnDefChanged = true;
            Parent.Invalidate();
        }
    }
}
