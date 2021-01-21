// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using Avalonia.Controls.Selection;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides notification that a tree element has been prepared for use.
    /// </summary>
    public class TreeElementPreparedEventArgs
    {
        public TreeElementPreparedEventArgs(IControl element, IndexPath index)
        {
            Element = element;
            Index = index;
        }

        /// <summary>
        /// Gets the prepared element.
        /// </summary>
        public IControl Element { get; private set; }

        /// <summary>
        /// Gets the index of the item the element was prepared for.
        /// </summary>
        public IndexPath Index { get; private set; }
    }
}
