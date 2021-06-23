using System;
using System.Diagnostics;

namespace Avalonia.Media
{
    /// <summary>
    ///     Represents information about a character hit within a glyph run.
    /// </summary>
    /// <remarks>
    ///     The CharacterHit structure provides information about the index of the first
    ///     character that got hit as well as information about leading or trailing edge.
    /// </remarks>
    [DebuggerDisplay("CharacterHit({FirstCharacterIndex}, {TrailingLength})")]
    public readonly struct CharacterHit : IEquatable<CharacterHit>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterHit"/> structure.
        /// </summary>
        /// <param name="firstCharacterIndex">Index of the first character that got hit.</param>
        /// <param name="trailingLength">In the case of a leading edge, this value is 0. In the case of a trailing edge,
        /// this value is the number of code points until the next valid caret position.</param>
        public CharacterHit(int firstCharacterIndex, int trailingLength = 0)
        {
            FirstCharacterIndex = firstCharacterIndex;

            TrailingLength = trailingLength;
        }

        /// <summary>
        ///     Gets the index of the first character that got hit.
        /// </summary>
        public int FirstCharacterIndex { get; }

        /// <summary>
        ///     Gets the trailing length value for the character that got hit.
        /// </summary>
        public int TrailingLength { get; }

        public bool Equals(CharacterHit other)
        {
            return FirstCharacterIndex == other.FirstCharacterIndex && TrailingLength == other.TrailingLength;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterHit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return FirstCharacterIndex * 397 ^ TrailingLength;
            }
        }

        public static bool operator ==(CharacterHit left, CharacterHit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterHit left, CharacterHit right)
        {
            return !left.Equals(right);
        }
    }
}
