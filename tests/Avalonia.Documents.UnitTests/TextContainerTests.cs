using System.Windows.Documents;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Documents.UnitTests
{
    public class TextContainerTests
    {
        [Fact]
        public void Should_Create_TextContainer()
        {
            var textContainer = new TextContainer(null, false);
        }

        [Fact]
        public void Should_Add_Text()
        {
            var textContainer = new TextContainer(null, false);

            textContainer.BeginChange();

            var endPosition = textContainer.End;

            var implicitRun = Inline.CreateImplicitRun();

            textContainer.InsertElementInternal(endPosition, endPosition, implicitRun);

            const string text = "SampleText";

            implicitRun.Text = text;

            textContainer.EndChange();

            const int startEdgeOffset = 1;

            var position = textContainer.CreateStaticPointerAtOffset(0);

            Assert.Equal(TextPointerContext.ElementStart, position.GetPointerContext(LogicalDirection.Forward));

            position = position.CreatePointer(startEdgeOffset);

            Assert.Equal(TextPointerContext.Text, position.GetPointerContext(LogicalDirection.Forward));

            position = position.CreatePointer(text.Length);

            Assert.Equal(TextPointerContext.ElementEnd, position.GetPointerContext(LogicalDirection.Forward));
        }
    }
}
