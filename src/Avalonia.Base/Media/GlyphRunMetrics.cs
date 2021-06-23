namespace Avalonia.Media
{
    public readonly struct GlyphRunMetrics
    {
        public GlyphRunMetrics(double width, double widthIncludingTrailingWhitespace, int trailingWhitespaceLength,
            int newlineLength, double height)
        {
            Width = width;
            WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
            TrailingWhitespaceLength = trailingWhitespaceLength;
            NewlineLength = newlineLength;
            Height = height;
        }

        public double Width { get; }

        public double WidthIncludingTrailingWhitespace { get; }

        public int TrailingWhitespaceLength { get; }
        
        public int NewlineLength { get; }

        public double Height { get; }
    }
}
