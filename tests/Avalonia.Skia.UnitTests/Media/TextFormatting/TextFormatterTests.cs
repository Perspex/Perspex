using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextFormatterTests
    {
        [Fact]
        public void Should_Format_TextRuns_With_Default_Style()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Single(textLine.TextRuns);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(defaultProperties.Typeface, textRun.Properties.Typeface);

                Assert.Equal(defaultProperties.ForegroundBrush, textRun.Properties.ForegroundBrush);

                Assert.Equal(text.Length, textRun.Text.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_Multiple_Buffers()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new MultiBufferTextSource(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(50, textLine.TextRange.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_TextRunStyles()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var GenericTextRunPropertiesRuns = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 3, defaultProperties),
                    new ValueSpan<TextRunProperties>(3, 3,
                        new GenericTextRunProperties(Typeface.Default, 13, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(6, 3,
                        new GenericTextRunProperties(Typeface.Default, 14, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(9, 1, defaultProperties)
                };

                var textSource = new FormattedTextSource(text.AsMemory(), defaultProperties, GenericTextRunPropertiesRuns);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(text.Length, textLine.TextRange.Length);

                for (var i = 0; i < GenericTextRunPropertiesRuns.Length; i++)
                {
                    var GenericTextRunPropertiesRun = GenericTextRunPropertiesRuns[i];

                    var textRun = textLine.TextRuns[i];

                    Assert.Equal(GenericTextRunPropertiesRun.Length, textRun.Text.Length);
                }
            }
        }

        [Theory]
        [InlineData("0123", 1)]
        [InlineData("\r\n", 1)]
        [InlineData("👍b", 2)]
        [InlineData("a👍b", 3)]
        [InlineData("a👍子b", 4)]
        public void Should_Produce_Unique_Runs(string text, int numberOfRuns)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(numberOfRuns, textLine.TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Produce_A_Single_Fallback_Run()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                const string text = "👍 👍 👍 👍";
                
                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(1, textLine.TextRuns.Count);
            } 
        }

        [Fact]
        public void Should_Split_Run_On_Script()
        {
            using (Start())
            {
                const string text = "ABCDالدولي";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(4, textLine.TextRuns[0].Text.Length);
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷", 10, 1)]
        [InlineData("01234 56789 01234 56789", 6, 4)]
        [Theory]
        public void Should_Wrap_With_Overflow(string text, int expectedCharactersPerLine, int expectedNumberOfLines)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var numberOfLines = 0;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 1,
                            new GenericTextParagraphProperties(defaultProperties, textWrap : TextWrapping.WrapWithOverflow));

                    if (text.Length - currentPosition > expectedCharactersPerLine)
                    {
                        Assert.Equal(expectedCharactersPerLine, textLine.TextRange.Length);
                    }

                    currentPosition += textLine.TextRange.Length;

                    numberOfLines++;
                }

                Assert.Equal(expectedNumberOfLines, numberOfLines);
            }
        }

        [InlineData("Whether to turn off HTTPS. This option only applies if Individual, " +
                    "IndividualB2C, SingleOrg, or MultiOrg aren't used for &#8209;&#8209;auth."
            , "Noto Sans", 40)]
        [InlineData("01234 56789 01234 56789", "Noto Mono", 7)]
        [Theory]
        public void Should_Wrap(string text, string familyName, int numberOfCharactersPerLine)
        {
            using (Start())
            {
                var lineBreaker = new LineBreakEnumerator(text.AsMemory());

                var expected = new List<int>();

                while (lineBreaker.MoveNext())
                {
                    expected.Add(lineBreaker.Current.PositionWrap - 1);
                }

                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#" +
                                            familyName);

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var glyph = typeface.GlyphTypeface.GetGlyph('a');

                var advance = typeface.GlyphTypeface.GetGlyphAdvance(glyph) *
                              (12.0 / typeface.GlyphTypeface.DesignEmHeight);

                var paragraphWidth = advance * numberOfCharactersPerLine;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, paragraphWidth,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap));

                    Assert.True(expected.Contains(textLine.TextRange.End));

                    var index = expected.IndexOf(textLine.TextRange.End);

                    for (var i = 0; i <= index; i++)
                    {
                        expected.RemoveAt(0);
                    }

                    currentPosition += textLine.TextRange.Length;
                }
            }
        }

        [Fact]
        public void Should_Produce_Fixed_Height_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties, lineHeight: 50));

                Assert.Equal(50, textLine.Height);
            }
        }

        [Fact]
        public void Should_Not_Produce_TextLine_Wider_Than_ParagraphWidth()
        {
            using (Start())
            {
                const string text =
                    "Multiline TextBlock with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. " +
                    "Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. " +
                    "Vivamus pretium ornare est.";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 200, paragraphProperties);

                    Assert.True(textLine.Width <= 200);

                    textSourceIndex += textLine.TextRange.Length;
                }
            }
        }

        [Fact]
        public void Wrap_Should_Not_Produce_Empty_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 3, paragraphProperties);

                    Assert.NotEqual(0, textLine.TextRange.Length);

                    textSourceIndex += textLine.TextRange.Length;
                }

                Assert.Equal(text.Length, textSourceIndex);
            }
        }

        [InlineData("Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor",  
            new []{ "Lorem ipsum ", "dolor sit amet, ", "consectetur ", "adipisicing elit, ", "sed do eiusmod "})]

        [Theory]
        public void Should_Produce_Wrapped_And_Trimmed_Lines(string text, string[] expectedLines)
        {
            using (Start())
            {
                var typeface = new Typeface("Verdana");

                var defaultProperties = new GenericTextRunProperties(typeface, 32, foregroundBrush: Brushes.Black);

                var styleSpans = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 5,
                        new GenericTextRunProperties(typeface, 48)),
                    new ValueSpan<TextRunProperties>(6, 11,
                        new GenericTextRunProperties(new Typeface("Verdana", weight: FontWeight.Bold), 32)),
                    new ValueSpan<TextRunProperties>(28, 28,
                        new GenericTextRunProperties(new Typeface("Verdana", FontStyle.Italic),32))
                };
                
                var textSource = new FormattedTextSource(text.AsMemory(), defaultProperties, styleSpans);

                var formatter = new TextFormatterImpl();

                var currentPosition = 0;

                var currentHeight = 0d;

                var currentLineIndex = 0;

                while (currentPosition < text.Length && currentLineIndex < expectedLines.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 300,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.WrapWithOverflow));

                    currentPosition += textLine.TextRange.Length;

                    if (textLine.Width > 300 || currentHeight + textLine.Height > 240)
                    {
                        textLine = textLine.Collapse(new TextTrailingWordEllipsis(300, defaultProperties));
                    }
                    
                    currentHeight += textLine.Height;

                    var currentText = text.Substring(textLine.TextRange.Start, textLine.TextRange.Length);
                    
                    Assert.Equal(expectedLines[currentLineIndex], currentText);

                    currentLineIndex++;
                }
                
                Assert.Equal(expectedLines.Length,currentLineIndex);
            }
        }

        [InlineData(TextAlignment.Left)]
        [InlineData(TextAlignment.Center)]
        [InlineData(TextAlignment.Right)]
        [Theory]
        public void Should_Align_TextLine(TextAlignment textAlignment)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textAlignment);
                
                var textSource = new SingleBufferTextSource("0123456789", defaultProperties);
                var formatter = new TextFormatterImpl();
                
                var textLine =
                    formatter.FormatLine(textSource, 0, 100, paragraphProperties);

                var expectedOffset = TextLine.GetParagraphOffsetX(textLine.Width, 100, textAlignment);
                
                Assert.Equal(expectedOffset, textLine.Start);
            }
        }

        [Fact]
        public void Should_FormatLine_With_Emergency_Breaks()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);
                
                var textSource = new SingleBufferTextSource("0123456789_0123456789_0123456789_0123456789", defaultProperties);
                var formatter = new TextFormatterImpl();
                
                var textLine =
                    formatter.FormatLine(textSource, 0, 33, paragraphProperties);
                
                Assert.NotNull(textLine.TextLineBreak?.RemainingCharacters);
            }
        }
        
        public static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    textShaperImpl: new TextShaperImpl()));

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(new FontManager(new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
