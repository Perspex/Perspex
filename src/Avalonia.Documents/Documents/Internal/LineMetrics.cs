// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Cached metrics of a text line.
//


using System;
using System.Diagnostics;
using System.Windows;
using Avalonia.Media.TextFormatting;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Storage for metrics of a formatted line.
    // ----------------------------------------------------------------------
    internal struct LineMetrics
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      wrappingWidth - wrapping width for the line
        //      length - number or characters in the line
        //      width - width of the line
        //      height - height of the line
        //      baseline - baseline of the line
        //      hasInlineObjects - has inline objects?
        // ------------------------------------------------------------------
        internal LineMetrics(
#if DEBUG
                    double wrappingWidth,
#endif
                    int length,
                    double width,
                    double height,
                    double baseline,
                    bool hasInlineObjects,
                    TextLineBreak textLineBreak)
        {
#if DEBUG
            _wrappingWidth = wrappingWidth;
#endif
            _start = 0;
            _width  = width;
            _height = height;
            _baseline = baseline;
            _textLineBreak = textLineBreak;

            _packedData = ((uint) length & LengthMask) | (hasInlineObjects ? HasInlineObjectsMask : 0);
        }

        internal LineMetrics(LineMetrics source, double start, double width)
        {
#if DEBUG
            _wrappingWidth = source.WrappingWidth;
#endif
            _start = start;
            _width = width;

            _height = source.Height;
            _baseline = source.Baseline;
            _textLineBreak = source.TextLineBreak;

            _packedData = source._packedData | HasBeenUpdatedMask;
        }

        /// <summary>
        /// Disposes linebreak object
        /// </summary>
        internal LineMetrics Dispose(bool returnUpdatedMetrics)
        {
            if(_textLineBreak != null)
            {
                if (returnUpdatedMetrics)
                {
                    return new LineMetrics(
#if DEBUG
                        _wrappingWidth,
#endif
                        Length,
                        _width,
                        _height,
                        _baseline,
                        HasInlineObjects,
                        null);
                }
            }
            return this;
        }


#if DEBUG
        // ------------------------------------------------------------------
        // Wrapping width for the line.
        // ------------------------------------------------------------------
        internal double WrappingWidth { get { return _wrappingWidth; } }
        private double _wrappingWidth;
#endif

        // ------------------------------------------------------------------
        // Number or characters in the line.
        // ------------------------------------------------------------------
        internal int Length { get { return (int) (_packedData & LengthMask); } }
        private uint _packedData;

        // ------------------------------------------------------------------
        // Width of the line.
        // ------------------------------------------------------------------
        internal double Width
        {
            get { Debug.Assert((_packedData & HasBeenUpdatedMask) != 0); return _width; }
        }
        private double _width;

        // ------------------------------------------------------------------
        // Height of the line.
        // ------------------------------------------------------------------
        internal double Height { get { return _height; } }
        private double _height;

        // ------------------------------------------------------------------
        // Start of the line. Distance from paragraph edge to line start.
        // ------------------------------------------------------------------
        internal double Start
        {
            get { Debug.Assert((_packedData & HasBeenUpdatedMask) != 0); return _start; }
        }
        private double _start;

        // ------------------------------------------------------------------
        // Baseline offset of the line.
        // ------------------------------------------------------------------
        internal double Baseline { get { return _baseline; } }
        private double _baseline;

        // ------------------------------------------------------------------
        // Has inline objects?
        // ------------------------------------------------------------------
        internal bool HasInlineObjects { get { return (_packedData & HasInlineObjectsMask) != 0; } }

        // ------------------------------------------------------------------
        // Line break for formatting. (Line Break In)
        // ------------------------------------------------------------------
        internal TextLineBreak TextLineBreak { get { return _textLineBreak; } }
        private TextLineBreak _textLineBreak;

        private static readonly uint HasBeenUpdatedMask = 0x40000000;
        private static readonly uint LengthMask = 0x3FFFFFFF;
        private static readonly uint HasInlineObjectsMask = 0x80000000;
    }
}
