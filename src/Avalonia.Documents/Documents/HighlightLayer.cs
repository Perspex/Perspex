// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A group of non-overlapping text highlights with a single owner.
//

using System.Collections;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    /// <summary>
    /// A group of non-overlapping text highlights with a single owner.
    /// 
    /// Conceptually, this object is a collection of ranges and a list
    /// of property/values pairs highlighting content under each range.
    /// </summary>
    internal abstract class HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns the value of a property stored on scoping highlight, if any.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        /// <returns>
        /// The property value if set on any scoping highlight.  If no property
        /// value is set, returns DependencyProperty.UnsetValue.
        /// </returns>
        internal abstract object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction);

        /// <summary>
        /// Returns true iff the indicated content has scoping highlights.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        internal abstract bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction);

        /// <summary>
        /// Returns the position of the next highlight start or end in an
        /// indicated direction, or null if there is no such position.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        internal abstract StaticTextPointer GetNextChangePosition(StaticTextPointer textPosition, LogicalDirection direction);

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Type identifying the owner of this layer for Highlights.GetHighlightValue calls.
        /// </summary>
        internal abstract Type OwnerType { get; }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Event raised when a highlight is inserted, removed, moved, or
        /// has a local property value change.
        /// </summary>
        internal abstract event HighlightChangedEventHandler Changed;

        #endregion Internal Events
    }
}
