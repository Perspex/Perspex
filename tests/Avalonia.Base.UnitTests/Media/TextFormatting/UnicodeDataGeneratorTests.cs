using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class UnicodeDataGeneratorTests
    {
        /// <summary>
        ///     This test is used to generate all Unicode related types.
        ///     We only need to run this when the Unicode spec changes.
        /// </summary>
        [Fact(Skip = "Only run when the Unicode spec changes.")]
        public void Should_Generate_Data()
        {
            UnicodeDataGenerator.Execute();
        }
    }
}
