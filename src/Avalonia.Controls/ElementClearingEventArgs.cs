// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides notification that a recyclable element has been cleared to a recycling pool.
    /// </summary>
    public class ElementClearingEventArgs : EventArgs
    {
        public ElementClearingEventArgs(IControl element) => Element = element;

        /// <summary>
        /// Gets the element that is being cleared for re-use.
        /// </summary>
        public IControl Element { get; private set; }

        internal void Update(IControl element) => Element = element;
    }
}
