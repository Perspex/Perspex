using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Utilities
{
    /// <summary>
    ///     ReadOnlySlice enables the ability to work with a sequence within a region of memory and retains the position in within that region.
    /// </summary>
    /// <typeparam name="T">The type of elements in the slice.</typeparam>
    [DebuggerTypeProxy(typeof(ReadOnlySlice<>.ReadOnlySliceDebugView))]
    public readonly struct ReadOnlySlice<T> : IReadOnlyList<T>
    {
        public ReadOnlySlice(ReadOnlyMemory<T> buffer) : this(buffer, 0, buffer.Length) { }

        public ReadOnlySlice(ReadOnlyMemory<T> buffer, int start, int length)
        {
            Buffer = buffer;
            Start = start;
            Length = length;
        }

        /// <summary>
        ///     Gets the start.
        /// </summary>
        /// <value>
        ///     The start.
        /// </value>
        public int Start { get; }

        /// <summary>
        ///     Gets the end.
        /// </summary>
        /// <value>
        ///     The end.
        /// </value>
        public int End => Start + Length - 1;

        /// <summary>
        ///     Gets the length.
        /// </summary>
        /// <value>
        ///     The length.
        /// </value>
        public int Length { get; }

        /// <summary>
        ///     Gets a value that indicates whether this instance of <see cref="ReadOnlySlice{T}"/> is Empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        ///     The buffer.
        /// </summary>
        public ReadOnlyMemory<T> Buffer { get; }

        public T this[int index] => Buffer.Span[index];

        /// <summary>
        ///     Returns a sub slice of elements that start at the specified index and has the specified number of elements.
        /// </summary>
        /// <param name="start">The start of the sub slice.</param>
        /// <param name="length">The length of the sub slice.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the specified number of elements from the specified start.</returns>
        public ReadOnlySlice<T> AsSlice(int start, int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (start < Start || start > End)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start + length > Start + Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var bufferOffset = start - Start;

            return new ReadOnlySlice<T>(Buffer.Slice(bufferOffset), start, length);
        }

        /// <summary>
        ///     Returns a specified number of contiguous elements from the start of the slice.
        /// </summary>
        /// <param name="length">The number of elements to return.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the specified number of elements from the start of this slice.</returns>
        public ReadOnlySlice<T> Take(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ReadOnlySlice<T>(Buffer.Slice(0, length), Start, length);
        }

        /// <summary>
        ///     Bypasses a specified number of elements in the slice and then returns the remaining elements.
        /// </summary>
        /// <param name="length">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the elements that occur after the specified index in this slice.</returns>
        public ReadOnlySlice<T> Skip(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ReadOnlySlice<T>(Buffer.Slice(length), Start + length, Length - length);
        }

        /// <summary>
        /// Returns an enumerator for the slice.
        /// </summary>
        public ImmutableReadOnlyListStructEnumerator<T> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<T>(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IReadOnlyCollection<T>.Count => Length;

        T IReadOnlyList<T>.this[int index] => this[index];

        public static implicit operator ReadOnlySlice<T>(T[] array)
        {
            return new ReadOnlySlice<T>(array);
        }

        public static implicit operator ReadOnlySlice<T>(ReadOnlyMemory<T> memory)
        {
            return new ReadOnlySlice<T>(memory);
        }

        internal class ReadOnlySliceDebugView
        {
            private readonly ReadOnlySlice<T> _readOnlySlice;

            public ReadOnlySliceDebugView(ReadOnlySlice<T> readOnlySlice)
            {
                _readOnlySlice = readOnlySlice;
            }

            public int Start => _readOnlySlice.Start;

            public int End => _readOnlySlice.End;

            public int Length => _readOnlySlice.Length;

            public bool IsEmpty => _readOnlySlice.IsEmpty;

            public ReadOnlyMemory<T> Items => _readOnlySlice.Buffer;
        }
    }
}
