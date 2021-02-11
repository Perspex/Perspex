// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: TextView implementation for TextBlock. 
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Documents.Internal;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace MS.Internal.Documents
{
    /// <summary>
    /// TextView implementation for TextBlock.
    /// </summary>
    internal class TextParagraphView : TextViewBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">
        /// Root of layout structure visualizing content.
        /// </param>
        /// <param name="textContainer">
        /// TextContainer providing content for this view.
        /// </param>
        internal TextParagraphView(NewTextBlock owner, ITextContainer textContainer)
        {
            _owner = owner;
            _textContainer = textContainer;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// <see cref="ITextView.GetTextPositionFromPoint"/>
        /// </summary>
        internal override ITextPointer GetTextPositionFromPoint(Point point, bool snapToText)
        {
            ITextPointer position;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }

            // Retrieve position from line array.
            position = GetTextPositionFromPoint(Lines, point, snapToText);

            Invariant.Assert(position == null || position.HasValidLayout);
            return position;
        }

        /// <summary>
        /// <see cref="ITextView.GetRectangleFromTextPosition"/>
        /// </summary>
        /// <remarks>
        /// Always returns identity for output transform. 
        /// </remarks>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            // Set transform to identity
            transform = IdentityTransform.Instance;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            return _owner.GetRectangleFromTextPosition(position);
        }

        /// <summary>
        /// <see cref="TextViewBase.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal override Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
#if TEXTPANELLAYOUTDEBUG
            TextPanelDebug.StartTimer("TextView.GetTightBoundingGeometryFromTextPositions", TextPanelDebug.Category.TextView);
#endif
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            if (startPosition == null)
            {
                throw new ArgumentNullException("startPosition");
            }
            if (endPosition == null)
            {
                throw new ArgumentNullException("endPosition");
            }
            ValidationHelper.VerifyPosition(_textContainer, startPosition, "startPosition");
            ValidationHelper.VerifyDirection(startPosition.LogicalDirection, "startPosition.LogicalDirection");
            ValidationHelper.VerifyPosition(_textContainer, endPosition, "endPosition");

            Geometry geometry = _owner.GetTightBoundingGeometryFromTextPositions(startPosition, endPosition);
#if TEXTPANELLAYOUTDEBUG
            TextPanelDebug.StopTimer("TextView.GetTightBoundingGeometryFromTextPositions", TextPanelDebug.Category.TextView);
#endif
            return (geometry);
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            ITextPointer positionOut;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            // TextBlock element does not support columns, hence suggestedX does not change
            // with line movement.
            // Initialy set linesMoved to 0;
            newSuggestedX = suggestedX;
            linesMoved = 0;

            if (count == 0)
            {
                return position;
            }

            ReadOnlyCollection<LineResult> lines = Lines;
            Debug.Assert(lines != null && lines.Count > 0);

            // Get index of the line that contains position.
            int lineIndex = GetLineFromPosition(lines, position);
            if (!(lineIndex >= 0 && lineIndex < lines.Count))
            {
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException("position");
            }

            // Advance line index by count.
            int oldLineIndex = lineIndex;
            lineIndex = Math.Max(0, lineIndex + count);
            lineIndex = Math.Min(lines.Count - 1, lineIndex);
            linesMoved = lineIndex - oldLineIndex;

            // Get position at suggested X. 
            // If line has not been moved, return the same position. 
            // If suggested X is not provided, use the first position in the line.
            if (linesMoved == 0)
            {
                positionOut = position;
            }
            else if (!double.IsNaN(suggestedX))
            {
                positionOut = lines[lineIndex].GetTextPositionFromDistance(suggestedX);
            }
            else
            {
                positionOut = lines[lineIndex].StartPosition.CreatePointer(LogicalDirection.Forward);
            }

            Invariant.Assert(positionOut == null || positionOut.HasValidLayout);
            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.IsAtCaretUnitBoundary"/>
        /// </summary>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            // Verify valid layout, position and direction
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            // No special cases for this, the only special case is handled in TextBlock
            int lineIndex = GetLineFromPosition(Lines, position);
            int dcp = Lines[lineIndex].StartPositionCP;

            return _owner.IsAtCaretUnitBoundary(position, dcp, lineIndex);
        }

        /// <summary>
        /// <see cref="ITextView.GetNextCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            // Verify valid layout, position and direction
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            // Get line index for position, and offset 
            int lineIndex = GetLineFromPosition(Lines, position);
            int dcp = Lines[lineIndex].StartPositionCP;

            ITextPointer positionOut = _owner.GetNextCaretUnitPosition(position, direction, dcp, lineIndex);

            Invariant.Assert(positionOut == null || positionOut.HasValidLayout);

            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.GetBackspaceCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            // Verify valid layout, position and direction
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            // Get line index for position, and offset 
            int lineIndex = GetLineFromPosition(Lines, position);
            int dcp = Lines[lineIndex].StartPositionCP;

            ITextPointer positionOut = _owner.GetBackspaceCaretUnitPosition(position, dcp, lineIndex);

            Invariant.Assert(positionOut == null || positionOut.HasValidLayout);

            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.GetLineRange"/>
        /// </summary>
        internal override TextSegment GetLineRange(ITextPointer position)
        {
            ReadOnlyCollection<LineResult> lines;
            int lineIndex;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");

            lines = Lines;
            Debug.Assert(lines != null && lines.Count > 0);

            // Get index of the line that contains position.
            lineIndex = GetLineFromPosition(lines, position);
            Debug.Assert(lineIndex >= 0 && lineIndex < lines.Count);

            return new TextSegment(lines[lineIndex].StartPosition, lines[lineIndex].GetContentEndPosition(), true);
        }

        /// <summary>
        /// <see cref="ITextView.Contains"/>
        /// </summary>
        internal override bool Contains(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!IsValid)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextViewInvalidLayout)*/);
            }

            // TextParagraphView has a single view that covers all its contents,
            // and there is no background layou mechanism for TextParagraphView,
            // so all positions considered contained in it.
            return true;
        }

        /// <summary>
        /// <see cref="ITextView.Validate()"/>
        /// </summary>
        internal override bool Validate()
        {
            _owner.InvalidateArrange();
            _owner.InvalidateMeasure();
            // TODO: This seemed to be instantaneous previously
            return this.IsValid;
        }

        /// <summary>
        /// HitTest a line array.
        /// </summary>
        /// <param name="lines">Collection of lines.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a positioned text position 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return null position, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the point.
        /// </returns>
        internal static ITextPointer GetTextPositionFromPoint(ReadOnlyCollection<LineResult> lines, Point point, bool snapToText)
        {
            int lineIndex;
            ITextPointer orientedPosition;

            Debug.Assert(lines != null && lines.Count > 0, "Line array is empty.");

            // Figure out which line is the closest to the input pixel position.
            lineIndex = GetLineFromPoint(lines, point, snapToText);
            Debug.Assert(lineIndex < lines.Count);

            // If no line is hit, return null oriented text position.
            // Otherwise hittest line content.
            if (lineIndex < 0)
            {
                orientedPosition = null;
            }
            else
            {
                // Get position from distance.
                orientedPosition = lines[lineIndex].GetTextPositionFromDistance(point.X);
            }

            return orientedPosition;
        }

        /// <summary>
        /// Returns the index of line that contains specified position.
        /// </summary>
        /// <param name="lines">Collections of lines.</param>
        /// <param name="position">Position with orientation.</param>
        /// <returns>
        /// Returns the index of line that contains specified position, 
        /// or -1 if position is not in line array.
        /// </returns>
        internal static int GetLineFromPosition(ReadOnlyCollection<LineResult> lines, ITextPointer position)
        {
            int lineIndex = -1;
            int indexStart = 0;
            int indexEnd = lines.Count - 1;

            // Needs to be calculated this way (and not from start of text tree) 
            // to ensure we're comparing the right dcps (cell boundaries reset cps from results)
            int dcp = lines[0].StartPosition.GetOffsetToPosition(position) + lines[0].StartPositionCP;

            // Check if the position is within line array range. If not, return closest line.
            if (dcp < lines[0].StartPositionCP ||
                dcp > lines[lines.Count - 1].EndPositionCP)
            {
                return dcp < lines[0].StartPositionCP ? 0 : lines.Count - 1;
            }

            // Search for line that contains specified position.
            // Use binary search.
            lineIndex = 0;
            while (indexStart < indexEnd)
            {
                // Get index of the next line for the search process.
                if (indexEnd - indexStart < 2)
                {
                    lineIndex = (lineIndex == indexStart) ? indexEnd : indexStart;
                }
                else
                {
                    lineIndex = indexStart + (indexEnd - indexStart) / 2;
                }
                // Check if the line is found and reduce searching range if necessary.
                if (dcp < lines[lineIndex].StartPositionCP)
                {
                    indexEnd = lineIndex;
                }
                else if (dcp > lines[lineIndex].EndPositionCP)
                {
                    indexStart = lineIndex;
                }
                else
                {
                    if (dcp == lines[lineIndex].EndPositionCP)
                    {
                        if (position.LogicalDirection == LogicalDirection.Forward && (lineIndex != lines.Count - 1))
                        {
                            ++lineIndex;
                        }
                    }
                    else if (dcp == lines[lineIndex].StartPositionCP)
                    {
                        if (position.LogicalDirection == LogicalDirection.Backward && lineIndex != 0)
                        {
                            --lineIndex;
                        }
                    }
                    break;
                }
            }
#if DEBUG
            Debug.Assert(dcp >= lines[lineIndex].StartPositionCP);
            Debug.Assert(dcp < lines[lineIndex].EndPositionCP ||
                         (  dcp == lines[lineIndex].EndPositionCP && 
                            (   position.LogicalDirection == LogicalDirection.Backward || 
                                (position.LogicalDirection == LogicalDirection.Forward && (lineIndex == lines.Count - 1)))));
#endif
            return lineIndex;
        }

        /// <summary>
        /// Raise TextView.Updated event.
        /// </summary>
        internal void OnUpdated()
        {
            OnUpdated(EventArgs.Empty);
        }

        /// <summary>
        /// Invalidate TextView internal state.
        /// </summary>
        internal void Invalidate()
        {
            _lines = null;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// <see cref="ITextView.RenderScope"/>
        /// </summary>
        internal override Visual RenderScope
        {
            get { return _owner; }
        }

        /// <summary>
        /// <see cref="ITextView.TextContainer"/>
        /// </summary>
        internal override ITextContainer TextContainer
        {
            get { return _textContainer; }
        }

        /// <summary>
        /// <see cref="ITextView.IsValid"/>
        /// </summary>
        internal override bool IsValid
        {
            get { return _owner.IsLayoutDataValid; }
        }

        /// <summary>
        /// <see cref="ITextView.TextSegments"/>
        /// </summary>
        internal override ReadOnlyCollection<TextSegment> TextSegments
        {
            get
            {
                List<TextSegment> segments = new List<TextSegment>(1);
                segments.Add(new TextSegment(_textContainer.Start, _textContainer.End, true));
                return new ReadOnlyCollection<TextSegment>(segments);
            }
        }

        /// <summary>
        /// Collection of LineResults for each line in the paragraph.
        /// </summary>
        internal ReadOnlyCollection<LineResult> Lines
        {
            get
            {
                if (_lines == null)
                {
                    _lines = _owner.GetLineResults();
                }
                return _lines;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// HitTest a line array.
        /// </summary>
        /// <param name="lines">Collection of lines.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a line index 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return -1, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// An index of line matching or closest to the point.
        /// </returns>
        internal static int GetLineFromPoint(ReadOnlyCollection<LineResult> lines, Point point, bool snapToText)
        {
            Debug.Assert(lines != null && lines.Count > 0);

            int lineIndex;
            bool foundHit;

            // Figure out which line is the closest vertically to the input pixel position.
            // Assume fixed line height to find a starting point for search.
            // If the first pick is not accurate, do linear search.
            foundHit = GetVerticalLineFromPoint(lines, point, snapToText, out lineIndex);

            // It is possible to have successive lines with the same 
            // vertical offset. It may happen when a line of text is split
            // because of figure/floater.
            // Figure out which line is the closest horizontally to the input pixel position.
            if (foundHit)
            {
                foundHit = GetHorizontalLineFromPoint(lines, point, snapToText, ref lineIndex);
            }

            return foundHit ? lineIndex : -1;
        }

        /// <summary>
        /// HitTest a line array and find the index of line hit in vertical direction.
        /// </summary>
        /// <param name="lines">Collection of lines.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a line index 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return -1, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <param name="lineIndex">Index of line that has been hit.</param>
        /// <returns>True if hit has been found.</returns>
        private static bool GetVerticalLineFromPoint(ReadOnlyCollection<LineResult> lines, Point point, bool snapToText, out int lineIndex)
        {
            Debug.Assert(lines != null && lines.Count > 0);

            bool foundHit = false;
            double approximatedLineHeight;

            // Figure out which line is the closest vertically to the input pixel position.
            // Assume fixed line height to find a starting point for search.
            // If the first pick is not accurate, do linear search.
            approximatedLineHeight = lines[0].LayoutBox.Height;
            lineIndex = Math.Max(Math.Min((int)(point.Y / approximatedLineHeight), lines.Count - 1), 0);
            while (!foundHit)
            {
                Rect lineBox = lines[lineIndex].LayoutBox;

                if (point.Y < lineBox.Y)
                {
                    // Go to the previous line, if this is not the first one.
                    if (lineIndex > 0)
                    {
                        --lineIndex;
                    }
                    else
                    {
                        // This is the first line.
                        foundHit = snapToText;
                        break;
                    }
                }
                else if (point.Y > lineBox.Y + lineBox.Height)
                {
                    // Go to the next line, if this is not the last one.
                    // But if the point belongs to the gap between lines, 
                    // consider the closest line.
                    if (lineIndex < lines.Count - 1)
                    {
                        Rect nextLineBox = lines[lineIndex + 1].LayoutBox;
                        if (point.Y < nextLineBox.Y)
                        {
                            // Point is in the gap between lines. Use the closest line.
                            double gap = nextLineBox.Y - (lineBox.Y + lineBox.Height);
                            if (point.Y > lineBox.Y + lineBox.Height + gap / 2)
                            {
                                ++lineIndex;
                            }
                            foundHit = snapToText;
                            break;
                        }
                        else
                        {
                            ++lineIndex;
                        }
                    }
                    else
                    {
                        // This is the last line.
                        foundHit = snapToText;
                        break;
                    }
                }
                else
                {
                    // The current line has been hit.
                    // But in the case of line overlapping, consider the closest line.
                    Rect siblingLineBox;
                    double siblingOverhang;

                    // Check the previous line overhang.
                    siblingOverhang = 0;
                    if (lineIndex > 0)
                    {
                        siblingLineBox = lines[lineIndex - 1].LayoutBox;
                        siblingOverhang = lineBox.Y - (siblingLineBox.Y + siblingLineBox.Height);
                    }
                    if (siblingOverhang < 0)
                    {
                        // The current line overlaps with the previous line.
                        // Use the closest one.
                        if (point.Y < lineBox.Y - siblingOverhang / 2)
                        {
                            --lineIndex;
                        }
                    }
                    else
                    {
                        // Check the next line overhang.
                        siblingOverhang = 0;
                        if (lineIndex < lines.Count - 1)
                        {
                            siblingLineBox = lines[lineIndex + 1].LayoutBox;
                            siblingOverhang = siblingLineBox.Y - (lineBox.Y + lineBox.Height);
                        }
                        if (siblingOverhang < 0)
                        {
                            // The current line overlaps with the next line.
                            // Use the closest one.
                            if (point.Y > lineBox.Y + lineBox.Height + siblingOverhang / 2)
                            {
                                ++lineIndex;
                            }
                        }
                    }

                    foundHit = true;
                    break;
                }
            }

            return foundHit;
        }

        /// <summary>
        /// HitTest a line array and find the index of line hit in horizontal direction.
        /// Assumes that vertical hittesting has been already done and lineIndex points to 
        /// index of line that has been hit in vertical direction.
        /// </summary>
        /// <param name="lines">Collection of lines.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a line index 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return -1, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <param name="lineIndex">Index of line that has been hit.</param>
        /// <returns>True if hit has been found.</returns>
        private static bool GetHorizontalLineFromPoint(ReadOnlyCollection<LineResult> lines, Point point, bool snapToText, ref int lineIndex)
        {
            Debug.Assert(lines != null && lines.Count > 0);

            bool foundHit = false;
            bool lookForSiblings = true;

            // It is possible to have successive lines with the same 
            // vertical offset. It may happen when a line of text is split
            // because of figure/floater.
            // Figure out which line is the closest horizontally to the input pixel position.
            while (lookForSiblings)
            {
                Rect lineBox = lines[lineIndex].LayoutBox;
                Rect siblingLineBox;
                double siblingGap;

                // Check sibling lines.
                if (point.X < lineBox.X && lineIndex > 0)
                {
                    // Check if the previous line starts at the same vertical position.
                    siblingLineBox = lines[lineIndex - 1].LayoutBox;
                    if (MathUtilities.AreClose(siblingLineBox.Y, lineBox.Y))
                    {
                        if (point.X <= siblingLineBox.X + siblingLineBox.Width)
                        {
                            --lineIndex;
                        }
                        else
                        {
                            siblingGap = Math.Max(lineBox.X - (siblingLineBox.X + siblingLineBox.Width), 0);
                            if (point.X < lineBox.X - siblingGap / 2)
                            {
                                --lineIndex;
                            }
                            foundHit = snapToText;
                            lookForSiblings = false;
                            break;
                        }
                    }
                    else
                    {
                        foundHit = snapToText;
                        lookForSiblings = false;
                        break;
                    }
                }
                else if ((point.X > lineBox.X + lineBox.Width) && (lineIndex < lines.Count - 1))
                {
                    // Check if the next line starts at the same vertical position.
                    siblingLineBox = lines[lineIndex + 1].LayoutBox;
                    if (MathUtilities.AreClose(siblingLineBox.Y, lineBox.Y))
                    {
                        if (point.X >= siblingLineBox.X)
                        {
                            ++lineIndex;
                        }
                        else
                        {
                            siblingGap = Math.Max(siblingLineBox.X - (lineBox.X + lineBox.Width), 0);
                            if (point.X > siblingLineBox.X - siblingGap / 2)
                            {
                                ++lineIndex;
                            }
                            foundHit = snapToText;
                            lookForSiblings = false;
                            break;
                        }
                    }
                    else
                    {
                        foundHit = snapToText;
                        lookForSiblings = false;
                        break;
                    }
                }
                else
                {
                    foundHit = snapToText || (point.X >= lineBox.X && point.X <= lineBox.X + lineBox.Width);
                    lookForSiblings = false;
                    break;
                }
            }

            return foundHit;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Root of layout structure visualizing content.
        /// </summary>
        /// <remarks>
        /// we don't need this!  We can use _textContainer
        /// once ITextContainer has TextView functionality to match the 
        /// public API.
        /// </remarks>
        private readonly NewTextBlock _owner;

        /// <summary>
        /// TextContainer providing content for this view.
        /// </summary>
        private readonly ITextContainer _textContainer;

        /// <summary>
        /// Cached collection of LineResults.
        /// </summary>
        private ReadOnlyCollection<LineResult> _lines;

        #endregion Private Fields
    }
}

