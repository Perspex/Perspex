// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using Avalonia.Controls.Selection;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides notification that the index for an element in a tree has changed.
    /// </summary>
    public class TreeElementIndexChangedEventArgs : EventArgs
    {
        public TreeElementIndexChangedEventArgs(IControl element, IndexPath oldIndex, IndexPath newIndex)
        {
            Element = element;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Get the element for which the index changed.
        /// </summary>
        public IControl Element { get; private set; }

        /// <summary>
        /// Gets the index of the element after the change.
        /// </summary>
        public IndexPath NewIndex { get; private set; }

        /// <summary>
        /// Gets the index of the element before the change.
        /// </summary>
        public IndexPath OldIndex { get; private set; }
    }
}
