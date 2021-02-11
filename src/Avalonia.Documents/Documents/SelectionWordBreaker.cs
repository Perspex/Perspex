// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Word breaker used for TextSelection's auto-word selection and
//              ctl-arrow navigation.
//


using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{

    // Word breaker used for TextSelection's auto-word selection and ctl-arrow
    // navigation.
    internal static class SelectionWordBreaker
    {
        public const int MinContextLength = 2;

        // Returns true if position points to a word break in the supplied
        // char array.  position is an inter-character offset -- 0 points
        // to the space preceeding the first char, 1 points between the
        // first and second char, etc.
        //
        // insideWordDirection specifies whether we're looking for a word start
        // or word end.  If insideWordDirection == LogicalDirection.Forward, then
        // text = "abc def", position = 4 will return true, but if the direction is
        // backward, no word boundary will be found (looking backward position is
        // at the edge of whitespace, not a word).
        //
        // This method requires at least MinContextLength chars ahead of and
        // following position to give accurate results, but no more.
        internal static bool IsAtWordBoundary(char[] text, int position, LogicalDirection insideWordDirection)
        {
            throw new NotImplementedException();
        }
    }
}
