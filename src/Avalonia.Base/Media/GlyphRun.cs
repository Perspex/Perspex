using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    ///     Represents a sequence of glyphs from a single face of a single font at a single size, and with a single rendering style.
    /// </summary>
    public sealed class GlyphRun : IDisposable
    {
        private static readonly IComparer<ushort> s_ascendingComparer = Comparer<ushort>.Default;
        private static readonly IComparer<ushort> s_descendingComparer = new ReverseComparer<ushort>();

        private IGlyphRunImpl _glyphRunImpl;
        private GlyphTypeface _glyphTypeface;
        private double _fontRenderingEmSize;
        private int _biDiLevel;
        private Point? _baselineOrigin;
        private GlyphRunMetrics? _glyphRunMetrics;

        private ReadOnlySlice<ushort> _glyphIndices;
        private ReadOnlySlice<double> _glyphAdvances;
        private ReadOnlySlice<Vector> _glyphOffsets;
        private ReadOnlySlice<ushort> _glyphClusters;
        private ReadOnlySlice<char> _characters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class.
        /// </summary>
        public GlyphRun()
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="glyphIndices">The glyph indices.</param>
        /// <param name="glyphAdvances">The glyph advances.</param>
        /// <param name="glyphOffsets">The glyph offsets.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphClusters">The glyph clusters.</param>
        /// <param name="biDiLevel">The bidi level.</param>
        public GlyphRun(
            GlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlySlice<ushort> glyphIndices,
            ReadOnlySlice<double> glyphAdvances = default,
            ReadOnlySlice<Vector> glyphOffsets = default,
            ReadOnlySlice<char> characters = default,
            ReadOnlySlice<ushort> glyphClusters = default,
            int biDiLevel = 0)
        {
            GlyphTypeface = glyphTypeface;

            FontRenderingEmSize = fontRenderingEmSize;

            GlyphIndices = glyphIndices;

            GlyphAdvances = glyphAdvances;

            GlyphOffsets = glyphOffsets;

            Characters = characters;

            GlyphClusters = glyphClusters;

            BiDiLevel = biDiLevel;
        }

        /// <summary>
        ///     Gets or sets the <see cref="Media.GlyphTypeface"/> for the <see cref="GlyphRun"/>.
        /// </summary>
        public GlyphTypeface GlyphTypeface
        {
            get => _glyphTypeface;
            set => Set(ref _glyphTypeface, value);
        }

        /// <summary>
        ///     Gets or sets the em size used for rendering the <see cref="GlyphRun"/>.
        /// </summary>
        public double FontRenderingEmSize
        {
            get => _fontRenderingEmSize;
            set => Set(ref _fontRenderingEmSize, value);
        }

        /// <summary>
        ///     Gets or sets the conservative bounding box of the <see cref="GlyphRun"/>.
        /// </summary>
        public Size Size => new Size(Metrics.WidthIncludingTrailingWhitespace, Metrics.Height);

        /// <summary>
        /// 
        /// </summary>
        public GlyphRunMetrics Metrics
        {
            get
            {
                _glyphRunMetrics ??= CreateGlyphRunMetrics();

                return _glyphRunMetrics.Value;
            }
        }

        /// <summary>
        ///     Gets or sets the baseline origin of the<see cref="GlyphRun"/>.
        /// </summary>
        public Point BaselineOrigin
        {
            get
            {
                _baselineOrigin ??= CalculateBaselineOrigin();

                return _baselineOrigin.Value;
            }
            set => Set(ref _baselineOrigin, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="ushort"/> values that represent the glyph indices in the rendering physical font.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphIndices
        {
            get => _glyphIndices;
            set => Set(ref _glyphIndices, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="double"/> values that represent the advances corresponding to the glyph indices.
        /// </summary>
        public ReadOnlySlice<double> GlyphAdvances
        {
            get => _glyphAdvances;
            set => Set(ref _glyphAdvances, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="Vector"/> values representing the offsets of the glyphs in the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<Vector> GlyphOffsets
        {
            get => _glyphOffsets;
            set => Set(ref _glyphOffsets, value);
        }

        /// <summary>
        ///     Gets or sets the list of UTF16 code points that represent the Unicode content of the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<char> Characters
        {
            get => _characters;
            set => Set(ref _characters, value);
        }

        /// <summary>
        ///     Gets or sets a list of <see cref="int"/> values representing a mapping from character index to glyph index.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphClusters
        {
            get => _glyphClusters;
            set => Set(ref _glyphClusters, value);
        }

        /// <summary>
        ///     Gets or sets the bidirectional nesting level of the <see cref="GlyphRun"/>.
        /// </summary>
        public int BiDiLevel
        {
            get => _biDiLevel;
            set => Set(ref _biDiLevel, value);
        }

        /// <summary>
        /// Gets the scale of the current <see cref="Media.GlyphTypeface"/>
        /// </summary>
        internal double Scale => FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

        /// <summary>
        /// Returns <c>true</c> if the text direction is left-to-right. Otherwise, returns <c>false</c>.
        /// </summary>
        public bool IsLeftToRight => ((BiDiLevel & 1) == 0);

        /// <summary>
        /// The platform implementation of the <see cref="GlyphRun"/>.
        /// </summary>
        public IGlyphRunImpl GlyphRunImpl
        {
            get
            {
                if (_glyphRunImpl == null)
                {
                    Initialize();
                }

                return _glyphRunImpl;
            }
        }

        /// <summary>
        /// Retrieves the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the specified character hit.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the offset.</param>
        /// <returns>
        /// A <see cref="double"/> that represents the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the character hit.
        /// </returns>
        public double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var distance = 0.0;

            if (characterHit.FirstCharacterIndex + characterHit.TrailingLength > Characters.End)
            {
                return Size.Width;
            }

            var glyphIndex = FindGlyphIndex(characterHit.FirstCharacterIndex);

            if (!GlyphClusters.IsEmpty)
            {
                var currentCluster = GlyphClusters[glyphIndex];

                if (characterHit.TrailingLength > 0)
                {
                    while (glyphIndex < GlyphClusters.Length && GlyphClusters[glyphIndex] == currentCluster)
                    {
                        glyphIndex++;
                    }
                }
            }

            for (var i = 0; i < glyphIndex; i++)
            {
                distance += GetGlyphAdvance(i);
            }

            return distance;
        }

        /// <summary>
        /// Retrieves the <see cref="CharacterHit"/> value that represents the character hit of the caret of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="distance">Offset to use for computing the caret character hit.</param>
        /// <param name="isInside">Determines whether the character hit is inside the <see cref="GlyphRun"/>.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> value that represents the character hit that is closest to the distance value.
        /// The out parameter <c>isInside</c> returns <c>true</c> if the character hit is inside the <see cref="GlyphRun"/>; otherwise, <c>false</c>.
        /// </returns>
        public CharacterHit GetCharacterHitFromDistance(double distance, out bool isInside)
        {
            // Before
            if (distance < 0)
            {
                isInside = false;

                var firstCharacterHit = FindNearestCharacterHit(_glyphClusters[0], out _);

                return IsLeftToRight ? new CharacterHit(firstCharacterHit.FirstCharacterIndex) : firstCharacterHit;
            }

            //After
            if (distance > Size.Width)
            {
                isInside = false;

                var lastCharacterHit = FindNearestCharacterHit(_glyphClusters[_glyphClusters.Length - 1], out _);

                return IsLeftToRight ? lastCharacterHit : new CharacterHit(lastCharacterHit.FirstCharacterIndex);
            }

            //Within
            var currentX = 0.0;
            var index = 0;

            for (; index < GlyphIndices.Length - Metrics.NewlineLength; index++)
            {
                var advance = GetGlyphAdvance(index);

                if (currentX + advance >= distance)
                {
                    break;
                }

                currentX += advance;
            }

            var characterHit =
                FindNearestCharacterHit(GlyphClusters.IsEmpty ? index : GlyphClusters[index], out var width);

            var offset = GetDistanceFromCharacterHit(new CharacterHit(characterHit.FirstCharacterIndex));

            isInside = true;

            var isTrailing = distance > offset + width / 2;

            return isTrailing ? characterHit : new CharacterHit(characterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the next valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the next hit value.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> that represents the next valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength == 0)
            {
                return FindNearestCharacterHit(characterHit.FirstCharacterIndex, out _);
            }

            var nextCharacterHit =
                FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

            return new CharacterHit(nextCharacterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the previous valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the previous hit value.</param>
        /// <returns>
        /// A cref="CharacterHit"/> that represents the previous valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength != 0)
            {
                return new CharacterHit(characterHit.FirstCharacterIndex);
            }

            return characterHit.FirstCharacterIndex == Characters.Start ?
                new CharacterHit(Characters.Start) :
                FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);
        }

        /// <summary>
        /// Finds a glyph index for given character index.
        /// </summary>
        /// <param name="characterIndex">The character index.</param>
        /// <returns>
        /// The glyph index.
        /// </returns>
        public int FindGlyphIndex(int characterIndex)
        {
            if (GlyphClusters.IsEmpty)
            {
                return characterIndex;
            }

            if (IsLeftToRight)
            {
                if (characterIndex < GlyphClusters[0])
                {
                    return 0;
                }

                if (characterIndex > GlyphClusters[GlyphClusters.Length - 1])
                {
                    return _glyphClusters.Length - 1;
                }
            }
            else
            {
                if (characterIndex < GlyphClusters[GlyphClusters.Length - 1])
                {
                    return _glyphClusters.Length - 1;
                }

                if (characterIndex > GlyphClusters[0])
                {
                    return 0;
                }
            }

            var comparer = IsLeftToRight ? s_ascendingComparer : s_descendingComparer;

            var clusters = GlyphClusters.Buffer.Span;

            // Find the start of the cluster at the character index.
            var start = clusters.BinarySearch((ushort)characterIndex, comparer);

            // No cluster found.
            if (start < 0)
            {
                while (characterIndex > 0 && start < 0)
                {
                    characterIndex--;

                    start = clusters.BinarySearch((ushort)characterIndex, comparer);
                }

                if (start < 0)
                {
                    return -1;
                }
            }

            if (IsLeftToRight)
            {
                while (start > 0 && clusters[start - 1] == clusters[start])
                {
                    start--;
                }
            }
            else
            {
                while (start + 1 < clusters.Length && clusters[start + 1] == clusters[start])
                {
                    start++;
                }
            }

            return start;
        }

        /// <summary>
        /// Finds the nearest <see cref="CharacterHit"/> at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="width">The width of found cluster.</param>
        /// <returns>
        /// The nearest <see cref="CharacterHit"/>.
        /// </returns>
        public CharacterHit FindNearestCharacterHit(int index, out double width)
        {
            width = 0.0;

            var start = FindGlyphIndex(index);

            if (GlyphClusters.IsEmpty)
            {
                width = GetGlyphAdvance(index);

                return new CharacterHit(start, 1);
            }

            var cluster = GlyphClusters[start];

            var nextCluster = cluster;

            var currentIndex = start;

            while (nextCluster == cluster)
            {
                width += GetGlyphAdvance(currentIndex);

                if (IsLeftToRight)
                {
                    currentIndex++;

                    if (currentIndex == GlyphClusters.Length)
                    {
                        break;
                    }
                }
                else
                {
                    currentIndex--;

                    if (currentIndex < 0)
                    {
                        break;
                    }
                }

                nextCluster = GlyphClusters[currentIndex];
            }

            int trailingLength;

            if (nextCluster == cluster)
            {
                trailingLength = Characters.Start + Characters.Length - cluster;
            }
            else
            {
                trailingLength = nextCluster - cluster;
            }

            return new CharacterHit(cluster, trailingLength);
        }

        /// <summary>
        /// Gets a glyph's width.
        /// </summary>
        /// <param name="index">The glyph index.</param>
        /// <returns>The glyph's width.</returns>
        private double GetGlyphAdvance(int index)
        {
            if (!GlyphAdvances.IsEmpty)
            {
                return GlyphAdvances[index];
            }

            var glyph = GlyphIndices[index];

            return GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
        }

        /// <summary>
        /// Calculates the default baseline origin of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <returns>The baseline origin.</returns>
        private Point CalculateBaselineOrigin()
        {
            return new Point(0, -GlyphTypeface.Ascent * Scale);
        }

        private GlyphRunMetrics CreateGlyphRunMetrics()
        {
            var height = (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * Scale;

            var widthIncludingTrailingWhitespace = 0d;
            var width = 0d;

            var trailingWhitespaceLength = GetTrailingWhitespaceLength(out var newLineLength);

            for (var index = 0; index < _glyphIndices.Length; index++)
            {
                var advance = GetGlyphAdvance(index);

                widthIncludingTrailingWhitespace += advance;

                if (index > _glyphIndices.Length - 1 - trailingWhitespaceLength)
                {
                    continue;
                }

                width += advance;
            }

            return new GlyphRunMetrics(width, widthIncludingTrailingWhitespace, trailingWhitespaceLength, newLineLength,
                height);
        }

        private int GetTrailingWhitespaceLength(out int newLineLength)
        {
            newLineLength = 0;

            if (_characters.IsEmpty)
            {
                return 0;
            }

            var trailingWhitespaceLength = 0;

            if (_glyphClusters.IsEmpty)
            {
                for (var i = _characters.Length - 1; i >= 0;)
                {
                    var codepoint = Codepoint.ReadAt(_characters, i, out var count);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength++;
                    }

                    trailingWhitespaceLength++;

                    i -= count;
                }
            }
            else
            {
                for (var i = _glyphClusters.Length - 1; i >= 0; i--)
                {
                    var cluster = _glyphClusters[i];

                    var codepointIndex = cluster - _characters.Start;

                    var codepoint = Codepoint.ReadAt(_characters, codepointIndex, out _);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength++;
                    }

                    trailingWhitespaceLength++;
                }
            }

            return trailingWhitespaceLength;
        }

        private void Set<T>(ref T field, T value)
        {
            if (_glyphRunImpl != null)
            {
                throw new InvalidOperationException("GlyphRun can't be changed after it has been initialized.'");
            }

            _glyphRunMetrics = null;

            _baselineOrigin = null;

            field = value;
        }

        /// <summary>
        /// Initializes the <see cref="GlyphRun"/>.
        /// </summary>
        private void Initialize()
        {
            if (GlyphIndices.Length == 0)
            {
                throw new InvalidOperationException();
            }

            var glyphCount = GlyphIndices.Length;

            if (GlyphAdvances.Length > 0 && GlyphAdvances.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            if (GlyphOffsets.Length > 0 && GlyphOffsets.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            var platformRenderInterface = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            _glyphRunImpl = platformRenderInterface.CreateGlyphRun(this);
        }

        void IDisposable.Dispose()
        {
            _glyphRunImpl?.Dispose();
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                return Comparer<T>.Default.Compare(y, x);
            }
        }
    }
}
