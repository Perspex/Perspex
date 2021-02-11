// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Bounding rectangle of text range and text run
//
//  Spec:      Text Formatting API.doc
//
//


using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// The bounding rectangle of a range of characters
    /// </summary>
    public sealed class TextBounds
    {
        /// <summary>
        /// Constructing TextBounds object
        /// </summary>
        internal TextBounds(
            Rect bounds,
            /* TODO FlowDirection           flowDirection, */
            IList<TextRunBounds> runBounds
        )
        {
            _bounds = bounds;
            _flowDirection = FlowDirection.LeftToRight;
            /* TODO _flowDirection = flowDirection; */
            _runBounds = runBounds;
        }


        /// <summary>
        /// Bounds rectangle
        /// </summary>
        public Rect Rectangle
        {
            get { return _bounds; }
        }


        /// <summary>
        /// Get a list of run bounding rectangles
        /// </summary>
        /// <returns>Array of text run bounds</returns>
        public IList<TextRunBounds> TextRunBounds
        {
            get { return _runBounds; }
        }


        /// <summary>
        /// Text flow direction inside the boundary rectangle
        /// </summary>
        public FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }


        private FlowDirection _flowDirection;
        private Rect _bounds;
        private IList<TextRunBounds> _runBounds;
    }


    /// <summary>
    /// The bounding rectangle of text run
    /// </summary>
    public sealed class TextRunBounds
    {
        /// <summary>
        /// Constructing TextRunBounds
        /// </summary>
        internal TextRunBounds(
            Rect bounds,
            int cpFirst,
            int cpEnd,
            TextRun textRun
        )
        {
            _cpFirst = cpFirst;
            _cch = cpEnd - cpFirst;
            _bounds = bounds;
            _textRun = textRun;
        }


        /// <summary>
        /// First text source character index of text run
        /// </summary>
        public int TextSourceCharacterIndex
        {
            get { return _cpFirst; }
        }


        /// <summary>
        /// character length of bounded text run
        /// </summary>
        public int Length
        {
            get { return _cch; }
        }


        /// <summary>
        /// Text run bounding rectangle
        /// </summary>
        public Rect Rectangle
        {
            get { return _bounds; }
        }


        /// <summary>
        /// text run
        /// </summary>
        public TextRun TextRun
        {
            get { return _textRun; }
        }

        private int _cpFirst;
        private int _cch;
        private Rect _bounds;
        private TextRun _textRun;
    }
}
