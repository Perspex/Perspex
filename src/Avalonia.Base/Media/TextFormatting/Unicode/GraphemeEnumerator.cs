// This source file is adapted from the .NET cross-platform runtime project. 
// (https://github.com/dotnet/runtime/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System.Runtime.InteropServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct GraphemeEnumerator
    {
        private ReadOnlySlice<char> _text;

        public GraphemeEnumerator(ReadOnlySlice<char> text)
        {
            _text = text;
            Current = default;
        }

        /// <summary>
        /// Gets the current <see cref="Grapheme"/>.
        /// </summary>
        public Grapheme Current { get; private set; }

        /// <summary>
        /// Moves to the next <see cref="Grapheme"/>.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_text.IsEmpty)
            {
                return false;
            }

            // Algorithm given at https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules.

            var processor = new Processor(_text);

            processor.MoveNext();

            var firstCodepoint = processor.CurrentCodepoint;

            // First, consume as many Prepend scalars as we can (rule GB9b).
            while (processor.CurrentType == GraphemeBreakClass.Prepend)
            {
                processor.MoveNext();
            }

            // Next, make sure we're not about to violate control character restrictions.
            // Essentially, if we saw Prepend data, we can't have Control | CR | LF data afterward (rule GB5).
            if (processor.CurrentCodeUnitOffset > 0)
            {
                if (processor.CurrentType == GraphemeBreakClass.Control
                    || processor.CurrentType == GraphemeBreakClass.CR
                    || processor.CurrentType == GraphemeBreakClass.LF)
                {
                    goto Return;
                }
            }

            // Now begin the main state machine.

            var previousClusterBreakType = processor.CurrentType;

            processor.MoveNext();

            switch (previousClusterBreakType)
            {
                case GraphemeBreakClass.CR:
                    if (processor.CurrentType != GraphemeBreakClass.LF)
                    {
                        goto Return; // rules GB3 & GB4 (only <LF> can follow <CR>)
                    }

                    processor.MoveNext();
                    goto case GraphemeBreakClass.LF;

                case GraphemeBreakClass.Control:
                case GraphemeBreakClass.LF:
                    goto Return; // rule GB4 (no data after Control | LF)

                case GraphemeBreakClass.L:
                    if (processor.CurrentType == GraphemeBreakClass.L)
                    {
                        processor.MoveNext(); // rule GB6 (L x L)
                        goto case GraphemeBreakClass.L;
                    }
                    else if (processor.CurrentType == GraphemeBreakClass.V)
                    {
                        processor.MoveNext(); // rule GB6 (L x V)
                        goto case GraphemeBreakClass.V;
                    }
                    else if (processor.CurrentType == GraphemeBreakClass.LV)
                    {
                        processor.MoveNext(); // rule GB6 (L x LV)
                        goto case GraphemeBreakClass.LV;
                    }
                    else if (processor.CurrentType == GraphemeBreakClass.LVT)
                    {
                        processor.MoveNext(); // rule GB6 (L x LVT)
                        goto case GraphemeBreakClass.LVT;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeBreakClass.LV:
                case GraphemeBreakClass.V:
                    if (processor.CurrentType == GraphemeBreakClass.V)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x V)
                        goto case GraphemeBreakClass.V;
                    }
                    else if (processor.CurrentType == GraphemeBreakClass.T)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x T)
                        goto case GraphemeBreakClass.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeBreakClass.LVT:
                case GraphemeBreakClass.T:
                    if (processor.CurrentType == GraphemeBreakClass.T)
                    {
                        processor.MoveNext(); // rule GB8 (LVT | T x T)
                        goto case GraphemeBreakClass.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeBreakClass.ExtendedPictographic:
                    // Attempt processing extended pictographic (rules GB11, GB9).
                    // First, drain any Extend scalars that might exist
                    while (processor.CurrentType == GraphemeBreakClass.Extend)
                    {
                        processor.MoveNext();
                    }

                    // Now see if there's a ZWJ + extended pictograph again.
                    if (processor.CurrentType != GraphemeBreakClass.ZWJ)
                    {
                        break;
                    }

                    processor.MoveNext();
                    if (processor.CurrentType != GraphemeBreakClass.ExtendedPictographic)
                    {
                        break;
                    }

                    processor.MoveNext();
                    goto case GraphemeBreakClass.ExtendedPictographic;

                case GraphemeBreakClass.RegionalIndicator:
                    // We've consumed a single RI scalar. Try to consume another (to make it a pair).

                    if (processor.CurrentType == GraphemeBreakClass.RegionalIndicator)
                    {
                        processor.MoveNext();
                    }

                    // Standlone RI scalars (or a single pair of RI scalars) can only be followed by trailers.

                    break; // nothing but trailers after the final RI

                default:
                    break;
            }

            // rules GB9, GB9a
            while (processor.CurrentType == GraphemeBreakClass.Extend
                || processor.CurrentType == GraphemeBreakClass.ZWJ
                || processor.CurrentType == GraphemeBreakClass.SpacingMark)
            {
                processor.MoveNext();
            }

            Return:

            var text = _text.Take(processor.CurrentCodeUnitOffset);

            Current = new Grapheme(firstCodepoint, text);

            _text = _text.Skip(processor.CurrentCodeUnitOffset);

            return true; // rules GB2, GB999
        }

        [StructLayout(LayoutKind.Auto)]
        private ref struct Processor
        {
            private readonly ReadOnlySlice<char> _buffer;
            private int _codeUnitLengthOfCurrentScalar;

            internal Processor(ReadOnlySlice<char> buffer)
            {
                _buffer = buffer;
                _codeUnitLengthOfCurrentScalar = 0;
                CurrentCodepoint = Codepoint.ReplacementCodepoint;
                CurrentType = GraphemeBreakClass.Other;
                CurrentCodeUnitOffset = 0;
            }

            public int CurrentCodeUnitOffset { get; private set; }

            /// <summary>
            /// Will be <see cref="GraphemeBreakClass.Other"/> if invalid data or EOF reached.
            /// Caller shouldn't need to special-case this since the normal rules will halt on this condition.
            /// </summary>
            public GraphemeBreakClass CurrentType { get; private set; }

            /// <summary>
            ///     Get the currently processed <see cref="Codepoint"/>.
            /// </summary>
            public Codepoint CurrentCodepoint { get; private set; }

            public void MoveNext()
            {
                // For ill-formed subsequences (like unpaired UTF-16 surrogate code points), we rely on
                // the decoder's default behavior of interpreting these ill-formed subsequences as
                // equivalent to U+FFFD REPLACEMENT CHARACTER. This code point has a boundary property
                // of Other (XX), which matches the modifications made to UAX#29, Rev. 35.
                // See: https://www.unicode.org/reports/tr29/tr29-35.html#Modifications
                // This change is also reflected in the UCD files. For example, Unicode 11.0's UCD file
                // https://www.unicode.org/Public/11.0.0/ucd/auxiliary/GraphemeBreakProperty.txt
                // has the line "D800..DFFF    ; Control # Cs [2048] <surrogate-D800>..<surrogate-DFFF>",
                // but starting with Unicode 12.0 that line has been removed.
                //
                // If a later version of the Unicode Standard further modifies this guidance we should reflect
                // that here.

                if (CurrentCodeUnitOffset == _buffer.Length)
                {
                    CurrentCodepoint = Codepoint.ReplacementCodepoint;
                }
                else
                {
                    CurrentCodeUnitOffset += _codeUnitLengthOfCurrentScalar;

                    if (CurrentCodeUnitOffset < _buffer.Length)
                    {
                        CurrentCodepoint = Codepoint.ReadAt(_buffer, CurrentCodeUnitOffset,
                            out _codeUnitLengthOfCurrentScalar);
                    }
                    else
                    {
                        CurrentCodepoint = Codepoint.ReplacementCodepoint;
                    }
                }

                CurrentType = CurrentCodepoint.GraphemeBreakClass;
            }
        }
    }
}
