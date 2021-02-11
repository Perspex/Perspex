// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A pair of TextPositions used to denote a run of TextContainer content.
//

using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Collections;

    /// <summary>
    /// A pair of TextPositions used to denote a run of TextContainer content.
    /// </summary>
    // make this public when we finish the work for
    // grouping TextEditor/TextContainer change events.
    internal struct TextSegment
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="startPosition">
        /// Position preceeding the TextSegment's content.
        /// </param>
        /// <param name="endPosition">
        /// Position following the TextSegment's content.
        /// </param>
        /// <remarks>
        /// If startPosition or endPosition are TextNavigators (derived from
        /// TextPointer), the TextSegment constructor will store new TextPointer
        /// instances internally.  The values returned by the Start and End
        /// properties are always immutable TextPositions.
        /// </remarks>
        internal TextSegment(ITextPointer startPosition, ITextPointer endPosition) :
            this(startPosition, endPosition, false)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="startPosition">
        /// Position preceeding the TextSegment's content.
        /// </param>
        /// <param name="endPosition">
        /// Position following the TextSegment's content.
        /// </param>
        /// <param name="preserveLogicalDirection">
        /// Whether preserves LogicalDirection of start and end positions.
        /// </param>
        internal TextSegment(ITextPointer startPosition, ITextPointer endPosition, bool preserveLogicalDirection)
        {
            ValidationHelper.VerifyPositionPair(startPosition, endPosition);

            if (startPosition.CompareTo(endPosition) == 0)
            {
                // To preserve segment emptiness
                // we use the same instance of a pointer
                // for both segment ends.
                _start = startPosition.GetFrozenPointer(startPosition.LogicalDirection);
                _end = _start;
            }
            else
            {
                Invariant.Assert(startPosition.CompareTo(endPosition) < 0);
                _start = startPosition.GetFrozenPointer(preserveLogicalDirection ? startPosition.LogicalDirection : LogicalDirection.Backward);
                _end = endPosition.GetFrozenPointer(preserveLogicalDirection ? endPosition.LogicalDirection : LogicalDirection.Forward);
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// returns true if the segment contains a given position
        /// </summary>
        // need to accound for position.LogicalDirection.
        internal bool Contains(ITextPointer position)
        {
            return (!this.IsNull && this._start.CompareTo(position) <= 0 && position.CompareTo(this._end) <= 0);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Position preceeding the TextSegment's content.
        /// </summary>
        internal ITextPointer Start
        {
            get
            {
                return _start;
            }
        }

        /// <summary>
        /// Position following the TextSegment's content.
        /// </summary>
        internal ITextPointer End
        {
            get
            {
                return _end;
            }
        }

        internal bool IsNull
        {
            get
            {
                return _start == null || _end == null;
            }
        }

        #endregion Internal Properties

        /// <summary>
        /// The "TextSegment.Null" value.
        /// </summary>
        /// <remarks>
        /// TextSegtemt.Null is used in contexts where text segment is missing.
        /// </remarks>
        internal static readonly TextSegment Null = new TextSegment();

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Position preceeding the TextSegment's content.
        private readonly ITextPointer _start;

        // Position following the TextSegment's content.
        private readonly ITextPointer _end;

        #endregion Private Fields
    }
}
