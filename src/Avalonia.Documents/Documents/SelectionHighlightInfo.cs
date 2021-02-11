// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Highlight render properties for selected text.
//

using Avalonia.Media;

namespace System.Windows.Documents
{
    /// <summary>
    /// Highlight render properties for selected text.
    /// </summary>
    internal static class SelectionHighlightInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Static constructor.
        static SelectionHighlightInfo()
        {
            // TODO: All uses of this should probably be replaced by a themed style property
            _objectMaskBrush = new SolidColorBrush(/* TODO SystemColors.HighlightColor*/);
            _objectMaskBrush.Opacity = 0.5;
        }

        #endregion Constructors
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Brush used to override selected text's ForegroundProperty.
        /// </summary>
        internal static Brush ForegroundBrush
        {
            get
            {
                return new SolidColorBrush(Colors.Red); // TODO this should probably be replaced by a styled property in the Theme
                // return SystemColors.HighlightTextBrush;
            }
        }

        /// <summary>
        /// Brush used to override selected text's BackgroundProperty.
        /// </summary>
        internal static Brush BackgroundBrush
        {
            get
            {
                return new SolidColorBrush(Colors.Red); // TODO this should probably be replaced by a styled property in the Theme
                // return SystemColors.HighlightBrush;
            }
        }

        /// <summary>
        /// Brush used to highlight selected embedded objects.
        /// </summary>
        internal static Brush ObjectMaskBrush
        {
            get
            {
                return _objectMaskBrush;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Brush used to highlight selected embedded objects.
        private static readonly Brush _objectMaskBrush;

        #endregion Private Fields
    }
}
