// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Cached metrics of an inline objects. 
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Documents.Internal;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using MS.Internal;

namespace MS.Internal.Text
{
    /// <summary>
    /// Inline object representation as TextRun.
    /// </summary>
    internal sealed class InlineObject: TextEmbeddedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">Text position of the inline object in the text array.</param>
        /// <param name="cch">Number of text position in the text array occupied by the inline object.</param>
        /// <param name="element">Control representing the inline object.</param>
        /// <param name="textProps">Text run properties for the inline object.</param>
        /// <param name="host">NewTextBlock element - the host of the inline object.</param>
        internal InlineObject(int dcp, int cch, IControl element, TextRunProperties textProps, NewTextBlock host)
        {
            _dcp = dcp;
            _cch = cch;
            _element = element;
            _textProps = textProps;
            _host = host;
        }

        /// <summary>
        /// Get inline object's measurement metrics.
        /// </summary>
        /// <param name="remainingParagraphWidth">Remaining paragraph width.</param>
        /// <returns>Inline object metrics.</returns>
        public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
        {
            Size desiredSize = _host.MeasureChild(this);

            double baseline = desiredSize.Height;
            double baselineOffsetValue = (double) Element.GetValue(NewTextBlock.BaselineOffsetProperty);

            if(!double.IsNaN(baselineOffsetValue))
            {
                baseline = baselineOffsetValue;
            }
            return new TextEmbeddedObjectMetrics(desiredSize.Width, desiredSize.Height, baseline);
        }

        /// <summary>
        /// Get computed bounding box of the inline object.
        /// </summary>
        /// <param name="rightToLeft">Run is drawn from right to left.</param>
        /// <param name="sideways">Run is drawn with its side parallel to baseline.</param>
        /// <returns>Computed bounding box size of text object.</returns>
        public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
        {
            if (_element.IsArrangeValid)
            {
                // Initially assume that bounding box is the same as layout box.
                Size size = _element.DesiredSize;
                double baseline = !sideways ? size.Height : size.Width;
                double baselineOffsetValue = (double)Element.GetValue(NewTextBlock.BaselineOffsetProperty);

                if (!sideways && !double.IsNaN(baselineOffsetValue))
                {
                    baseline = (double)baselineOffsetValue;
                }
                return new Rect(0, -baseline, sideways ? size.Height : size.Width, sideways ? size.Width : size.Height);
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// Draw the inline object.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        /// <param name="origin">Origin where the object is drawn.</param>
        /// <param name="rightToLeft">Run is drawn from right to left.</param>
        /// <param name="sideways">Run is drawn with its side parallel to baseline.</param>
        public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        {
            // Inline object has its own visual and it is attached to a visual
            // tree during arrange process.
            // Do nothing here.
        }

        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public override TextRunProperties Properties { get { return _textProps;  } }

        /// <summary>
        /// Flag indicates whether inline object has fixed size regardless of where 
        /// it is placed within a line.
        /// </summary>
        public override bool HasFixedSize
        {
            get
            {
                // Size of inline object is not dependent on position in the line.
                return true;
            }
        }

        /// <summary>
        /// Text position on the inline object in the text array.
        /// </summary>
        internal int Dcp { get { return _dcp; } }

        /// <summary>
        /// UIElement representing the inline object.
        /// </summary>
        internal IControl Element { get { return _element; } }

        /// <summary>
        /// Text position on the inline object in the text array.
        /// </summary>
        private readonly int _dcp;

        /// <summary>
        /// Number of text position in the text array occupied by the inline object.
        /// </summary>
        private readonly int _cch;

        /// <summary>
        /// UIElement representing the inline object.
        /// </summary>
        private readonly IControl _element;

        /// <summary>
        /// Text run properties for the inline object.
        /// </summary>
        private readonly TextRunProperties _textProps;

        /// <summary>
        /// Text element - the host of the inline object.
        /// </summary>
        private readonly NewTextBlock _host;
    }
}
