using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class LineBreakEnumeratorTests
    {
        private readonly ITestOutputHelper _outputHelper;
        
        public LineBreakEnumeratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public void BasicLatinTest()
        {
            var lineBreaker = new LineBreakEnumerator("Hello World\r\nThis is a test.".AsMemory());

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(6, lineBreaker.Current.PositionWrap);
            Assert.False(lineBreaker.Current.Required);

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(13, lineBreaker.Current.PositionWrap);
            Assert.True(lineBreaker.Current.Required);

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(18, lineBreaker.Current.PositionWrap);
            Assert.False(lineBreaker.Current.Required);

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(21, lineBreaker.Current.PositionWrap);
            Assert.False(lineBreaker.Current.Required);

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(23, lineBreaker.Current.PositionWrap);
            Assert.False(lineBreaker.Current.Required);

            Assert.True(lineBreaker.MoveNext());
            Assert.Equal(28, lineBreaker.Current.PositionWrap);
            Assert.False(lineBreaker.Current.Required);

            Assert.False(lineBreaker.MoveNext());
        }


        [Fact]
        public void ForwardTextWithOuterWhitespace()
        {
            var lineBreaker = new LineBreakEnumerator(" Apples Pears Bananas   ".AsMemory());
            var positionsF = GetBreaks(lineBreaker);
            Assert.Equal(1, positionsF[0].PositionWrap);
            Assert.Equal(0, positionsF[0].PositionMeasure);
            Assert.Equal(8, positionsF[1].PositionWrap);
            Assert.Equal(7, positionsF[1].PositionMeasure);
            Assert.Equal(14, positionsF[2].PositionWrap);
            Assert.Equal(13, positionsF[2].PositionMeasure);
            Assert.Equal(24, positionsF[3].PositionWrap);
            Assert.Equal(21, positionsF[3].PositionMeasure);
        }

        private static List<LineBreak> GetBreaks(LineBreakEnumerator lineBreaker)
        {
            var breaks = new List<LineBreak>();

            while (lineBreaker.MoveNext())
            {
                breaks.Add(lineBreaker.Current);
            }

            return breaks;
        }

        [Fact]
        public void ForwardTest()
        {
            var lineBreaker = new LineBreakEnumerator("Apples Pears Bananas".AsMemory());

            var positionsF = GetBreaks(lineBreaker);
            Assert.Equal(7, positionsF[0].PositionWrap);
            Assert.Equal(6, positionsF[0].PositionMeasure);
            Assert.Equal(13, positionsF[1].PositionWrap);
            Assert.Equal(12, positionsF[1].PositionMeasure);
            Assert.Equal(20, positionsF[2].PositionWrap);
            Assert.Equal(20, positionsF[2].PositionMeasure);
        }

        [Theory(Skip = "Only run when the Unicode spec changes.")]
        [ClassData(typeof(LineBreakTestDataGenerator))]
        public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints)
        {
            var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

            var lineBreaker = new LineBreakEnumerator(text.AsMemory());

            var foundBreaks = new List<int>();
            
            while (lineBreaker.MoveNext())
            {
                foundBreaks.Add(lineBreaker.Current.PositionWrap);
            }
            
            // Check the same
            var pass = true;

            if (foundBreaks.Count != breakPoints.Length)
            {
                pass = false;
            }
            else
            {
                for (var i = 0; i < foundBreaks.Count; i++)
                {
                    if (foundBreaks[i] != breakPoints[i])
                    {
                        pass = false;
                    }
                }
            }

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed test on line {lineNumber}");
                _outputHelper.WriteLine("");
                _outputHelper.WriteLine($"    Code Points: {string.Join(" ", codePoints)}");
                _outputHelper.WriteLine($"Expected Breaks: {string.Join(" ", breakPoints)}");
                _outputHelper.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                _outputHelper.WriteLine($"           Text: {text}");
                _outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint(x).LineBreakClass))}");
                _outputHelper.WriteLine("");
            }
            
            Assert.True(pass);
        }

        private class LineBreakTestDataGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _testData;

            public LineBreakTestDataGenerator()
            {
                _testData = GenerateTestData();
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                return _testData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static List<object[]> GenerateTestData()
            {
                // Process each line
                var tests = new List<object[]>();

                // Read the test file
                var url = Path.Combine(UnicodeDataGenerator.Ucd, "auxiliary/LineBreakTest.txt");

                using (var client = new HttpClient())
                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        return tests;
                    }

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        var lineNumber = 1;

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (line is null)
                            {
                                break;
                            }

                            // Get the line, remove comments
                            line = line.Split('#')[0].Trim();

                            // Ignore blank/comment only lines
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                lineNumber++;
                                continue;
                            }

                            var codePoints = new List<int>();
                            var breakPoints = new List<int>();

                            // Parse the test
                            var p = 0;

                            while (p < line.Length)
                            {
                                // Ignore white space
                                if (char.IsWhiteSpace(line[p]))
                                {
                                    p++;
                                    continue;
                                }

                                if (line[p] == '×')
                                {
                                    p++;
                                    continue;
                                }

                                if (line[p] == '÷')
                                {
                                    breakPoints.Add(codePoints.Select(x=> x > ushort.MaxValue ? 2 : 1).Sum());
                                    p++;
                                    continue;
                                }

                                var codePointPos = p;

                                while (p < line.Length && IsHexDigit(line[p]))
                                {
                                    p++;
                                }

                                var codePointStr = line.Substring(codePointPos, p - codePointPos);
                                var codePoint = Convert.ToInt32(codePointStr, 16);
                                codePoints.Add(codePoint);
                            }

                            tests.Add(new object[] { lineNumber, codePoints.ToArray(), breakPoints.ToArray() });

                            lineNumber++;
                        }
                    }
                }

                return tests;
            }

            private static bool IsHexDigit(char ch)
            {
                return char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
            }
        }
    }
}
