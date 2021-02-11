// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: HighlightLayer.Changed event argument.
//

using System.Collections;

namespace System.Windows.Documents
{
    /// <summary>
    /// HighlightLayer.Changed event argument.
    /// </summary>
    internal abstract class HighlightChangedEventArgs
    {
        /// <summary>
        /// Sorted, non-overlapping, readonly collection of TextSegments
        /// affected by a highlight change.
        /// </summary>
        internal abstract IList Ranges { get; }

        /// <summary>
        /// Type identifying the owner of the changed layer.
        /// </summary>
        internal abstract Type OwnerType { get; }
    }
}
