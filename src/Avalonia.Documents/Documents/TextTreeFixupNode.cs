// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A special TextContainer node used to parent deleted nodes.
//

using System;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // TextTreeFixupNodes never actually appear in live trees.  However,
    // whenever nodes are removed from the tree, we parent them to a fixup
    // node whose job it is to serve as a guide for any orphaned TextPositions
    // that might later need to find their way back to the original tree.
    internal class TextTreeFixupNode : TextTreeNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new TextTreeFixupNode instance.
        // previousNode/Edge should point to the node TextPositions will
        // move to after synchronizing against the deleted content.
        internal TextTreeFixupNode(TextTreeNode previousNode, ElementEdge previousEdge, TextTreeNode nextNode, ElementEdge nextEdge) :
                    this(previousNode, previousEdge, nextNode, nextEdge, null, null)
        {
        }

        // Creates a new TextTreeFixupNode instance.
        // This ctor should only be called when extracting a single TextTreeTextElementNode.
        // previousNode/Edge should point to the node TextPositions will
        // move to after synchronizing against the deleted content.
        // first/lastContainedNode point to the first and last contained nodes
        // of an extracted element node.  Positions may move into these nodes.
        internal TextTreeFixupNode(TextTreeNode previousNode, ElementEdge previousEdge, TextTreeNode nextNode, ElementEdge nextEdge,
                                   TextTreeNode firstContainedNode, TextTreeNode lastContainedNode)
        {
            _previousNode = previousNode;
            _previousEdge = previousEdge;
            _nextNode = nextNode;
            _nextEdge = nextEdge;
            _firstContainedNode = firstContainedNode;
            _lastContainedNode = lastContainedNode;
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
            return ("FixupNode Id=" + this.DebugId);
        }
#endif // DEBUG

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns a shallow copy of this node.
        internal override TextTreeNode Clone()
        {
            Invariant.Assert(false, "Unexpected call to TextTreeFixupNode.Clone!");
            return null;
        }

        // Returns the TextPointerContext of the node.
        // Because fixup nodes are never in live trees, we should never get here.
        internal override TextPointerContext GetPointerContext(LogicalDirection direction)
        {
            Invariant.Assert(false, "Unexpected call to TextTreeFixupNode.GetPointerContext!");
            return TextPointerContext.None;
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Fixup nodes never have parents.
        internal override SplayTreeNode ParentNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes never contain nodes.
        internal override SplayTreeNode ContainedNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have symbol counts.
        internal override int LeftSymbolCount
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have char counts.
        internal override int LeftCharCount
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have siblings.
        internal override SplayTreeNode LeftChildNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have siblings.
        internal override SplayTreeNode RightChildNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have symbol counts.
        internal override uint Generation
        {
            get
            {
                return 0;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have symbol counts.
        internal override int SymbolOffsetCache
        {
            get
            {
                return -1;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have symbol counts.
        internal override int SymbolCount
        {
            get
            {
                return 0;
            }
            
            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes don't have char counts.
        internal override int IMECharCount
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(false, "FixupNode");
            }
        }

        // Fixup nodes are never referenced by TextPositions.
        internal override bool BeforeStartReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "TextTreeFixupNode should never have a position reference!");
            }
        }

        // Fixup nodes are never referenced by TextPositions.
        internal override bool AfterStartReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "TextTreeFixupNode should never have a position reference!");
            }
        }

        // Fixup nodes are never referenced by TextPositions.
        internal override bool BeforeEndReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "TextTreeFixupNode should never have a position reference!");
            }
        }

        // Fixup nodes are never referenced by TextPositions.
        internal override bool AfterEndReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "TextTreeFixupNode should never have a position reference!");
            }
        }

        // The node TextPositions with Backward gravity should move to
        // when leaving the deleted content.
        internal TextTreeNode PreviousNode
        {
            get
            {
                return _previousNode;
            }
        }

        // The edge TextPositions with Backward gravity should move to
        // when leaving the deleted content.
        internal ElementEdge PreviousEdge
        {
            get
            {
                return _previousEdge;
            }
        }

        // The node TextPositions with Forward gravity should move to
        // when leaving the deleted content.
        internal TextTreeNode NextNode
        {
            get
            {
                return _nextNode;
            }
        }

        // The edge TextPositions with Forward gravity should move to
        // when leaving the deleted content.
        internal ElementEdge NextEdge
        {
            get
            {
                return _nextEdge;
            }
        }

        // If this fixup is for a single TextElementNode extraction, this
        // field is the first contained node of the extracted element node.
        internal TextTreeNode FirstContainedNode
        {
            get
            {
                return _firstContainedNode;
            }
        }

        // If this fixup is for a single TextElementNode extraction, this
        // field is the last contained node of the extracted element node.
        internal TextTreeNode LastContainedNode
        {
            get
            {
                return _lastContainedNode;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The node immediately preceding the deleted content parented by this fixup node.
        private readonly TextTreeNode _previousNode;

        // The edge immediately preceding the deleted content parented by this fixup node.
        private readonly ElementEdge _previousEdge;

        // The node immediately following the deleted content parented by this fixup node.
        private readonly TextTreeNode _nextNode;

        // The edge immediately following the deleted content parented by this fixup node.
        private readonly ElementEdge _nextEdge;

        // If this fixup is for a single TextElementNode extraction, this
        // field is the first contained node of the extracted element node.
        private readonly TextTreeNode _firstContainedNode;

        // If this fixup is for a single TextElementNode extraction, this
        // field is the last contained node of the extracted element node.
        private readonly TextTreeNode _lastContainedNode;

        #endregion Private Fields
    }
}
