// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A TextContainer node representing a UIElement.
//

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // UIElements in the TextContainer are represented internally by TextTreeObjectNodes.
    //
    // This class is a simple container for a UIElement, it only holds state.
    internal class TextTreeObjectNode : TextTreeNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new TextTreeObjectNode instance.
        internal TextTreeObjectNode(IAvaloniaObject embeddedElement)
        {
            _embeddedElement = embeddedElement;
            _symbolOffsetCache = -1;
        }
 
        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

#if DEBUG
        // Debug-only ToString override.
        public override string ToString()
        {
            return ("ObjectNode Id=" + this.DebugId + " Object=" + _embeddedElement);
        }
#endif // DEBUG

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns a shallow copy of this node.
        internal override TextTreeNode Clone()
        {
            TextTreeObjectNode clone;

            clone = new TextTreeObjectNode(_embeddedElement);

            return clone;
        }

        // Returns the TextPointerContext of the node.
        internal override TextPointerContext GetPointerContext(LogicalDirection direction)
        {
            return TextPointerContext.EmbeddedElement;
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        internal override SplayTreeNode ParentNode
        {
            get
            {
                return _parentNode;
            }
            
            set
            {
                _parentNode = (TextTreeNode)value;
            }
        }

        // TextTreeObjectNode never has contained nodes.
        internal override SplayTreeNode ContainedNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "Can't set contained node on a TextTreeObjectNode!");
            }
        }

        // Count of symbols of all siblings preceding this node.
        internal override int LeftSymbolCount
        {
            get
            {
                return _leftSymbolCount;
            }

            set
            {
                _leftSymbolCount = value;
            }
        }

        // Count of symbols of all siblings preceding this node.
        internal override int LeftCharCount
        {
            get
            {
                return _leftCharCount;
            }

            set
            {
                _leftCharCount = value;
            }
        }

        // Left child node in a sibling tree.
        internal override SplayTreeNode LeftChildNode
        {
            get
            {
                return _leftChildNode;
            }
            
            set
            {
                _leftChildNode = (TextTreeNode)value;
            }
        }

        // Right child node in a sibling tree.
        internal override SplayTreeNode RightChildNode
        {
            get
            {
                return _rightChildNode;
            }
            
            set
            {
                _rightChildNode = (TextTreeNode)value;
            }
        }

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        internal override uint Generation
        {
            get
            {
                return _generation;
            }
            
            set
            {
                _generation = value;
            }
        }

        // Cached symbol offset.
        internal override int SymbolOffsetCache
        {
            get
            {
                return _symbolOffsetCache;
            }
            
            set
            {
                _symbolOffsetCache = value;
            }
        }

        // Count of symbols covered by this node.
        internal override int SymbolCount
        {
            get
            {
                return 1;
            }
            
            set
            {
                Invariant.Assert(false, "Can't set SymbolCount on TextTreeObjectNode!");
            }
        }

        // Count of symbols covered by this node.
        internal override int IMECharCount
        {
            get
            {
                return 1;
            }

            set
            {
                Invariant.Assert(false, "Can't set CharCount on TextTreeObjectNode!");
            }
        }

        // Count of TextPositions referencing the node's BeforeStart edge.
        // Since nodes don't usually have any references, we demand allocate
        // storage when needed.
        internal override bool BeforeStartReferenceCount
        {
            get
            {
                return (_edgeReferenceCounts & ElementEdge.BeforeStart) != 0;
            }

            set
            {
                Invariant.Assert(value); // Illegal to clear a set ref count.
                _edgeReferenceCounts |= ElementEdge.BeforeStart;
            }
        }

        // Count of TextPositions referencing the node's AfterStart edge.
        // Since object nodes don't have an AfterStart edge, this is always zero.
        internal override bool AfterStartReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "Object nodes don't have an AfterStart edge!");
            }
        }

        // Count of TextPositions referencing the node's BeforeEnd edge.
        // Since object nodes don't have an BeforeEnd edge, this is always zero.
        internal override bool BeforeEndReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "Object nodes don't have a BeforeEnd edge!");
            }
        }

        // Count of TextPositions referencing the node's right
        // edge.
        // Since nodes don't usually have any references, we demand allocate
        // storage when needed.
        internal override bool AfterEndReferenceCount
        {
            get
            {
                return (_edgeReferenceCounts & ElementEdge.AfterEnd) != 0;
            }

            set
            {
                Invariant.Assert(value); // Illegal to clear a set ref count.
                _edgeReferenceCounts |= ElementEdge.AfterEnd;
            }
        }

        // The UIElement or ContentElement linked to this node.
        internal IAvaloniaObject EmbeddedElement
        {
            get
            {
                return _embeddedElement;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Count of symbols of all siblings preceding this node.
        private int _leftSymbolCount;

        // Count of chars of all siblings preceding this node.
        private int _leftCharCount;

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        private TextTreeNode _parentNode;

        // Left child node in a sibling tree.
        private TextTreeNode _leftChildNode;

        // Right child node in a sibling tree.
        private TextTreeNode _rightChildNode;

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        private uint _generation;

        // Cached symbol offset.
        private int _symbolOffsetCache;

        // Reference counts of TextPositions referencing this node.
        // Lazy allocated -- null means no references.
        private ElementEdge _edgeReferenceCounts;

        // The UIElement or ContentElement linked to this node.
        private readonly IAvaloniaObject _embeddedElement;

        #endregion Private Fields
    }
}
