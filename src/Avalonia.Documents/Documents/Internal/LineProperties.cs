// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text line properties provider. 
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Documents.Internal;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using MS.Internal.Documents;

// TextParagraph

namespace System.Windows
{
    /// <summary>
    /// This property describes mechanism by which a line box is determined for each line.
    /// </summary>
    public enum LineStackingStrategy
    {
        /// <summary>
        /// The stack-height is determined by the block element 'line-height' property value.
        /// </summary>
        BlockLineHeight,

        /// <summary>
        /// The stack-height is the smallest value that contains the extended block progression
        /// dimension of all the inline elements on that line when those elements are properly aligned.
        /// The 'line-height' property value is taken into account only for the block elements.
        /// </summary>
        MaxHeight,

        ///// <summary>
        ///// The stack-height is the smallest value that contains the extended block progression
        ///// dimension of all the inline elements on that line when those elements are properly aligned.
        ///// </summary>
        //InlineLineHeight,

        ///// <summary>
        ///// The stack-height is the smallest multiple of the block element 'line-height' computed value
        ///// that can contain the block progression of all the inline elements on that line when those
        ///// elements are properly aligned.
        ///// </summary>
        //GridHeight,
    }
}

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Text line properties provider.
    // ----------------------------------------------------------------------
    internal class LineProperties : TextParagraphProperties
    {
        // ------------------------------------------------------------------
        //
        //  TextParagraphProperties Implementation
        //
        // ------------------------------------------------------------------

        #region TextParagraphProperties Implementation

        /// <summary>
        /// This property specifies whether the primary text advance 
        /// direction shall be left-to-right, right-to-left, or top-to-bottom.
        /// </summary>
        public override FlowDirection FlowDirection { get { return _flowDirection; } }

        /// <summary>
        /// This property describes how inline content of a block is aligned.
        /// </summary>
        public override TextAlignment TextAlignment { get { return IgnoreTextAlignment ? TextAlignment.Left : _textAlignment; } }

        /// <summary>
        /// Paragraph's line height
        /// </summary>
        /// <remarks>
        /// TextFormatter does not do appropriate line height handling, so
        /// report always 0 as line height. 
        /// Line height is handled by TextFormatter host.
        /// </remarks>
        public override double LineHeight 
        { 
            get 
            {
                if (LineStackingStrategy == LineStackingStrategy.BlockLineHeight && !double.IsNaN(_lineHeight))
                {
                    return _lineHeight;
                }
                return 0.0;
            } 
        }

        /// <summary>
        /// Indicates the first line of the paragraph.
        /// </summary>
        public override bool FirstLineInParagraph { get { return false; } }

        /// <summary>
        /// Paragraph's default run properties
        /// </summary>
        public override TextRunProperties DefaultTextRunProperties { get { return _defaultTextProperties; } }

        /// <summary>
        /// Text decorations specified at the paragraph level.
        /// </summary>
        public override TextDecorationCollection TextDecorations { get { return _defaultTextProperties.TextDecorations; } }

        /// <summary>
        /// This property controls whether or not text wraps when it reaches the flow edge 
        /// of its containing block box 
        /// </summary>
        public override TextWrapping TextWrapping { get { return _textWrapping; } }

        /// <summary>
        /// This property specifies marker characteristics of the first line in paragraph
        /// </summary>
        // TODO public override TextMarkerProperties TextMarkerProperties { get { return _markerProperties; } }

        /// <summary>
        /// Line indentation
        /// </summary>
        /// <remarks>
        /// Line indentation. Line indent by default is always 0.
        /// Use FirstLineProperties class to return real value of this property.
        /// </remarks>
        public override double Indent { get { return 0.0; } }

        #endregion TextParagraphProperties Implementation

        /// <summary>
        /// Constructor.
        /// </summary>
        internal LineProperties(
            AvaloniaObject element,
            AvaloniaObject contentHost,
            TextRunProperties defaultTextProperties/*,
             TODO MarkerProperties markerProperties*/)
            : this(element, contentHost, defaultTextProperties, /* TODO markerProperties,*/ element.GetValue(NewTextBlock.TextAlignmentProperty))
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal LineProperties(
            AvaloniaObject element,
            AvaloniaObject contentHost,
            TextRunProperties defaultTextProperties,
            // TODO MarkerProperties markerProperties,
            TextAlignment textAlignment)
        {
            _defaultTextProperties = defaultTextProperties;
            // TODO _markerProperties = (markerProperties != null) ? markerProperties.GetTextMarkerProperties(this) : null;

            _flowDirection = (FlowDirection)element.GetValue(Inline.FlowDirectionProperty);
            _textAlignment = textAlignment;
            _lineHeight = (double)element.GetValue(NewTextBlock.LineHeightProperty);
            // TODO _textIndent = (double)element.GetValue(Paragraph.TextIndentProperty);
            // TODO _lineStackingStrategy = (LineStackingStrategy)element.GetValue(Block.LineStackingStrategyProperty);

            _textWrapping = TextWrapping.Wrap;
            _textTrimming = TextTrimming.None;

            if (contentHost is TextBlock /* TODO || contentHost is ITextBoxViewHost */)
            {
                // NB: we intentially don't try to find the "PropertyOwner" of
                // a FlowDocument here.  TextWrapping has a hard-coded
                // value SetValue'd when a FlowDocument is hosted by a TextBoxBase.
                _textWrapping = (TextWrapping)contentHost.GetValue(TextBlock.TextWrappingProperty);
                _textTrimming = (TextTrimming)contentHost.GetValue(TextBlock.TextTrimmingProperty);
            }
            // TODO else if (contentHost is FlowDocument)
            // TODO {
            // TODO     _textWrapping = ((FlowDocument)contentHost).TextWrapping;
            // TODO }
        }

        /// <summary>
        /// Calculate line advance from actual line height and the line stacking strategy.
        /// </summary>
        internal double CalcLineAdvance(double lineAdvance)
        {
            // We support MaxHeight and BlockLineHeight stacking strategies
            if (!double.IsNaN(_lineHeight))
            {
                switch (LineStackingStrategy)
                {
                    case LineStackingStrategy.BlockLineHeight:
                        lineAdvance = _lineHeight;
                        break;

                    case LineStackingStrategy.MaxHeight:
                    default:
                        lineAdvance = Math.Max(lineAdvance, _lineHeight);
                        break;

                    //    case LineStackingStrategy.InlineLineHeight:
                    //        // Inline uses the height of the line just processed.
                    //        break;

                    //    case LineStackingStrategy.GridHeight:
                    //        lineAdvance = (((TextDpi.ToTextDpi(lineAdvance) - 1) / TextDpi.ToTextDpi(_lineHeight)) + 1) * _lineHeight;
                    //        break;
                    //}
                }
            }

            return lineAdvance;
        }

        /// <summary>
        /// Raw TextAlignment, without considering IgnoreTextAlignment.
        /// </summary>
        internal TextAlignment TextAlignmentInternal
        {
            get
            {
                return _textAlignment;
            }
        }

        /// <summary>
        /// Ignore text alignment?
        /// </summary>
        internal bool IgnoreTextAlignment
        {
            get { return _ignoreTextAlignment; }
            set { _ignoreTextAlignment = value; }
        }

        ///// <summary>
        ///// Line stacking strategy.
        ///// </summary>
        internal LineStackingStrategy LineStackingStrategy
        {
            get { return _lineStackingStrategy; }
        }

        /// <summary>
        /// Text trimming.
        /// </summary>
        internal TextTrimming TextTrimming
        {
            get { return _textTrimming; }
        }

        /// <summary>
        /// Does it have first line specific properties?
        /// </summary>
        internal bool HasFirstLineProperties
        {
            get { return (/* TODO _markerProperties != null || */ !MathUtilities.IsZero(_textIndent)); }
        }

        /// <summary>
        /// Local cache for first line properties.
        /// </summary>
        internal TextParagraphProperties FirstLineProps
        {
            get
            {
                if (_firstLineProperties == null)
                {
                    _firstLineProperties = new FirstLineProperties(this);
                }
                return _firstLineProperties;
            }
        }

        // ------------------------------------------------------------------
        // Local cache for line properties with paragraph ellipsis.
        // ------------------------------------------------------------------
        /// <summary>
        /// Local cache for line properties with paragraph ellipsis.
        /// </summary>
        internal TextParagraphProperties GetParaEllipsisLineProps(bool firstLine)
        {
            return new ParaEllipsisLineProperties(firstLine ? FirstLineProps : this);
        }

        private TextRunProperties _defaultTextProperties;   // Line's default text properties.
        // TODO private TextMarkerProperties _markerProperties;     // Marker properties
        private FirstLineProperties _firstLineProperties;   // Local cache for first line properties.
        private bool _ignoreTextAlignment;                  // Ignore horizontal alignment?

        private FlowDirection _flowDirection;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;
        private TextTrimming _textTrimming;
        private double _lineHeight;
        private double _textIndent;
        private LineStackingStrategy _lineStackingStrategy;

        // ----------------------------------------------------------------------
        // First text line properties provider.
        // ----------------------------------------------------------------------
        private sealed class FirstLineProperties : TextParagraphProperties
        {
            // ------------------------------------------------------------------
            //
            //  TextParagraphProperties Implementation
            //
            // ------------------------------------------------------------------

            #region TextParagraphProperties Implementation

            // ------------------------------------------------------------------
            // Text flow direction (text advance + block advance direction).
            // ------------------------------------------------------------------
            public override FlowDirection FlowDirection { get { return _lp.FlowDirection; } }

            // ------------------------------------------------------------------
            // Alignment of the line's content.
            // ------------------------------------------------------------------
            public override TextAlignment TextAlignment { get { return _lp.TextAlignment; } }

            // ------------------------------------------------------------------
            // Line's height.
            // ------------------------------------------------------------------
            public override double LineHeight { get { return _lp.LineHeight; } }

            // ------------------------------------------------------------------
            // An instance of this class is always the first line in a paragraph.
            // ------------------------------------------------------------------
            public override bool FirstLineInParagraph { get { return true; } }

            // ------------------------------------------------------------------
            // Line's default text properties.
            // ------------------------------------------------------------------
            public override TextRunProperties DefaultTextRunProperties { get { return _lp.DefaultTextRunProperties; } }

            // ------------------------------------------------------------------
            // Line's text decorations (in addition to any run-level text decorations).
            // ------------------------------------------------------------------
            public override TextDecorationCollection TextDecorations { get { return _lp.TextDecorations; } }

            // ------------------------------------------------------------------
            // Text wrap control.
            // ------------------------------------------------------------------
            public override TextWrapping TextWrapping { get { return _lp.TextWrapping; } }

            // ------------------------------------------------------------------
            // Marker characteristics of the first line in paragraph.
            // ------------------------------------------------------------------
            // TODO public override TextMarkerProperties TextMarkerProperties { get { return _lp.TextMarkerProperties; } }

            // ------------------------------------------------------------------
            // Line indentation.
            // ------------------------------------------------------------------
            public override double Indent { get { return _lp._textIndent; } }

            #endregion TextParagraphProperties Implementation

            // ------------------------------------------------------------------
            // Constructor.
            // ------------------------------------------------------------------
            internal FirstLineProperties(LineProperties lp)
            {
                _lp = lp;
                // properly set the local copy of hyphenator accordingly.
                // TODO Hyphenator = lp.Hyphenator;
            }

            // ------------------------------------------------------------------
            // LineProperties object reference.
            // ------------------------------------------------------------------
            private LineProperties _lp;
        }

        // ----------------------------------------------------------------------
        // Line properties provider for line with paragraph ellipsis.
        // ----------------------------------------------------------------------
        private sealed class ParaEllipsisLineProperties : TextParagraphProperties
        {
            // ------------------------------------------------------------------
            //
            //  TextParagraphProperties Implementation
            //
            // ------------------------------------------------------------------

            #region TextParagraphProperties Implementation

            // ------------------------------------------------------------------
            // Text flow direction (text advance + block advance direction).
            // ------------------------------------------------------------------
            public override FlowDirection FlowDirection { get { return _lp.FlowDirection; } }

            // ------------------------------------------------------------------
            // Alignment of the line's content.
            // ------------------------------------------------------------------
            public override TextAlignment TextAlignment { get { return _lp.TextAlignment; } }

            // ------------------------------------------------------------------
            // Line's height.
            // ------------------------------------------------------------------
            public override double LineHeight { get { return _lp.LineHeight; } }

            // ------------------------------------------------------------------
            // First line in paragraph option.
            // ------------------------------------------------------------------
            public override bool FirstLineInParagraph { get { return _lp.FirstLineInParagraph; } }

            // ------------------------------------------------------------------
            // Always collapsible option.
            // ------------------------------------------------------------------
            public override bool AlwaysCollapsible { get { return _lp.AlwaysCollapsible; } }

            // ------------------------------------------------------------------
            // Line's default text properties.
            // ------------------------------------------------------------------
            public override TextRunProperties DefaultTextRunProperties { get { return _lp.DefaultTextRunProperties; } }

            // ------------------------------------------------------------------
            // Line's text decorations (in addition to any run-level text decorations).
            // ------------------------------------------------------------------
            public override TextDecorationCollection TextDecorations { get { return _lp.TextDecorations; } }

            // ------------------------------------------------------------------
            // Text wrap control.
            // If paragraph ellipsis are enabled, force this line not to wrap.
            // ------------------------------------------------------------------
            public override TextWrapping TextWrapping { get { return TextWrapping.NoWrap; } }

            // ------------------------------------------------------------------
            // Marker characteristics of the first line in paragraph.
            // ------------------------------------------------------------------
            // TODO public override TextMarkerProperties TextMarkerProperties { get { return _lp.TextMarkerProperties; } }

            // ------------------------------------------------------------------
            // Line indentation.
            // ------------------------------------------------------------------
            public override double Indent { get { return _lp.Indent; } }

            #endregion TextParagraphProperties Implementation

            // ------------------------------------------------------------------
            // Constructor.
            // ------------------------------------------------------------------
            internal ParaEllipsisLineProperties(TextParagraphProperties lp)
            {
                _lp = lp;
            }

            // ------------------------------------------------------------------
            // LineProperties object reference.
            // ------------------------------------------------------------------
            private TextParagraphProperties _lp;
        }
    }
}
