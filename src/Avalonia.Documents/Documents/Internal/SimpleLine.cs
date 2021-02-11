// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text line formatter. 
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Avalonia.Documents.Internal;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Text line formatter.
    // ----------------------------------------------------------------------
    internal sealed class SimpleLine : Line
    {
        // ------------------------------------------------------------------
        //
        //  TextSource Implementation
        //
        // ------------------------------------------------------------------

        #region TextSource Implementation

        // ------------------------------------------------------------------
        // Get a text run at specified text source position.
        // ------------------------------------------------------------------
        public override TextRun GetTextRun(int dcp)
        {
            Debug.Assert(dcp >= 0, "Character index must be non-negative.");

            TextRun run;

            // There is only one run of text.
            if (dcp  < _content.Length)
            {
                // LineLayout may ask for dcp != 0. This case may only happen during partial 
                // validation of TextRunCache.
                // Example:
                //  1) TextRunCache and LineMetrics array were created during measure process.
                //  2) Before OnRender is called somebody invalidates render only property.
                //     This invalidates TextRunCache.
                //  3) Before OnRender is called InputHitTest is invoked. Because LineMetrics
                //     array is valid, we don't have to recreate all lines. There is only
                //     need to recreate the N-th line (line that has been hit).
                //     During line recreation LineLayout will not refetch all runs from the 
                //     beginning of TextBlock control - it will ask for the run at the beginning 
                //     of the current line.
                // For this reason set 'offsetToFirstChar' to 'dcp' value.
                run = new TextCharacters(new ReadOnlySlice<char>(_content.AsMemory(), dcp, _content.Length - dcp), _textProps);
            }
            else
            {
                run = new TextEndOfParagraph();
            }

            if (run.Properties != null)
            {
                // TODO run.Properties.PixelsPerDip = this.PixelsPerDip;
            }

            return run;
        }

        #endregion TextSource Implementation

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      owner - owner of the line.
        // ------------------------------------------------------------------
        internal SimpleLine(NewTextBlock owner, string content, TextRunProperties textProps) : base(owner)
        {
            Debug.Assert(content != null);
            _content = content;
            _textProps = textProps;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // ------------------------------------------------------------------
        // Content of the line.
        // ------------------------------------------------------------------
        private readonly string _content;

        // ------------------------------------------------------------------
        // Text properties.
        // ------------------------------------------------------------------
        private readonly TextRunProperties _textProps;

        #endregion Private Fields
    }
}
