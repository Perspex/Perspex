namespace Avalonia.Utilities
{
    /// <summary>
    /// Pairing of value and positions sharing that value.
    /// </summary>
    public readonly struct ValueSpan<T>
    {
        public ValueSpan(int start, int length, T value)
        {
            Start = start;
            Length = length;
            Value = value;
        }

        /// <summary>
        /// Get's the start of the span.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Get's the length of the span.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Get's the value of the span.
        /// </summary>
        public T Value { get; }
    }
}
