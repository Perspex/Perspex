// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IEnumerator for TextRange and TextElement content.
//
// 

using System;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
//using System.Text;
//using MS.Utility;
//using System.Windows.Controls;
using Avalonia.Documents;
using Avalonia.Media.TextFormatting;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    internal class TextElementEnumerator<TextElementType> : IEnumerator<TextElementType> where TextElementType : TextElement
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates an enumerator instance.
        // Null start/end creates an empty enumerator.
        internal TextElementEnumerator(TextPointer start, TextPointer end)
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

        public void Dispose()
        {
            // Empty the enumerator
            _current = null;
            _navigator = null;
            GC.SuppressFinalize(this);
        }

        object System.Collections.IEnumerator.Current
        {
            get 
            {
                return this.Current;
            }
        }

        /// <summary>
        /// Return the current object this enumerator is pointing to.  This is 
        /// generally a FrameworkElement, TextElement, an embedded
        /// object, or Text content.
        /// </summary>
        /// <remarks>
        /// According to the IEnumerator spec, the Current property keeps
        /// the element even after the content has been modified
        /// (even if the current element has been deleted from the collection).
        /// This is unlike to Reset or MoveNext which throw after any content change.
        /// </remarks>
        public TextElementType Current
        {
            get 
            { 
                if (_navigator == null)
                {
                    #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorNotStarted)*/);
                }

                if (_current == null)
                {
                    #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                    throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorReachedEnd)*/);
                }

                return _current;
            }
        }

        /// <summary>
        /// Advance the enumerator to the next object in the range.  Return true if content
        /// was found.
        /// </summary>
        public bool MoveNext()
        {
            // Throw if the tree has been modified since this enumerator was created.
            if (_start != null && _generation != _start.TextContainer.Generation)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.EnumeratorVersionChanged)*/);
            }

            // Return false if the collection is empty
            if (_start == null || _start.CompareTo(_end) == 0)
            {
                return false;
            }

            // Return false if the navigator reached the end of the collection
            if (_navigator != null && _navigator.CompareTo(_end) >= 0)
            {
                return false;
            }

            // Advance the navigator
            if (_navigator == null)
            {
                // Set it to the first element for the very first move
                _navigator = new TextPointer(_start);
                _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }
            else
            {
                // Move to the next element in the collection
                Invariant.Assert(_navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart,
                    "Unexpected run type in TextElementEnumerator");

                _navigator.MoveToElementEdge(ElementEdge.AfterEnd);
                _navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            // Update current cache
            if (_navigator.CompareTo(_end) < 0)
            {
                _current = (TextElementType)_navigator.Parent;
            }
            else
            {
                _current = null;
            }

            // Return true if the content was found
            return (_current != null);
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
            _current = null;
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
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

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
        private TextElementType _current;

        #endregion Private Fields
    }
}
