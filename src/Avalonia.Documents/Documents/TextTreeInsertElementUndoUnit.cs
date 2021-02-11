// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for TextContainer.InsertElement calls.
//

using System;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // Undo unit for TextContainer.InsertElement calls.
    internal class TextTreeInsertElementUndoUnit : TextTreeUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new undo unit instance.
        // symbolOffset should be just before the start edge of the TextElement to remove.
        // If deep is true, this unit will undo not only the scoping element
        // insert, but also all content scoped by the element.
        internal TextTreeInsertElementUndoUnit(TextContainer tree, int symbolOffset, bool deep) : base(tree, symbolOffset)
        {
            _deep = deep;
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // Called by the undo manager.  Restores tree state to its condition
        // when the unit was created.  Assumes the tree state matches conditions
        // just after the unit was created.
        public override void DoCore()
        {
            TextPointer start;
            TextPointer end;
            TextElement element;

            VerifyTreeContentHashCode();

            start = new TextPointer(this.TextContainer, this.SymbolOffset, LogicalDirection.Forward);

            Invariant.Assert(start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart, "TextTree undo unit out of sync with TextTree.");

            element = start.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);

            if (_deep)
            {
                // Extract the element and its content.
                end = new TextPointer(this.TextContainer, element.TextElementNode, ElementEdge.AfterEnd);
                this.TextContainer.DeleteContentInternal(start, end);
            }
            else
            {
                // Just extract the element, not its content.
                this.TextContainer.ExtractElementInternal(element);
            }
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // If true, this unit tracks a TextElement and its scoped content.
        // Otherwise, this unit only tracks the TextElement.
        private readonly bool _deep;

        #endregion Private Fields
    }
}

