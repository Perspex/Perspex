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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Documents.Internal;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using MS.Internal.Documents;

namespace MS.Internal.Text
{

    // Replacement for ContainerVisual
    internal class VisualProxy : Visual
    {
        internal void RemoveChild(IControl visual) => VisualChildren.Remove(visual);

        internal void AddChild(IControl visual)
        {
            VisualChildren.Add(visual);
            // TODO: Unclear whether this is enough or not
            if (!visual.IsMeasureValid && visual.PreviousMeasure.HasValue)
            {
                visual.Measure(visual.PreviousMeasure.Value);
            }
            Bounds = new Rect(Bounds.Position, visual.DesiredSize);
        }

        // TODO: Not sure where the proxy visual would get it's size
        internal Vector Offset
        {
            get => Bounds.Position;
            set
            {
                Bounds = new Rect(value.X, value.Y, Bounds.X, Bounds.Y);
            }
        }
    }

    // ----------------------------------------------------------------------
    // Text line formatter.
    // ----------------------------------------------------------------------
    internal sealed class ComplexLine : Line
    {

        // TODO: Figure out what to do with this
        private double PixelsPerDip = 0;

        // ------------------------------------------------------------------
        //
        //  TextSource Implementation
        //
        // ------------------------------------------------------------------

        #region TextSource Implementation

        // ------------------------------------------------------------------
        // Get a text run at specified text source position.
        // ------------------------------------------------------------------
        public override TextRun GetTextRun(int dcp)
        {
            TextRun run = null;
            StaticTextPointer position = _owner.TextContainer.CreateStaticPointerAtOffset(dcp);

            switch (position.GetPointerContext(LogicalDirection.Forward))
            {
                case TextPointerContext.Text:
                    run = HandleText(position);
                    break;

                case TextPointerContext.ElementStart:
                    run = HandleElementStartEdge(position);
                    break;

                case TextPointerContext.ElementEnd:
                    run = HandleElementEndEdge(position);
                    break;

                case TextPointerContext.EmbeddedElement:
                    run = HandleInlineObject(position, dcp);
                    break;

                case TextPointerContext.None:
                    run = new TextEndOfParagraph();
                    break;
            }
            Debug.Assert(run != null, "TextRun has not been created.");
            Debug.Assert(run.TextSourceLength > 0, "TextRun has to have positive length.");
            if (run.Properties != null)
            {
                // TODO run.Properties.PixelsPerDip = this.PixelsPerDip;
            }

            return run;
        }
        #endregion TextSource Implementation

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
        internal ComplexLine(NewTextBlock owner) : base(owner)
        {
        }

        // ------------------------------------------------------------------
        // Arrange content of formatted line.
        //
        //      vc - Visual collection of the parent.
        //      lineOffset - Offset of the line.
        // ------------------------------------------------------------------
        internal override void Arrange(IAvaloniaList<IVisual> vc, Vector lineOffset)
        {
            // Arrange inline objects
            int runDcp = _dcp;
            var runs = _line.TextRuns;
            Debug.Assert(runs != null, "Cannot retrieve runs collection.");

            // Calculate offset shift due to trailing spaces
            double adjustedXOffset = lineOffset.X + CalculateXOffsetShift();
            foreach (var run in runs)
            {
                if (run is InlineObject)
                {
                    InlineObject inlineObject = run as InlineObject;

                    // Disconnect visual from its old parent, if necessary.
                    var currentParent = inlineObject.Element.GetVisualParent();
                    if (currentParent != null)
                    {
                        var parent = currentParent as VisualProxy;
                        Invariant.Assert(parent != null, "parent should always derives from VisualProxy");
                        parent.RemoveChild(inlineObject.Element);
                    }

                    // Get position of inline object withing the text line.
                    FlowDirection flowDirection;
                    Rect rect = GetBoundsFromPosition(runDcp, inlineObject.TextSourceLength, out flowDirection);
                    Debug.Assert(MathUtilities.GreaterThanOrClose(rect.Width, 0), "Negative inline object's width.");

                    VisualProxy proxyVisual = new VisualProxy();
                    if (inlineObject.Element is Visual)
                    {
                        var parentFlowDirection = _owner.FlowDirection;
                        // Check parent's FlowDirection to determine if mirroring is needed

                        IAvaloniaObject parent = ((Visual)inlineObject.Element).Parent;
                        if(parent != null)
                        {
                            parentFlowDirection = parent.GetValue(Inline.FlowDirectionProperty);
                        }

                        UpdateMirroringTransform(_owner.FlowDirection, parentFlowDirection, proxyVisual, rect.Width);
                    }
                    vc.Add(proxyVisual);

                    if (_owner.UseLayoutRounding)
                    {
                        // If using layout rounding, check whether rounding needs to compensate for high DPI
                        var dpi = (_owner.GetVisualRoot() as ILayoutRoot)?.LayoutScaling ?? 1.0;
                        proxyVisual.Offset = new Vector(LayoutHelper.RoundLayoutValue(lineOffset.X + rect.Left, dpi),
                                                        LayoutHelper.RoundLayoutValue(lineOffset.Y + rect.Top, dpi));
                    }
                    else
                    {
                        proxyVisual.Offset = new Vector(lineOffset.X + rect.Left, lineOffset.Y + rect.Top);
                    }
                    proxyVisual.AddChild(inlineObject.Element);

                    // Combine text line offset (relative to the Text control) with inline object
                    // offset (relative to the line) and set transorm on the visual. Trailing spaces
                    // shift is not added here because it is returned by GetBoundsFromPosition
                    inlineObject.Element.Arrange(new Rect(inlineObject.Element.DesiredSize));
                }

                // Do not use TextRun.Length, because it gives total length of the run.
                // So, if the run is broken between lines, it gives incorrect value.
                // Use length of the TextSpan instead, which gives the correct length here.
                runDcp += run.Text.Length;
            }
        }

        // ------------------------------------------------------------------
        // Update mirroring transform.  (Previously in PtsHelper)
        // ------------------------------------------------------------------
        private static void UpdateMirroringTransform(FlowDirection parentFD, FlowDirection childFD, Visual visualChild, double width)
        {
            // Set mirroring transform if necessary, or clear it just in case it was set in the previous
            // format process.
            if (parentFD != childFD)
            {
                var transform = new MatrixTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, width, 0.0));
                visualChild.RenderTransform = transform;
                visualChild.SetValue(Inline.FlowDirectionProperty, childFD);
            }
            else
            {
                visualChild.RenderTransform = null;
                visualChild.ClearValue(Inline.FlowDirectionProperty);
            }
        }

        // ------------------------------------------------------------------
        // Find out if there are any inline objects.
        // ------------------------------------------------------------------
        internal override bool HasInlineObjects()
        { 
            bool hasInlineObjects = false;

            var runs = _line.TextRuns;
            Debug.Assert(runs != null, "Cannot retrieve runs collection.");
            foreach (var run in runs)
            {
                if (run is InlineObject)
                {
                    hasInlineObjects = true;
                    break;
                }
            }
            return hasInlineObjects;
        }

        // ------------------------------------------------------------------
        //  Hit tests to the correct ContentElement within the line.
        //
        //      offset - offset within the line.
        //
        // Returns: ContentElement which has been hit.
        // ------------------------------------------------------------------
        internal override IInputElement InputHitTest(double offset)
        {
            TextContainer tree;
            IAvaloniaObject element;
            CharacterHit charHit;
            TextPointer position;
            TextPointerContext type = TextPointerContext.None;

            element = null;

            // We can only support hittesting text elements in a TextContainer.
            // If the TextContainer is not a TextContainer, return null which higher up the stack
            // will be converted into a reference to the control itself.
            tree = _owner.TextContainer as TextContainer;

            // Adjusted offset for shift due to trailing spaces rendering
            double delta = CalculateXOffsetShift();
            if (tree != null)
            {
                if (_line.HasOverflowed && _owner.ParagraphProperties.TextTrimming != TextTrimming.None)
                {
                    // We should not shift offset in this case
                    Invariant.Assert(MathUtilities.AreClose(delta, 0));
                    var line = _line.Collapse(GetCollapsingProps(_wrappingWidth, _owner.ParagraphProperties));
                    Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");

                    // Get TextPointer from specified distance.
                    charHit = line.GetCharacterHitFromDistance(offset);
                }
                else
                {
                    charHit = _line.GetCharacterHitFromDistance(offset - delta);
                }

                position = new TextPointer(_owner.ContentStart, CalcPositionOffset(charHit), LogicalDirection.Forward);

                if (position != null)
                {
                    if (charHit.TrailingLength == 0)
                    {
                        // Start of character. Look forward
                        type = position.GetPointerContext(LogicalDirection.Forward);
                    }
                    else
                    {
                        // End of character. Look backward
                        type = position.GetPointerContext(LogicalDirection.Backward);
                    }
             
                    // Get element only for Text & Start/End element, for all other positions
                    // return null (it means that the line owner has been hit).
                    if (type == TextPointerContext.Text || type == TextPointerContext.ElementEnd)
                    {
                        element = position.Parent as TextElement;
                    }
                    else if (type == TextPointerContext.ElementStart)
                    {
                        element = position.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                    }
                }
            }

            return element as IInputElement;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // ------------------------------------------------------------------
        // Fetch the next run at text position.
        //
        //      position - current position in the text array
        // ------------------------------------------------------------------
        private TextRun HandleText(StaticTextPointer position)
        {
            IAvaloniaObject element;
            StaticTextPointer endOfRunPosition;

            Debug.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text, "TextPointer does not point to characters.");
            if (position.Parent != null)
            {
                element = position.Parent;
            }
            else
            {
                element = _owner;
            }

            // Extract the aggregated properties into something that the textrun can use.
            //      For properties that can be applied to Highlight services,
            //      need to use 'textHighlights'.
            //      Right now only background is properly retrieved.
            TextRunProperties textProps = new TextProperties(element, position, false /* inline objects */, true /* get background */, PixelsPerDip);

            // Calculate the end of the run by finding either:
            //      a) the next intersection of highlight ranges, or
            //      b) the natural end of this textrun
            endOfRunPosition = _owner.Highlights.GetNextPropertyChangePosition(position, LogicalDirection.Forward);

            // Clamp the text run at an arbitrary limit, so we don't make
            // an unbounded allocation.
            if (position.GetOffsetToPosition(endOfRunPosition) > 4096)
            {
                endOfRunPosition = position.CreatePointer(4096);
            }

            // Get character buffer for the text run.
            char[] textBuffer = new char[position.GetOffsetToPosition(endOfRunPosition)];

            // Copy characters from text run into buffer. Note the actual number of characters copied,
            // which may be different than the buffer's length. Buffer length only specifies the maximum
            // number of characters
            int charactersCopied = position.GetTextInRun(LogicalDirection.Forward, textBuffer, 0, textBuffer.Length);

            // Create text run, using characters copied as length
            return new TextCharacters(textBuffer, 0, charactersCopied, textProps);
        }

        // ------------------------------------------------------------------
        // Fetch the next run at element open edge position.
        //
        //      position - current position in the text array
        // ------------------------------------------------------------------
        private TextRun HandleElementStartEdge(StaticTextPointer position)
        {
            Debug.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart, "TextPointer does not point to element start edge.");

            //      Need to Handle visibility collapsed.
            TextRun run = null;
            TextElement element = (TextElement)position.GetAdjacentElement(LogicalDirection.Forward);
            Debug.Assert(element != null, "Cannot use ITextContainer that does not provide TextElement instances.");

            if (element is LineBreak)
            {
                run = new TextEndOfLine(_elementEdgeCharacterLength * 2);
            }
            else if (element.IsEmpty)
            {
                // Empty TextElement should affect line metrics.
                // TextFormatter does not support this feature right now, so as workaround
                // TextRun with ZERO WIDTH SPACE is used.
                TextRunProperties textProps = new TextProperties(element, position, false /* inline objects */, true /* get background */, PixelsPerDip);
                char[] textBuffer = new char[_elementEdgeCharacterLength * 2];
                textBuffer[0] = (char)0x200B;
                textBuffer[1] = (char)0x200B;
                run = new TextCharacters(textBuffer, 0, textBuffer.Length, textProps);
            }
            else
            {
                Inline inline = element as Inline;
                if (inline == null)
                {
                    run = new TextHidden(_elementEdgeCharacterLength);
                }
                else
                {
                    IAvaloniaObject parent = inline.Parent;
                    FlowDirection inlineFlowDirection = inline.FlowDirection;
                    FlowDirection parentFlowDirection = inlineFlowDirection;

                    if(parent != null)
                    {
                        parentFlowDirection = (FlowDirection)parent.GetValue(Inline.FlowDirectionProperty);
                    }

                    TextDecorationCollection inlineTextDecorations = DynamicPropertyReader.GetTextDecorations(inline);
                    
                    if (inlineFlowDirection != parentFlowDirection)
                    {
                        // Inline's flow direction is different from its parent. Need to create new TextSpanModifier with flow direction
                        if (inlineTextDecorations == null || inlineTextDecorations.Count == 0)
                        {
                            run = new TextSpanModifier(
                                _elementEdgeCharacterLength,
                                null,
                                null,
                                inlineFlowDirection
                                );
                        }
                        else
                        {
                            run = new TextSpanModifier(
                                _elementEdgeCharacterLength,
                                inlineTextDecorations,
                                inline.Foreground,
                                inlineFlowDirection
                                );
                        }
                    }
                    else
                    {
                        if (inlineTextDecorations == null || inlineTextDecorations.Count == 0)
                        {
                            run = new TextHidden(_elementEdgeCharacterLength);
                        }
                        else
                        {
                            run = new TextSpanModifier(
                                _elementEdgeCharacterLength,
                                inlineTextDecorations,
                                inline.Foreground
                                );
                        }
                    }
                }
            }
            return run;
        }

        // ------------------------------------------------------------------
        // Fetch the next run at element close edge position.
        //
        //      position - current position in the text array
        // ------------------------------------------------------------------
        private TextRun HandleElementEndEdge(StaticTextPointer position)
        {
            Debug.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd, "TextPointer does not point to element end edge.");

            TextRun run = null;

            TextElement element = (TextElement)position.GetAdjacentElement(LogicalDirection.Forward);
            Debug.Assert(element != null, "Element should be here.");
            Inline inline = element as Inline;
            if (inline == null)
            {
                run = new TextHidden(_elementEdgeCharacterLength);
            }
            else
            {
                IAvaloniaObject parent = inline.Parent;
                FlowDirection parentFlowDirection = inline.FlowDirection;

                if(parent != null)
                {
                    parentFlowDirection = (FlowDirection)parent.GetValue(Inline.FlowDirectionProperty);
                }

                if (inline.FlowDirection != parentFlowDirection)
                {
                    run = new TextEndOfSegment(_elementEdgeCharacterLength);
                }
                else
                {
                    TextDecorationCollection textDecorations = DynamicPropertyReader.GetTextDecorations(inline);                
                    if (textDecorations == null || textDecorations.Count == 0)
                    {
                        // (2) End of inline element, hide CloseEdge character and continue
                        run = new TextHidden(_elementEdgeCharacterLength);
                    }
                    else
                    {
                        run = new TextEndOfSegment(_elementEdgeCharacterLength);
                    }
                }
            }
            return run;
        }

        // ------------------------------------------------------------------
        // Fetch the next run at UIElment position.
        //
        //      position - current position in the text array
        //      dcp - current position in the text array
        // ------------------------------------------------------------------
        private TextRun HandleInlineObject(StaticTextPointer position, int dcp)
        {
            Debug.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement, "TextPointer does not point to embedded object.");

            TextRun run = null;
            if (position.GetAdjacentElement(LogicalDirection.Forward) is IControl control)
            {
                //  Need to Handle visibility collapsed.

                TextRunProperties textProps = new TextProperties(control, position, true /* inline objects */, true /* get background */, PixelsPerDip);

                // Create object run.
                run = new InlineObject(dcp, TextContainerHelper.EmbeddedObjectLength, control, textProps, _owner);
            }
            else
            {
                // If the embedded object is of an unknown type (not UIElement),
                // treat it as element edge.
                run = HandleElementEndEdge(position);
            }
            return run;
        }

        /// <summary>
        /// Calculates the offset for the corresponding TextPointer from a CharacterHit
        /// </summary>
        /// <remarks>
        /// This is necessary to ensure that we don't try to create a position at an offset greater than TextContainer's symbol count.
        /// This may happen when a line is collapsed with ellipsis and we hit-test at the trailing edge of ellipsis, the trailing length
        /// returned for the CharacterHit is the length of all collapsed characters, including the synthetic EOP. If we try to
        /// create a position at this trailing length we can exceed TextContainer's symbol count.
        /// </remarks>
        private int CalcPositionOffset(CharacterHit charHit)
        {
            int offset = charHit.FirstCharacterIndex + charHit.TrailingLength;
            if (this.EndOfParagraph)
            {
                offset = Math.Min(_dcp + this.Length, offset);
            }
            return offset;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // ------------------------------------------------------------------
        // Element edge character length.
        // ------------------------------------------------------------------
        private static int _elementEdgeCharacterLength = 1;

        #endregion Private Fields
    }
}

