using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="RowDefinition"/>s.
    /// </summary>
    public class RowDefinitions : DefinitionList<RowDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        public RowDefinitions() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the row definitions.</param>
        public RowDefinitions(string s)
            : this()
        {
            AddRange(GridLength.ParseLengths(s).Select(x => new RowDefinition(x)));
        }

        /// <summary>
        /// Parses a string representation of row definitions collection.
        /// </summary>
        /// <param name="s">The row definitions string.</param>
        /// <returns>The <see cref="RowDefinitions"/>.</returns>
        public static RowDefinitions Parse(string s) => new RowDefinitions(s);

        public override void UpdateParentGrid()
        {
            if (Parent is null) return;
            Parent.RowIsEmpty = this.Count == 0;
            Parent.RowOrColumnDefChanged = true;
            Parent.Invalidate();
        }
    }
}