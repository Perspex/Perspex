using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    public enum TextPatternRangeEndpoint
    {
        Start = 0,
        End = 1,
    }

    public enum TextUnit
    {
        Character = 0,
        Format = 1,
        Word = 2,
        Line = 3,
        Paragraph = 4,
        Page = 5,
        Document = 6,
    }

    [ComVisible(true)]
    [Guid("5347ad7b-c355-46f8-aff5-909033582f63")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITextRangeProvider

    {
        ITextRangeProvider Clone();
        [return: MarshalAs(UnmanagedType.Bool)]
        bool Compare(ITextRangeProvider range);
        int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void ExpandToEnclosingUnit(TextUnit unit);
        ITextRangeProvider FindAttribute(int attribute, object value, [MarshalAs(UnmanagedType.Bool)] bool backward);
        ITextRangeProvider FindText(string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);
        object GetAttributeValue(int attribute);
        double [] GetBoundingRectangles();
        IRawElementProviderSimple GetEnclosingElement();
        string GetText(int maxLength);
        int Move(TextUnit unit, int count);
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);
        void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void Select();
        void AddToSelection();
        void RemoveFromSelection();
        void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
        IRawElementProviderSimple[] GetChildren();
    }
}
