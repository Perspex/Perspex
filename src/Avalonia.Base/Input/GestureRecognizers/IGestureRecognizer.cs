namespace Avalonia.Input.GestureRecognizers
{
    public interface IGestureRecognizer
    {
        void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions);
        void PointerPressed(PointerPressedEventArgs e);
        void PointerReleased(PointerReleasedEventArgs e);
        void PointerMoved(PointerEventArgs e);
        void PointerCaptureLost(IPointer pointer);
    }
    
    public interface IGestureRecognizerActionsDispatcher
    {
        void Capture(IPointer pointer, IGestureRecognizer recognizer);
    }
    
    public enum GestureRecognizerResult
    {
        None,
        Capture,
        ReleaseCapture
    }
}
