using System;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    /// <summary>
    ///     This class is intended for use when the Unicode spec changes. Otherwise the containing tests are redundant.
    ///     To update the <c>GraphemeBreak.trie</c> run the <see cref="Should_Generate_Trie"/> test.
    /// </summary>
    public class GraphemeBreakClassTrieGeneratorTests
    {
        [Theory(Skip = "Only run when we update the trie.")]
        [ClassData(typeof(GraphemeBreakTestDataGenerator))]
        public void Should_Enumerate(string text, int expectedLength)
        {
            var textMemory = text.AsMemory();

            var enumerator = new GraphemeEnumerator(textMemory);

            Assert.True(enumerator.MoveNext());

            Assert.Equal(expectedLength, enumerator.Current.Text.Length);
        }

        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Enumerate_Other()
        {
            const string text = "ABCDEFGHIJ";

            var textMemory = text.AsMemory();

            var enumerator = new GraphemeEnumerator(textMemory);

            var count = 0;

            while (enumerator.MoveNext())
            {
                Assert.Equal(1, enumerator.Current.Text.Length);

                count++;
            }

            Assert.Equal(10, count);
        }

        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Generate_Trie()
        {
            GraphemeBreakClassTrieGenerator.Execute();
        }

        private class GraphemeBreakTestDataGenerator : TestDataGenerator
        {
            public GraphemeBreakTestDataGenerator() 
                : base("auxiliary/GraphemeBreakTest.txt")
            {
            }
        }
    }
}
