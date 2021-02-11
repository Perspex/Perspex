// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text line formatter.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Documents.Internal;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using FlowDirection = Avalonia.Media.FlowDirection;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Text line formatter.
    // ----------------------------------------------------------------------
    internal abstract class Line : ITextSource, IDisposable
    {
        // ------------------------------------------------------------------
        //
        //  IDisposable Implementation
        //
        // ------------------------------------------------------------------

        #region IDisposable Implementation

        // ------------------------------------------------------------------
        // Free all resources associated with the line. Prepare it for reuse.
        // ------------------------------------------------------------------
        public void Dispose()
        {
            // Dispose text line
            _line = null; // TODO No longer needed
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Implementation

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      owner - owner of the line.
        // ------------------------------------------------------------------
        internal Line(NewTextBlock owner)
        {
            _owner = owner;
            _textAlignment = owner.TextAlignment;
            _showParagraphEllipsis = false;
            _wrappingWidth = _owner.RenderSize.Width;
        }

        // ------------------------------------------------------------------
        // Create and format text line.
        //
        //      lineStartIndex - index of the first character in the line
        //      width - wrapping width of the line
        //      lineProperties - properties of the line
        //      textRunCache - run cache used by text formatter
        //      showParagraphEllipsis - true if paragraph ellipsis is shown 
        //                              at the end of the line
        // ------------------------------------------------------------------
        internal void Format(int dcp, double width, TextParagraphProperties lineProperties, TextLineBreak textLineBreak,
            bool showParagraphEllipsis)
        {
#if TEXTPANELLAYOUTDEBUG
            TextPanelDebug.IncrementCounter("Line.Format", TextPanelDebug.Category.TextView);
#endif
            _mirror = false; // TODO (lineProperties.FlowDirection == FlowDirection.RightToLeft);
            _dcp = dcp;
            _showParagraphEllipsis = showParagraphEllipsis;
            _wrappingWidth = width;
            _line = _owner.TextFormatter.FormatLine(this, dcp, width, lineProperties, textLineBreak);
        }

        // ------------------------------------------------------------------
        // Arrange content of formatted line.
        //
        //      vc - Visual collection of the parent.
        //      lineOffset - Offset of the line.
        // ------------------------------------------------------------------
        internal virtual void Arrange(IAvaloniaList<IVisual> vc, Vector lineOffset)
        {
        }

        // ------------------------------------------------------------------
        // Render formatted line.
        //
        //      ctx - Drawing context to be used for rendering.
        //      lineOffset - Offset of the line.
        //      wrappingWidth - Wrapping width for the line.
        // ------------------------------------------------------------------
        internal void Render(DrawingContext ctx, Point lineOffset)
        {
            Debug.Assert(_line != null, "Rendering line that has not been measured yet.");

            // Handle text trimming.
            var line = _line;
            if (_line.HasOverflowed && _owner.ParagraphProperties.TextTrimming != TextTrimming.None)
            {
                line = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
                Debug.Assert(line.HasCollapsed, "Line has not been collapsed");
            }

            double delta = CalculateXOffsetShift();
            line.Draw(ctx,
                new Point(lineOffset.X + delta,
                    lineOffset.Y) /*,  TODO (_mirror ? InvertAxes.Horizontal : InvertAxes.None)*/);
        }

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text position.
        //
        //      characterIndex - position of an object/character
        //      flowDirection - flow direction of object/character
        //
        // Returns: Bounds of an object/character.
        // ------------------------------------------------------------------
        internal Rect GetBoundsFromTextPosition(int characterIndex, out FlowDirection flowDirection)
        {
            return GetBoundsFromPosition(characterIndex, 1, out flowDirection);
        }

        /// <summary>
        /// Returns an ArrayList of rectangles (Rect) that form the bounds of the region specified between
        /// the start and end points
        /// </summary>
        /// <param name="cp"></param>
        /// int offset indicating the starting point of the region for which bounds are required
        /// <param name="cch">
        /// Length in characters of the region for which bounds are required
        /// </param>
        /// <param name="xOffset">
        /// Offset of line in x direction, to be added to line bounds to get actual rectangle for line
        /// </param>
        /// <param name="yOffset">
        /// Offset of line in y direction, to be added to line bounds to get actual rectangle for line
        /// </param>
        /// <remarks>
        /// This function calls GetTextBounds for the line, and then checks if there are text run bounds. If they exist,
        /// it uses those as the bounding rectangles. If not, it returns the rectangle for the first (and only) element
        /// of the text bounds.
        /// </remarks>
        internal List<Rect> GetRangeBounds(int cp, int cch, double xOffset, double yOffset)
        {
            List<Rect> rectangles = new List<Rect>();

            // Adjust x offset for trailing spaces
            double delta = CalculateXOffsetShift();
            double adjustedXOffset = xOffset + delta;

            IList<TextBounds> textBounds;
            if (_line.HasOverflowed && _owner.ParagraphProperties.TextTrimming != TextTrimming.None)
            {
                // We should not shift offset in this case
                Invariant.Assert(MathUtilities.AreClose(delta, 0));
                var line = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                textBounds = line.GetTextBounds(cp, cch);
            }
            else
            {
                textBounds = _line.GetTextBounds(cp, cch);
            }

            Invariant.Assert(textBounds.Count > 0);


            for (int boundIndex = 0; boundIndex < textBounds.Count; boundIndex++)
            {
                Rect rect = textBounds[boundIndex].Rectangle.Translate(new Vector(adjustedXOffset, yOffset));
                rectangles.Add(rect);
            }

            return rectangles;
        }

        //-------------------------------------------------------------------
        // Retrieve text position index from the distance.
        //
        //      distance - distance relative to the beginning of the line
        //
        // Returns: Text position index.
        //-------------------------------------------------------------------
        internal CharacterHit GetTextPositionFromDistance(double distance)
        {
            // Adjust distance to account for a line shift due to rendering of trailing spaces
            double delta = CalculateXOffsetShift();
            if (_line.HasOverflowed && _owner.ParagraphProperties.TextTrimming != TextTrimming.None)
            {
                var line = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
                Invariant.Assert(MathUtilities.AreClose(delta, 0));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                return line.GetCharacterHitFromDistance(distance);
            }

            return _line.GetCharacterHitFromDistance(distance - delta);
        }

        //-------------------------------------------------------------------
        // Retrieve text position for next caret position
        //
        // index: CharacterHit for current position
        //
        // Returns: Text position index.
        //-------------------------------------------------------------------
        internal CharacterHit GetNextCaretCharacterHit(CharacterHit index)
        {
            return _line.GetNextCaretCharacterHit(index);
        }

        //-------------------------------------------------------------------
        // Retrieve text position for previous caret position
        //
        // index: CharacterHit for current position
        //
        // Returns: Text position index.
        //-------------------------------------------------------------------
        internal CharacterHit GetPreviousCaretCharacterHit(CharacterHit index)
        {
            return _line.GetPreviousCaretCharacterHit(index);
        }

        //-------------------------------------------------------------------
        // Retrieve text position for backspace caret position
        //
        // index: CharacterHit for current position
        //
        // Returns: Text position index.
        //-------------------------------------------------------------------
        internal CharacterHit GetBackspaceCaretCharacterHit(CharacterHit index)
        {
            return _line.GetBackspaceCaretCharacterHit(index);
        }

        /// <summary>
        /// Returns true of char hit is at caret unit boundary.
        /// </summary>
        /// <param name="charHit">
        /// CharacterHit to be tested.
        /// </param>
        internal bool IsAtCaretCharacterHit(CharacterHit charHit)
        {
            // TODO return _line.IsAtCaretCharacterHit(charHit, _dcp);
            throw new NotImplementedException();
        }

        // ------------------------------------------------------------------
        // Find out if there are any inline objects.
        // ------------------------------------------------------------------
        internal virtual bool HasInlineObjects()
        {
            return false;
        }

        // ------------------------------------------------------------------
        //  Hit tests to the correct ContentElement within the line.
        //
        //      offset - offset within the line.
        //
        // Returns: ContentElement which has been hit.
        // ------------------------------------------------------------------
        internal virtual IInputElement InputHitTest(double offset)
        {
            return null;
        }

        /// <summary>
        /// Passes linebreak object back up from contained line
        /// </summary>
        internal TextLineBreak GetTextLineBreak()
        {
            if (_line == null)
            {
                return null;
            }

            return _line.TextLineBreak;
        }

        // ------------------------------------------------------------------
        // Get length of content hidden by ellipses.
        //
        //      wrappingWidth - Wrapping width for the line.
        //
        // Returns: Length of collapsed content (number of characters hidden
        //          by ellipses).
        // ------------------------------------------------------------------
        internal int GetEllipsesLength()
        {
            // There are no ellipses, if:
            // * there is no overflow in the line
            // * text trimming is turned off
            if (!_line.HasOverflowed) { return 0; }

            if (_owner.ParagraphProperties.TextTrimming == TextTrimming.None) { return 0; }

            // Create collapsed text line to get length of collapsed content.
            // TODO var collapsedLine = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
            // TODO Debug.Assert(collapsedLine.HasCollapsed, "Line has not been collapsed");
            // TODO IList<TextCollapsedRange> collapsedRanges = collapsedLine.GetTextCollapsedRanges();
            // TODO if (collapsedRanges != null)
            // TODO {
            // TODO     Debug.Assert(collapsedRanges.Count == 1, "Multiple collapsed ranges are not supported.");
            // TODO     TextCollapsedRange collapsedRange = collapsedRanges[0];
            // TODO     return collapsedRange.Length;
            // TODO }
            return 0;
        }


        // ------------------------------------------------------------------
        // Gets width of content, collapsed at wrappingWidth (if necessary)
        //
        //      wrappingWidth - Wrapping width for the line.
        //
        // Returns: Width of content, after collapse (may be greater than wrappingWidth)
        //
        // ------------------------------------------------------------------
        internal double GetCollapsedWidth()
        {
            // There are no ellipses, if:
            // * there is no overflow in the line
            // * text trimming is turned off
            if (!_line.HasOverflowed)
            {
                return Width;
            }

            if (_owner.ParagraphProperties.TextTrimming == TextTrimming.None)
            {
                return Width;
            }

            // Create collapsed text line to get length of collapsed content.
            var collapsedLine = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
            Debug.Assert(collapsedLine.HasCollapsed, "Line has not been collapsed");

            return collapsedLine.Width;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        // ------------------------------------------------------------------
        // Calculated width of the line.
        // ------------------------------------------------------------------
        internal double Width
        {
            get
            {
                if (IsWidthAdjusted)
                {
                    // Trailing spaces add to width
                    return _line.WidthIncludingTrailingWhitespace;
                }
                else
                {
                    return _line.Width;
                }
            }
        }

        // ------------------------------------------------------------------
        // Distance from the beginning of paragraph edge to the line edge.
        // ------------------------------------------------------------------
        internal double Start
        {
            get
            {
                if (IsXOffsetAdjusted)
                {
                    return _line.Start + CalculateXOffsetShift();
                }
                else
                {
                    return _line.Start;
                }
            }
        }

        // ------------------------------------------------------------------
        // Height of the line; line advance distance.
        // ------------------------------------------------------------------
        internal double Height { get { return _line.Height; } }

        // ------------------------------------------------------------------
        // Distance from top to baseline of this text line.
        // ------------------------------------------------------------------
        internal double BaselineOffset { get { return _line.Baseline; } }

        // ------------------------------------------------------------------
        // Is this the last line of the paragraph?
        // ------------------------------------------------------------------
        internal bool EndOfParagraph
        {
            get
            {
                // If there are no Newline characters, it is not the end of paragraph.
                // TODO if (_line.NewLineLength == 0) { return false; }
                if (_line.TextRuns.Count == 0)
                {
                    return false;
                }

                // Since there are Newline characters in the line, do more expensive and
                // accurate check.
                var runs = _line.TextRuns;
                return runs[runs.Count - 1] is TextEndOfParagraph;
            }
        }

        // ------------------------------------------------------------------
        // Length of the line excluding any synthetic characters.
        // ------------------------------------------------------------------
        internal int Length { get { return _line.TextRange.Length - (EndOfParagraph ? _syntheticCharacterLength : 0); } }

        // ------------------------------------------------------------------
        // Length of the line excluding any synthetic characters and line breaks.
        // ------------------------------------------------------------------
        internal int ContentLength { get { return _line.TextRange.Length - _line.NewLineLength; } }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text index.
        //
        //      cp - character index of an object/character
        //      cch - number of positions occupied by object/character
        //      flowDirection - flow direction of object/character
        //
        // Returns: Bounds of an object/character.
        // ------------------------------------------------------------------
        protected Rect GetBoundsFromPosition(int cp, int cch, out FlowDirection flowDirection)
        {
            Rect rect;

            // Adjust x offset for trailing spaces
            double delta = CalculateXOffsetShift();
            IList<TextBounds> textBounds;
            if (_line.HasOverflowed && _owner.ParagraphProperties.TextTrimming != TextTrimming.None)
            {
                // We should not shift offset in this case
                Invariant.Assert(MathUtilities.AreClose(delta, 0));
                var line = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                textBounds = line.GetTextBounds(cp, cch);
            }
            else
            {
                textBounds = _line.GetTextBounds(cp, cch);
            }

            Invariant.Assert(textBounds != null && textBounds.Count == 1,
                "Expecting exactly one TextBounds for a single text position.");

            IList<TextRunBounds> runBounds = textBounds[0].TextRunBounds;
            if (runBounds != null)
            {
                Debug.Assert(runBounds.Count == 1, "Expecting exactly one TextRunBounds for a single text position.");
                rect = runBounds[0].Rectangle;
            }
            else
            {
                rect = textBounds[0].Rectangle;
            }

            rect = rect.WithX(rect.X + delta);
            flowDirection = textBounds[0].FlowDirection;
            return rect;
        }

        // ------------------------------------------------------------------
        // Get collapsing properties.
        //
        //      wrappingWidth - wrapping width for collapsed line.
        //      paraProperties - paragraph properties.
        //
        // Returns: Line collapsing properties.
        // ------------------------------------------------------------------
        protected TextCollapsingProperties GetCollapsingProps(double wrappingWidth, LineProperties paraProperties)
        {
            Debug.Assert(paraProperties.TextTrimming != TextTrimming.None, "Text trimming must be enabled.");
            TextCollapsingProperties collapsingProps;
            if (paraProperties.TextTrimming == TextTrimming.CharacterEllipsis)
            {
                collapsingProps =
                    new TextTrailingCharacterEllipsis(wrappingWidth, paraProperties.DefaultTextRunProperties);
            }
            else
            {
                collapsingProps = new TextTrailingWordEllipsis(wrappingWidth, paraProperties.DefaultTextRunProperties);
            }

            return collapsingProps;
        }

        /// <summary>
        /// Returns amount of shift for X-offset to render trailing spaces
        /// </summary>
        protected double CalculateXOffsetShift()
        {
            // Assert that textblock autosize is working correctly and that moving the offset back
            // will not result in the front of the line being taken off rendered area
            if (IsXOffsetAdjusted)
            {
                if (_textAlignment == TextAlignment.Center)
                {
                    // Return trailing spaces length divided by two so line remains centered
                    return (_line.Width - _line.WidthIncludingTrailingWhitespace) / 2;
                }
                else
                {
                    return (_line.Width - _line.WidthIncludingTrailingWhitespace);
                }
            }
            else
            {
                return 0.0;
            }
        }

        public abstract TextRun GetTextRun(int textSourceIndex);

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Properites
        //
        //-------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// True if eliipsis is displayed in the line
        /// </summary>
        protected bool ShowEllipsis
        {
            get
            {
                if (_owner.ParagraphProperties.TextTrimming == TextTrimming.None)
                {
                    return false;
                }

                if (_line.HasOverflowed || _showParagraphEllipsis)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// True if line ends in hard line break
        /// </summary>
        protected bool HasLineBreak
        {
            get
            {
                return (_line.NewLineLength > 0);
            }
        }

        /// <summary>
        /// True if line's X-offset needs adjustment to render trailing spaces
        /// </summary>
        protected bool IsXOffsetAdjusted
        {
            get
            {
                return ((_textAlignment == TextAlignment.Right || _textAlignment == TextAlignment.Center) &&
                        IsWidthAdjusted);
            }
        }

        /// <summary>
        /// True if line's width is adjusted to include trailing spaces. For right and center alignment we need to
        /// adjust line offset as well, but for left alignment we need to only make a width asjustment
        /// </summary>
        protected bool IsWidthAdjusted
        {
            get
            {
                bool adjusted = false;

                // Trailing spaces rendered only around hard breaks
                if (HasLineBreak || EndOfParagraph)
                {
                    // Lines with ellipsis are not shifted because ellipsis would not appear after trailing spaces
                    if (!ShowEllipsis)
                    {
                        adjusted = true;
                    }
                }

                return adjusted;
            }
        }

        #endregion Protected Properties


        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // ------------------------------------------------------------------
        // Owner of the line.
        // ------------------------------------------------------------------
        protected NewTextBlock _owner;

        // ------------------------------------------------------------------
        // Cached text line.
        // ------------------------------------------------------------------
        protected TextLine _line;

        // ------------------------------------------------------------------
        // Index of the first character in the line.
        // ------------------------------------------------------------------
        protected int _dcp;

        // ------------------------------------------------------------------
        // Synthetic character length.
        // ------------------------------------------------------------------
        protected static int _syntheticCharacterLength = 1;

        // ------------------------------------------------------------------
        // Is text mirrored?
        // ------------------------------------------------------------------
        protected bool _mirror;

        /// <summary>
        ///  Alignment direction of line. Set during formatting.
        /// </summary>
        protected TextAlignment _textAlignment;

        /// <summary>
        /// Does the line habe paragraph ellipsis. This is determined during formatting depending upon
        /// the type of line properties passed.
        /// </summary>
        protected bool _showParagraphEllipsis;

        /// <summary>
        /// Wrapping width of line
        /// </summary>
        protected double _wrappingWidth;

        #endregion Private Fields
    }

    // TODO: Temporary hack to keep references to GetTextBounds, but not have to implement it
    static class TextLineExtensions
    {
        /// <summary>
        /// Client to get an array of bounding rectangles of a range of characters within a text line.
        /// </summary>
        /// <param name="firstTextSourceCharacterIndex">index of first character of specified range</param>
        /// <param name="textLength">number of characters of the specified range</param>
        /// <returns>an array of bounding rectangles.</returns>
        public static IList<TextBounds> GetTextBounds(
            this TextLine textLine,
            int firstTextSourceCharacterIndex,
            int textLength
        )
        {
            throw new NotImplementedException();
        }
    }
}
