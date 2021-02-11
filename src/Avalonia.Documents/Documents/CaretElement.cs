// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Caret rendering visual.
//

using Avalonia.Media;

namespace System.Windows.Documents
{

    // TODO This is not complete
    internal sealed class CaretElement
    {

        internal static void AddGeometry(ref Geometry geometry, Geometry addedGeometry)
        {
            if (addedGeometry != null)
            {
                if (geometry == null)
                {
                    geometry = addedGeometry;
                }
                else
                {
                   // TODO geometry = Geometry.Combine(geometry, addedGeometry, GeometryCombineMode.Union, null, CaretElement.c_geometryCombineTolerance, ToleranceType.Absolute);
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Caret opacity percent
        private const double CaretOpacity = 0.5;

        // BiDi caret indicator height ratio of caret
        private const double BidiIndicatorHeightRatio = 10.0;

        // default narrow caret width
        private const double DefaultNarrowCaretWidth = 1.0;

        //  selection related data
        internal const double c_geometryCombineTolerance = 1e-4;
        internal const double c_endOfParaMagicMultiplier = 0.5;

        // ZOrder
        internal const int ZOrderValue = System.Int32.MaxValue / 2;

        #endregion Private Fields
    }
}

