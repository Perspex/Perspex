// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.
//
// Description: IEnumerator for TextRange and TextElement content.
//

using System;
using System.Collections;
using MS.Internal;
using System.Text;
//using MS.Utility;
//using System.Windows.Controls;
using Avalonia.Documents;
using Avalonia.Media.TextFormatting;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    internal class RangeContentEnumerator : IEnumerator
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates an enumerator instance.
        // Null start/end creates an empty enumerator.
        internal RangeContentEnumerator(TextPointer start, TextPointer end)
        {
            Invariant.Assert(start != null && end != null || start == null && end == null, "If start is null end should be null!");

            _start = start;
            _end = end;

            // Remember what generation the backing store was in when we started,
            // so we can throw if this enumerator is accessed after content has
            // changed.
            if (_start != null)
            {
                _generation = _start.TextContainer.Generation;
            }
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Return the current object this enumerator is pointing to.  This is 
        /// generally a FrameworkElement, TextElement, an embedded
        /// object, or Text content.
        /// </summary>
        public object Current
        {
            get 
            { 
                int runLength;
                int offset;

                if (_navigator == null)
                {
                    // Disable presharp 6503: Property get methods should not throw exceptions.
                    // We must throw here -- it's part of the IEnumerator contract.
                    #pragma warning suppress 6503
                    throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorNotStarted)*/);
                }

                // Check _currentCache before looking at _navigator.  For the
                // last item in the enumeration, _navigator == _end, but
                // that's ok, the previous get_Current call (which sets _currentCache
                // non-null) put it there.
                if (_currentCache != null)
                {
                    return _currentCache;
                }

                if (_navigator.CompareTo(_end) >= 0)
                {
                    #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorReachedEnd)*/);
                }

                // Throw if the tree has been modified since this enumerator was created unless a tree walk
                // by the property system is in progress. For example, changing DataContext on FlowDocument
                // can invalidate a binding on Run.Text during the inherited property change tree walk,
                // which in turn can modify the TextContainer.
                if (_generation != _start.TextContainer.Generation && !IsLogicalChildrenIterationInProgress)
                {
                    #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorVersionChanged)*/);
                }

                switch (_navigator.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.Text:
                        offset = 0;

                        // Merge all successive text runs into a single value.
                        do
                        {
                            runLength = _navigator.GetTextRunLength(LogicalDirection.Forward);
                            EnsureBufferCapacity(offset + runLength);
                            _navigator.GetTextInRun(LogicalDirection.Forward, _buffer, offset, runLength);
                            offset += runLength;
                            _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                        while (_navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text);

                        _currentCache = new string(_buffer, 0, offset);

                        // _navigator is at next content.
                        break;

                    case TextPointerContext.EmbeddedElement:
                        _currentCache = _navigator.GetAdjacentElement(LogicalDirection.Forward);

                        // Position _navigator at next content.
                        _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    case TextPointerContext.ElementStart:
                        // Return the element, and advance past its entire content.
                        _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        _currentCache = _navigator.Parent;

                        // Position _navigator at next content.
                        _navigator.MoveToElementEdge(ElementEdge.AfterEnd);
                        break;

                    default:
                        Invariant.Assert(false, "Unexpected run type!");
                        _currentCache = null; // We should never get here.
                        break;
                }

                return _currentCache;
            }
        }

        /// <summary>
        /// Advance the enumerator to the next object in the range.  Return true if content
        /// was found.
        /// </summary>
        public bool MoveNext()
        {
            // Throw if the tree has been modified since this enumerator was created unless a tree walk
            // by the property system is in progress. For example, changing DataContext on FlowDocument
            // can invalidate a binding on Run.Text during the inherited property change tree walk,
            // which in turn can modify the TextContainer.
            if (_start != null && _generation != _start.TextContainer.Generation && !IsLogicalChildrenIterationInProgress)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorVersionChanged)*/);
            }

            if (_start == null || _start.CompareTo(_end) == 0)
                return false;

            if (_navigator != null && _navigator.CompareTo(_end) >= 0)
            {
                return false;
            }

            if (_navigator == null)
            {
                _navigator = new TextPointer(_start);
            }
            else if (_currentCache == null) // If we have a cache, _navigator is already positioned at the next item.
            {
                switch (_navigator.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.Text:

                        // Skip all successive text runs as a single value.
                        do
                        {
                            _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                        while (_navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text && _navigator.CompareTo(_end) < 0);

                        break;

                    case TextPointerContext.EmbeddedElement:
                        _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    case TextPointerContext.ElementStart:
                        // Return the element, and advance past its entire content.
                        _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        _navigator.MoveToPosition(((TextElement)_navigator.Parent).ElementEnd);
                        break;

                    default:
                        Invariant.Assert(false, "Unexpected run type!");
                        break;
                }
            }

            _currentCache = null;

            return (_navigator.CompareTo(_end) < 0);
        }

        /// <summary>
        /// Reset the enumerator to the start of the range.
        /// </summary>
        public void Reset()
        {
            // Throw if the tree has been modified since this enumerator was created.
            if (_start != null && _generation != _start.TextContainer.Generation)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorVersionChanged)*/);
            }

            _navigator = null;
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        #region Protected Methods

        #endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void EnsureBufferCapacity(int size)
        {
            char [] newBuffer;

            if (_buffer == null)
            {
                _buffer = new char[size];
            }
            else if (_buffer.Length < size)
            {
                newBuffer = new char[Math.Max(2*_buffer.Length, size)];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsLogicalChildrenIterationInProgress
        {
            get 
            {
                //IAvaloniaObject node = _start.Parent;

                //while (node != null)
                //{
                //    FrameworkElement fe = node as FrameworkElement;
                //    if (fe != null)
                //    {
                //        if (fe.IsLogicalChildrenIterationInProgress)
                //        {
                //            return true;
                //        }
                //    }
                //    else
                //    {
                //        FrameworkContentElement fce = node as FrameworkContentElement;
                //        if (fce != null && fce.IsLogicalChildrenIterationInProgress)
                //        {
                //            return true;
                //        }
                //    }

                //    node = LogicalTreeHelper.GetParent(node);
                //}

                return false;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Edges of the span to enumerator over.
        private readonly TextPointer _start;
        private readonly TextPointer _end;

        // Backing store generation when this enumerator was created.
        private readonly uint _generation;

        // Current position in the enumeration.
        private TextPointer _navigator;

        // Calculated content at the current position.
        private object _currentCache;

        // Buffer to text content retrieval.
        private char [] _buffer;

        #endregion Private Fields
    }
}
