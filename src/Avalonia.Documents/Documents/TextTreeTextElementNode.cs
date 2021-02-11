// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A TextContainer node representing a TextElement.
//

using System;
using Avalonia.Documents;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // Each TextElement inserted though a public API is represented internally
    // by a TextTreeTextElementNode.
    //
    // TextTreeTextElementNodes may contain trees of child nodes, nodes in
    // a contained tree are scoped by the element node.
    internal class TextTreeTextElementNode : TextTreeNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new instance.
        internal TextTreeTextElementNode()
        {
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
            return ("TextElementNode Id=" + this.DebugId + " SymbolCount=" + _symbolCount);
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
            TextTreeTextElementNode clone;

            clone = new TextTreeTextElementNode();
            clone._symbolCount = _symbolCount;
            clone._imeCharCount = _imeCharCount;
            clone._textElement = _textElement;

            return clone;
        }

        // Returns the TextPointerContext of the node.
        // Returns ElementStart if direction == Forward, otherwise ElementEnd
        // if direction == Backward.
        internal override TextPointerContext GetPointerContext(LogicalDirection direction)
        {
            return (direction == LogicalDirection.Forward) ? TextPointerContext.ElementStart : TextPointerContext.ElementEnd;
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

        // Root node of a contained tree, if any.
        internal override SplayTreeNode ContainedNode
        {
            get
            {
                return _containedNode;
            }
            
            set
            {
                _containedNode = (TextTreeNode)value;
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

        // Count of chars of all siblings preceding this node.
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

        // Count of symbols covered by this node and any contained nodes.
        // Includes two symbols for this node's start/end edges.
        internal override int SymbolCount
        {
            get
            {
                return _symbolCount;
            }
            
            set
            {
                _symbolCount = value;
            }
        }

        // Count of chars covered by this node and any contained nodes.
        internal override int IMECharCount
        {
            get
            {
                return _imeCharCount;
            }

            set
            {
                _imeCharCount = value;
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
        // Since nodes don't usually have any references, we demand allocate
        // storage when needed.
        internal override bool AfterStartReferenceCount
        {
            get
            {
                return (_edgeReferenceCounts & ElementEdge.AfterStart) != 0;
            }

            set
            {
                Invariant.Assert(value); // Illegal to clear a set ref count.
                _edgeReferenceCounts |= ElementEdge.AfterStart;
            }
        }

        // Count of TextPositions referencing the node's BeforeEnd edge.
        // Since nodes don't usually have any references, we demand allocate
        // storage when needed.
        internal override bool BeforeEndReferenceCount
        {
            get
            {
                return (_edgeReferenceCounts & ElementEdge.BeforeEnd) != 0;
            }

            set
            {
                Invariant.Assert(value); // Illegal to clear a set ref count.
                _edgeReferenceCounts |= ElementEdge.BeforeEnd;
            }
        }

        // Count of TextPositions referencing the node's AfterEnd edge.
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

        // The TextElement associated with this node.
        internal TextElement TextElement
        {
            get
            {
                return _textElement;
            }
            
            set
            {
                _textElement = value;
            }
        }

        // Plain text character count of this element's start edge.
        // This property depends on the current location of the node!
        internal int IMELeftEdgeCharCount
        {
            get
            {
                return (_textElement == null) ? -1 : _textElement.IMELeftEdgeCharCount;
            }
        }

        // Returns true if this node is the leftmost sibling of its parent.
        internal bool IsFirstSibling
        {
            get
            {
                Splay();
                return (_leftChildNode == null);
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

        // Root node of a contained tree, if any.
        private TextTreeNode _containedNode;

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        private uint _generation;

        // Cached symbol offset.
        private int _symbolOffsetCache;

        // Count of symbols covered by this node and any contained nodes.
        // Includes two symbols for this node's start/end edges.
        private int _symbolCount;

        // Count of chars covered by this node and any contained nodes.
        private int _imeCharCount;

        // The TextElement associated with this node.
        private TextElement _textElement;

        // Reference counts of TextPositions referencing this node.
        // Lazy allocated -- null means no references.
        private ElementEdge _edgeReferenceCounts;

        #endregion Private Fields
    }
}
