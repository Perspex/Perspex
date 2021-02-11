// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Access to calculated information of a line of text. 
//

using System;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Provides access to calculated information of a line of text.
    /// </summary>
    internal abstract class LineResult
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// See description in TextView.
        /// </summary>
        internal abstract ITextPointer GetTextPositionFromDistance(double distance);

        /// <summary>
        /// See description in TextView.
        /// </summary>
        internal abstract bool IsAtCaretUnitBoundary(ITextPointer position);

        /// <summary>
        /// See description in TextView.
        /// </summary>
        internal abstract ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction);

        /// <summary>
        /// See description in TextView.
        /// </summary>
        internal abstract ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position);

        /// <summary>
        /// See description in TextView.
        /// </summary>
        internal abstract ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end);

        /// <summary>
        /// Retrieves the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        /// <returns>
        /// The position after last content character of the line, 
        /// not including any line breaks.
        /// </returns>
        internal abstract ITextPointer GetContentEndPosition();

        /// <summary>
        /// Retrieves the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        /// <returns>
        /// The position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </returns>
        internal abstract ITextPointer GetEllipsesPosition();

        /// <summary>
        /// Retrieves the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        /// <returns>
        /// The position after last content character of the line, 
        /// not including any line breaks.
        /// </returns>
        internal abstract int GetContentEndPositionCP();

        /// <summary>
        /// Retrieves the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        /// <returns>
        /// The position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </returns>
        internal abstract int GetEllipsesPositionCP();

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// ITextPointer representing the beginning of the Line's contents.
        /// </summary>
        internal abstract ITextPointer StartPosition { get; }

        /// <summary>
        /// ITextPointer representing the end of the Line's contents.
        /// </summary>
        internal abstract ITextPointer EndPosition { get; }

        /// <summary>
        /// Character position representing the beginning of the Line's contents.
        /// </summary>
        internal abstract int StartPositionCP { get; }

        /// <summary>
        /// Character position representing the end of the Line's contents.
        /// </summary>
        internal abstract int EndPositionCP { get; }

        /// <summary>
        /// The bounding rectangle of the line; this is relative to the parent bounding box.
        /// </summary>
        internal abstract Rect LayoutBox { get; }

        /// <summary>
        /// The dominant baseline of the line. 
        /// Distance from the top of the line to the baseline.
        /// </summary>
        internal abstract double Baseline { get; }

        #endregion Internal Properties
    }
}
