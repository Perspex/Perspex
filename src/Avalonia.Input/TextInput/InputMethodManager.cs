using System;
using Avalonia.VisualTree;

namespace Avalonia.Input.TextInput
{
    internal class TextInputMethodManager
    {
        private ITextInputMethodImpl? _im;
        private IInputElement? _focusedElement;
        private ITextInputMethodClient? _client;
        private readonly TransformTrackingHelper _transformTracker = new TransformTrackingHelper();

        public TextInputMethodManager() => _transformTracker.MatrixChanged += UpdateCursorRect;

        private ITextInputMethodClient? Client
        {
            get => _client;
            set
            {
                if(_client == value)
                    return;
                if (_client != null)
                {
                    _client.CursorRectangleChanged -= OnCursorRectangleChanged;
                    _client.TextViewVisualChanged -= OnTextViewVisualChanged;
                }

                _client = value;
                
                if (_client != null)
                {
                    _client.CursorRectangleChanged += OnCursorRectangleChanged;
                    _client.TextViewVisualChanged += OnTextViewVisualChanged;
                    var optionsQuery = new TextInputOptionsQueryEventArgs
                    {
                        RoutedEvent = InputElement.TextInputOptionsQueryEvent
                    };
                    _focusedElement?.RaiseEvent(optionsQuery);
                    _im?.Reset();
                    _im?.SetOptions(optionsQuery);
                    _transformTracker?.SetVisual(_client?.TextViewVisual);
                    UpdateCursorRect();
                    _im?.SetActive(true);
                }
                else
                {
                    _im?.SetActive(false);
                    _transformTracker.SetVisual(null);
                }
            }
        }

        private void OnTextViewVisualChanged(object sender, EventArgs e) 
            => _transformTracker.SetVisual(_client?.TextViewVisual);

        private void UpdateCursorRect()
        {
            if (_im == null || _client == null)
                return;
            var visualWithFocus = _focusedElement?.GetClosestVisual();
            if (visualWithFocus?.VisualRoot == null)
                return;
            var transform = visualWithFocus.TransformToVisual(visualWithFocus.VisualRoot);
            if (transform == null)
                _im.SetCursorRect(default);
            else
                _im.SetCursorRect(_client.CursorRectangle.TransformToAABB(transform.Value));
        }

        private void OnCursorRectangleChanged(object sender, EventArgs e)
        {
            if (sender == _client)
                UpdateCursorRect();
        }
        
        public void SetFocusedElement(IInputElement? element)
        {
            if(_focusedElement == element)
                return;
            _focusedElement = element;
            
            var inputMethod = (element?.GetClosestVisual()?.VisualRoot as ITextInputMethodRoot)?.InputMethod;
            if(_im != inputMethod)
                _im?.SetActive(false);

            _im = inputMethod;
            
            if (_focusedElement == null || _im == null)
            {
                Client = null;
                _im?.SetActive(false);
                return;
            }

            var clientQuery = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            
            _focusedElement.RaiseEvent(clientQuery);
            Client = clientQuery.Client;
        }
    }
}
