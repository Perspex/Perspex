// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
//
// Ported from: https://github.com/foliojs/linebreak
// Copied from: https://github.com/toptensoftware/RichTextKit

using System.Diagnostics;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Information about a potential line break position
    /// </summary>
    [DebuggerDisplay("{PositionMeasure}/{PositionWrap} @ {Required}")]
    public readonly struct LineBreak
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="positionMeasure">The code point index to measure to</param>
        /// <param name="positionWrap">The code point index to actually break the line at</param>
        /// <param name="required">True if this is a required line break; otherwise false</param>
        public LineBreak(int positionMeasure, int positionWrap, bool required = false)
        {
            PositionMeasure = positionMeasure;
            PositionWrap = positionWrap;
            Required = required;
        }

        /// <summary>
        /// The break position, before any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This doesn't include trailing whitespace
        /// </remarks>
        public int PositionMeasure { get; }

        /// <summary>
        /// The break position, after any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This includes trailing whitespace
        /// </remarks>
        public int PositionWrap { get; }

        /// <summary>
        /// True if there should be a forced line break here
        /// </summary>
        public bool Required { get; }
    }
}
