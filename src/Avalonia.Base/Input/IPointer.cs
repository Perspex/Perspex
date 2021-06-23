namespace Avalonia.Input
{
    public interface IPointer
    {
        int Id { get; }
        void Capture(IInputElement? control);
        IInputElement? Captured { get; }
        PointerType Type { get; }
        bool IsPrimary { get; }
        
    }

    public enum PointerType
    {
        Mouse,
        Touch
    }
}
