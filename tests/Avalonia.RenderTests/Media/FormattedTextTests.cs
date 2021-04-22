using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Media.TextFormatting;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else

using Avalonia.Direct2D1.RenderTests;

namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class FormattedTextImplTests : TestBase
    {
        private const string FontName = "Courier New";
        private const double FontSize = 12;
        private const double MediumFontSize = 18;
        private const double BigFontSize = 32;
        private const double FontSizeHeight = 13.594;//real value 13.59375
        private const string stringword = "word";
        private const string stringmiddle = "The quick brown fox jumps over the lazy dog";
        private const string stringmiddle2lines = "The quick brown fox\njumps over the lazy dog";
        private const string stringmiddle3lines = "01234567\n\n0123456789";
        private const string stringmiddlenewlines = "012345678\r 1234567\r\n 12345678\n0123456789";

        private const string stringlong =
"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus magna. Cras in mi at felis " +
"aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. Donec eleifend, libero" +
" at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus " +
"pretium ornare est.";

        public FormattedTextImplTests()
            : base(@"Media\FormattedText")
        {
        }

        private TextLayout Create(string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight,
            TextWrapping wrapping,
            double widthConstraint)
        {
            var typeface = new Typeface(fontFamily, fontStyle, fontWeight);

            var formattedText = new TextLayout(text, typeface, fontSize, null, textAlignment, wrapping,
                maxWidth: widthConstraint == -1 ? double.PositiveInfinity : widthConstraint);

            return formattedText;
        }

        private TextLayout Create(string text, double fontSize)
        {
            return Create(text, FontName, fontSize,
                FontStyle.Normal, TextAlignment.Left,
                FontWeight.Normal, TextWrapping.NoWrap,
                -1);
        }

        private TextLayout Create(string text, double fontSize, TextAlignment alignment, double widthConstraint)
        {
            return Create(text, FontName, fontSize,
                FontStyle.Normal, alignment,
                FontWeight.Normal, TextWrapping.NoWrap,
                widthConstraint);
        }

        private TextLayout Create(string text, double fontSize, TextWrapping wrap, double widthConstraint)
        {
            return Create(text, FontName, fontSize,
                FontStyle.Normal, TextAlignment.Left,
                FontWeight.Normal, wrap,
                widthConstraint);
        }


        [Theory]
        [InlineData("", FontSize, 0, FontSizeHeight)]
        [InlineData("x", FontSize, 7.20, FontSizeHeight)]
        [InlineData(stringword, FontSize, 28.80, FontSizeHeight)]
        [InlineData(stringmiddle, FontSize, 309.65, FontSizeHeight)]
        [InlineData(stringmiddle, MediumFontSize, 464.48, 20.391)]
        [InlineData(stringmiddle, BigFontSize, 825.73, 36.25)]
        [InlineData(stringmiddle2lines, FontSize, 165.63, 2 * FontSizeHeight)]
        [InlineData(stringmiddle2lines, MediumFontSize, 248.44, 2 * 20.391)]
        [InlineData(stringmiddle2lines, BigFontSize, 441.67, 2 * 36.25)]
        [InlineData(stringlong, FontSize, 2160.35, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, FontSize, 72.01, 4 * FontSizeHeight)]
        public void Should_Measure_String_Correctly(string input, double fontSize, double expWidth, double expHeight)
        {
            var fmt = Create(input, fontSize);

            Assert.Equal(expWidth, fmt.Size.Width, 2);
            Assert.Equal(expHeight, fmt.Size.Height, 2);
        }

        [Theory]
        [InlineData("", 1, -1, TextWrapping.NoWrap)]
        [InlineData("x", 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringword, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle, 3, 150, TextWrapping.Wrap)]
        [InlineData(stringmiddle2lines, 2, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle2lines, 3, 150, TextWrapping.Wrap)]
        [InlineData(stringlong, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringlong, 18, 150, TextWrapping.Wrap)]
        [InlineData(stringmiddlenewlines, 4, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddlenewlines, 4, 150, TextWrapping.Wrap)]
        public void Should_Break_Lines_String_Correctly(string input,
                                                            int linesCount,
                                                            double widthConstraint,
                                                            TextWrapping wrap)
        {
            var fmt = Create(input, FontSize, wrap, widthConstraint);
            var constrained = fmt;

            var lines = constrained.TextLines.ToArray();
            Assert.Equal(linesCount, lines.Count());
        }

        [Theory]
        [InlineData("x", 0, 0, true, false, 0)]
        [InlineData(stringword, -1, -1, false, false, 0)]
        [InlineData(stringword, 25, 13, true, false, 3)]
        [InlineData(stringword, 28.70, 13.5, true, true, 4)]
        [InlineData(stringword, 30, 13, false, true, 4)]
        [InlineData(stringword + "\r\n", 30, 13, false, false, 4)]
        [InlineData(stringword + "\r\nnext", 30, 13, false, false, 4)]
        [InlineData(stringword, 300, 13, false, true, 4)]
        [InlineData(stringword + "\r\n", 300, 13, false, false, 4)]
        [InlineData(stringword + "\r\nnext", 300, 13, false, false, 4)]
        [InlineData(stringword, 300, 300, false, true, 4)]
        //TODO: Direct2D implementation return textposition 6
        //but the text is 6 length, can't find the logic for me it should be 5
        //[InlineData(stringword + "\r\n", 300, 300, false, false, 6)]
        [InlineData(stringword + "\r\nnext", 300, 300, false, true, 10)]
        [InlineData(stringword + "\r\nnext", 300, 25, false, true, 10)]
        [InlineData(stringword, 28, 15, false, true, 4)]
        [InlineData(stringword, 30, 15, false, true, 4)]
        [InlineData(stringmiddle3lines, 30, 15, false, false, 9)]
        [InlineData(stringmiddle3lines, 500, 13, false, false, 8)]
        [InlineData(stringmiddle3lines, 30, 25, false, false, 9)]
        [InlineData(stringmiddle3lines, -1, 30, false, false, 10)]
        public void Should_HitTestPoint_Correctly(string input,
            double x, double y,
            bool isInside, bool isTrailing, int pos)
        {
            var fmt = Create(input, FontSize);
            var htRes = fmt.HitTestPoint(new Point(x, y));

            Assert.Equal(pos, htRes.TextPosition);
            Assert.Equal(isInside, htRes.IsInside);
            Assert.Equal(isTrailing, htRes.IsTrailing);
        }

        [Theory]
        [InlineData("", 0, 0, 0, 0, FontSizeHeight)]
        [InlineData("x", 0, 0, 0, 7.20, FontSizeHeight)]
        [InlineData("x", -1, 7.20, 0, 0, FontSizeHeight)]
        [InlineData(stringword, 3, 21.60, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 4, 21.60 + 7.20, 0, 0, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 10, 0, FontSizeHeight, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 15, 36.01, FontSizeHeight, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 20, 0, 2 * FontSizeHeight, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, -1, 72.01, 3 * FontSizeHeight, 0, FontSizeHeight)]
        public void Should_HitTestPosition_Correctly(string input,
                    int index, double x, double y, double width, double height)
        {
            var fmt = Create(input, FontSize);
            var r = fmt.HitTestTextPosition(index);

            Assert.Equal(x, r.X, 2);
            Assert.Equal(y, r.Y, 2);
            Assert.Equal(width, r.Width, 2);
            Assert.Equal(height, r.Height, 2);
        }

        [Theory]
        [InlineData("x", 0, 200, 200 - 7.20, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 0, 200, 171.20, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 3, 200, 200 - 7.20, 0, 7.20, FontSizeHeight)]
        public void Should_HitTestPosition_RigthAlign_Correctly(
                                                    string input, int index, double widthConstraint,
                                                    double x, double y, double width, double height)
        {
            //parse expected
            var fmt = Create(input, FontSize, TextAlignment.Right, widthConstraint);
            var constrained = fmt;
            var r = constrained.HitTestTextPosition(index);

            Assert.Equal(x, r.X, 2);
            Assert.Equal(y, r.Y, 2);
            Assert.Equal(width, r.Width, 2);
            Assert.Equal(height, r.Height, 2);
        }

        [Theory]
        [InlineData("x", 0, 200, 100 - 7.20 / 2, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 0, 200, 85.6, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 3, 200, 100 + 7.20, 0, 7.20, FontSizeHeight)]
        public void Should_HitTestPosition_CenterAlign_Correctly(
                                                    string input, int index, double widthConstraint,
                                                    double x, double y, double width, double height)
        {
            //parse expected
            var fmt = Create(input, FontSize, TextAlignment.Center, widthConstraint);
            var constrained = fmt;
            var r = constrained.HitTestTextPosition(index);

            Assert.Equal(x, r.X, 2);
            Assert.Equal(y, r.Y, 2);
            Assert.Equal(width, r.Width, 2);
            Assert.Equal(height, r.Height, 2);
        }

        [Theory]
        [InlineData("x", 0, 1, "0,0,7.20,13.59")]
        [InlineData(stringword, 0, 4, "0,0,28.80,13.59")]
        [InlineData(stringmiddlenewlines, 10, 10, "0,13.59,57.61,13.59")]
        [InlineData(stringmiddlenewlines, 10, 20, "0,13.59,57.61,13.59;0,27.19,64.81,13.59")]
        [InlineData(stringmiddlenewlines, 10, 15, "0,13.59,57.61,13.59;0,27.19,36.01,13.59")]
        [InlineData(stringmiddlenewlines, 15, 15, "36.01,13.59,21.60,13.59;0,27.19,64.81,13.59")]
        public void Should_HitTestRange_Correctly(string input,
                            int index, int length,
                            string expectedRects)
        {
            //parse expected result
            var rects = expectedRects.Split(';').Select(s =>
            {
                double[] v = s.Split(',')
                .Select(sd => double.Parse(sd, CultureInfo.InvariantCulture)).ToArray();
                return new Rect(v[0], v[1], v[2], v[3]);
            }).ToArray();

            var fmt = Create(input, FontSize);
            var htRes = fmt.HitTestTextRange(index, length).ToArray();

            Assert.Equal(rects.Length, htRes.Length);

            for (int i = 0; i < rects.Length; i++)
            {
                var exr = rects[i];
                var r = htRes[i];

                Assert.Equal(exr.X, r.X, 2);
                Assert.Equal(exr.Y, r.Y, 2);
                Assert.Equal(exr.Width, r.Width, 2);
                Assert.Equal(exr.Height, r.Height, 2);
            }
        }
    }
}
