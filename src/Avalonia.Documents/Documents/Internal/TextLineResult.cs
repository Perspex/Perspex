// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Access to calculated information of a line of text created
//              by TextBlock. 
//


using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Documents.Internal;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using MS.Internal.Documents;

namespace MS.Internal.Text
{
    /// <summary>
    /// Provides access to calculated information of a line of text created
    /// by TextBlock.
    /// </summary>
    internal sealed class TextLineResult : LineResult
    {
        //-------------------------------------------------------------------
        //
        //  LineResult Methods
        //
        //-------------------------------------------------------------------

        #region LineResult Methods

        /// <summary>
        /// Retrieves a position matching distance within the line.
        /// </summary>
        /// <param name="distance">Distance within the line.</param>
        /// <returns>
        /// A text position and its orientation matching or closest to the distance.
        /// </returns>
        internal override ITextPointer GetTextPositionFromDistance(double distance)
        {
            return _owner.GetTextPositionFromDistance(_dcp, distance, _layoutBox.Top, _index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            Debug.Assert(false);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
         internal override ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Retrieves the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        /// <returns>
        /// The position after last content character of the line, 
        /// not including any line breaks.
        /// </returns>
        internal override ITextPointer GetContentEndPosition()
        {
            EnsureComplexData();
            return _owner.TextContainer.CreatePointerAtOffset(_dcp + _cchContent, LogicalDirection.Backward);
        }

        /// <summary>
        /// Retrieves the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        /// <returns>
        /// The position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </returns>
        internal override ITextPointer GetEllipsesPosition()
        {
            EnsureComplexData();
            if (_cchEllipses != 0)
            {
                return _owner.TextContainer.CreatePointerAtOffset(_dcp + _cch - _cchEllipses, LogicalDirection.Forward);
            }
            return null;
        }

        /// <summary>
        /// Retrieves the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        /// <returns>
        /// The position after last content character of the line, 
        /// not including any line breaks.
        /// </returns>
        internal override int GetContentEndPositionCP()
        {
            EnsureComplexData();
            return _dcp + _cchContent;
        }

        /// <summary>
        /// Retrieves the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        /// <returns>
        /// The position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </returns>
        internal override int GetEllipsesPositionCP()
        {
            EnsureComplexData();
            return _dcp + _cch - _cchEllipses;
        }

        #endregion LineResult Methods

        //-------------------------------------------------------------------
        //
        //  LineResult Properties
        //
        //-------------------------------------------------------------------

        #region LineResult Properties

        /// <summary>
        /// ITextPointer representing the beginning of the Line's contents.
        /// </summary>
        internal override ITextPointer StartPosition
        {
            get
            {
                if (_startPosition == null)
                {
                    _startPosition = _owner.TextContainer.CreatePointerAtOffset(_dcp, LogicalDirection.Forward);
                }
                return _startPosition;
            }
        }

        /// <summary>
        /// ITextPointer representing the end of the Line's contents.
        /// </summary>
        internal override ITextPointer EndPosition
        {
            get
            {
                if (_endPosition == null)
                {
                    _endPosition = _owner.TextContainer.CreatePointerAtOffset(_dcp + _cch, LogicalDirection.Backward);
                }
                return _endPosition;
            }
        }

        /// <summary>
        /// Character position representing the beginning of the Line's contents.
        /// </summary>
        internal override int StartPositionCP { get { return _dcp; } }

        /// <summary>
        /// Character position representing the end of the Line's contents.
        /// </summary>
        internal override int EndPositionCP { get { return _dcp + _cch; } }

        /// <summary>
        /// The bounding rectangle of the line.
        /// </summary>
        internal override Rect LayoutBox { get { return _layoutBox; } }

        /// <summary>
        /// The dominant baseline of the line. 
        /// Distance from the top of the line to the baseline.
        /// </summary>
        internal override double Baseline { get { return _baseline; } }

        #endregion LineResult Properties

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the line.</param>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="cch">Number of all characters in the line.</param>
        /// <param name="layoutBox">Rectangle of the line within a text paragraph.</param>
        /// <param name="baseline">Distance from the top of the line to the baseline.</param>
        /// <param name="index">Index of the line within the text block</param>
        internal TextLineResult(NewTextBlock owner, int dcp, int cch, Rect layoutBox, double baseline, int index)
        {
            _owner = owner;
            _dcp = dcp;
            _cch = cch;
            _layoutBox = layoutBox;
            _baseline = baseline;
            _index = index;
            _cchContent = _cchEllipses = -1;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private void EnsureComplexData()
        {
            if (_cchContent == -1)
            {
                _owner.GetLineDetails(_dcp, _index, _layoutBox.Top, out _cchContent, out _cchEllipses);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Owner of the line.
        /// </summary>
        private readonly NewTextBlock _owner;

        /// <summary>
        /// Index of the first character in the line.
        /// </summary>
        private readonly int _dcp;

        /// <summary>
        /// Number of all characters in the line.
        /// </summary>
        private readonly int _cch;

        /// <summary>
        /// Rectangle of the line within a text paragraph.
        /// </summary>
        private readonly Rect _layoutBox;
        
        /// <summary>
        /// Index of the line within the TextBlock  
        /// </summary>
        private int _index;

        /// <summary>
        /// The dominant baseline of the line. Distance from the top of the 
        /// line to the baseline.
        /// </summary>
        private readonly double _baseline;

        /// <summary>
        /// ITextPointer representing the beginning of the Line's contents.
        /// </summary>
        private ITextPointer _startPosition;

        /// <summary>
        /// ITextPointer representing the end of the Line's contents.
        /// </summary>
        private ITextPointer _endPosition;

        /// <summary>
        /// Number of characters of content of the line, not including any line breaks.
        /// </summary>
        private int _cchContent;

        /// <summary>
        /// Number of characters hidden by ellipses.
        /// </summary>
        private int _cchEllipses;

        #endregion Private Fields
    }
}
