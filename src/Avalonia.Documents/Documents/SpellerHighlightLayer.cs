// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Highlight rendering for the Speller.
//

using System;
using MS.Internal;
using System.Windows.Documents;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    // Highlight rendering for the Speller.
    // The speller tags errors with red squigglies.
    internal class SpellerHighlightLayer : HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Static constructor.
        static SpellerHighlightLayer()
        {
            _errorTextDecorations = GetErrorTextDecorations();
        }

        // Constructor.
        internal SpellerHighlightLayer(Speller speller)
        {
            _speller = speller;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns a TextDecorationCollection used to tag spelling errors,
        // or DependencyProperty.UnsetValue if there's no error at the specified position.
        internal override object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction)
        {
            object value;

            if (IsContentHighlighted(textPosition, direction))
            {
                value = _errorTextDecorations;
            }
            else
            {
                value = AvaloniaProperty.UnsetValue;
            }

            return value;
        }

        // Returns true iff the indicated content has scoping highlights.
        internal override bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction)
        {
            return _speller.StatusTable.IsRunType(textPosition, direction, SpellerStatusTable.RunType.Error);
        }

        // Returns the position of the next highlight start or end in an
        // indicated direction, or null if there is no such position.
        internal override StaticTextPointer GetNextChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            return _speller.StatusTable.GetNextErrorTransition(textPosition, direction);
        }

        // Raises a Changed event for any listeners, covering the
        // specified text.
        internal void FireChangedEvent(ITextPointer start, ITextPointer end)
        {
            if (Changed != null)
            {
                Changed(this, new SpellerHighlightChangedEventArgs(start, end));
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Type identifying this layer for Highlights.GetHighlightValue calls.
        internal override Type OwnerType
        {
            get
            {
                return typeof(SpellerHighlightLayer);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Event raised when a highlight range is inserted, removed, moved, or
        /// has a local property value change.
        /// </summary>
        internal override event HighlightChangedEventHandler Changed;

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Calculates TextDecorations for speller errors.
        private static TextDecorationCollection GetErrorTextDecorations()
        {
            //
            // Build a "squiggle" Brush.
            //
            // This works by hard-coding a 3 pixel high TextDecoration, and
            // then tiling horizontally.  (We can't scale the squiggle in
            // the vertical direction, there's no support to limit tiling
            // to just the horizontal from the MIL.)
            //
            var brush = new VisualBrush(new SquigglyVisual());
            brush.TileMode = TileMode.Tile;
            brush.SourceRect = new RelativeRect(0, 0, 3, 3, RelativeUnit.Absolute);
            brush.DestinationRect = new RelativeRect(0, 0, 3, 3, RelativeUnit.Absolute);
            // TODO brush.ViewportUnits = BrushMappingMode.Absolute;

            TextDecoration textDecoration = new TextDecoration();
            textDecoration.Location = TextDecorationLocation.Underline;
            textDecoration.Stroke = brush;
            textDecoration.StrokeThickness = 3;
            textDecoration.StrokeOffset = 0;
            textDecoration.StrokeOffsetUnit = TextDecorationUnit.FontRecommended;
            textDecoration.StrokeThicknessUnit = TextDecorationUnit.Pixel;

            TextDecorationCollection decorationCollection = new TextDecorationCollection();
            decorationCollection.Add(textDecoration);

            return decorationCollection;
        }

        private class SquigglyVisual : Visual
        {
            public override void Render(DrawingContext context)
            {
                Pen pen = new Pen(Brushes.Red, 0.33);

                // This is our tile:
                //
                //  x   x
                //   x x
                //    x
                //
                context.DrawLine(pen, new Point(0.0, 0.0), new Point(0.5, 1.0));
                context.DrawLine(pen, new Point(0.5, 1.0), new Point(1.0, 0.0));
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Argument for the Changed event, encapsulates a highlight change.
        private class SpellerHighlightChangedEventArgs : HighlightChangedEventArgs
        {
            // Constructor.
            internal SpellerHighlightChangedEventArgs(ITextPointer start, ITextPointer end)
            {
                List<TextSegment> list;

                Invariant.Assert(start.CompareTo(end) < 0, "Bogus start/end combination!");

                list = new List<TextSegment>(1);
                list.Add(new TextSegment(start, end));

                _ranges = new ReadOnlyCollection<TextSegment>(list);
            }

            // Collection of changed content ranges.
            internal override IList Ranges
            {
                get
                {
                    return _ranges;
                }
            }

            // Type identifying the owner of the changed layer.
            internal override Type OwnerType
            {
                get
                {
                    return typeof(SpellerHighlightLayer);
                }
            }

            // Collection of changed content ranges.
            private readonly ReadOnlyCollection<TextSegment> _ranges;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Assocated Speller.
        private readonly Speller _speller;

        // Decorations for text flagged with a spelling error.
        private static readonly TextDecorationCollection _errorTextDecorations;

        #endregion Private Fields
    }

    // TODO: This is a placeholder definition
    internal class Speller
    {
        public SpellerStatusTable StatusTable { get; }
    }

}
