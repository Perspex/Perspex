// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for TextContainer.InsertText and InsertEmbeddedObject calls.
//

using System;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // Undo unit for TextContainer.InsertText and InsertEmbeddedObject calls.
    internal class TextTreeInsertUndoUnit : TextTreeUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Create a new undo unit instance.
        // symbolOffset and symbolCount track the offset of the inserted content
        // and its symbol count, respectively.
        internal TextTreeInsertUndoUnit(TextContainer tree, int symbolOffset, int symbolCount) : base(tree, symbolOffset)
        {
            Invariant.Assert(symbolCount > 0, "Creating no-op insert undo unit!");

            _symbolCount = symbolCount;
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

            VerifyTreeContentHashCode();

            start = new TextPointer(this.TextContainer, this.SymbolOffset, LogicalDirection.Forward);
            end = new TextPointer(this.TextContainer, this.SymbolOffset + _symbolCount, LogicalDirection.Forward);

            this.TextContainer.DeleteContentInternal(start, end);
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Count of symbols to remove.
        private readonly int _symbolCount;

        #endregion Private Fields
    }
}

