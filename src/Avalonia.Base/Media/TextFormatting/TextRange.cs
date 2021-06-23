using System;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// References a portion of a text buffer.
    /// </summary>
    public readonly struct TextRange
    {
        public TextRange(int start, int length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets the start.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public int Start { get; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length { get; }

        /// <summary>
        /// Gets the end.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public int End => Start + Length - 1;

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of the slice.
        /// </summary>
        /// <param name="length">The number of elements to return.</param>
        /// <returns>A <see cref="TextRange"/> that contains the specified number of elements from the start of this slice.</returns>
        public TextRange Take(int length)
        {
            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new TextRange(Start, length);
        }

        /// <summary>
        /// Bypasses a specified number of elements in the slice and then returns the remaining elements.
        /// </summary>
        /// <param name="length">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A <see cref="TextRange"/> that contains the elements that occur after the specified index in this slice.</returns>
        public TextRange Skip(int length)
        {
            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new TextRange(Start + length, Length - length);
        }
    }
}
