using System;
using System.Threading.Tasks;
using Avalonia.FreeDesktop;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    partial class X11Window
    {

        class XimInputMethod : ITextInputMethodImpl, IX11InputMethodControl
        {
            private readonly X11Window _parent;
            private bool _controlActive, _windowActive, _imeActive;
            private Rect? _queuedCursorRect;

            public XimInputMethod(X11Window parent)
            {
                _parent = parent;
            }
            
            public void SetCursorRect(Rect rect)
            {
                var needEnqueue = _queuedCursorRect == null;
                _queuedCursorRect = rect;
                if(needEnqueue)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if(_queuedCursorRect == null)
                            return;
                        var rc = _queuedCursorRect.Value;
                        _queuedCursorRect = null;

                        if (_parent._xic == IntPtr.Zero)
                            return;

                        rect *= _parent._scaling;
                        
                        var pt = new XPoint
                        {
                            X = (short)Math.Min(Math.Max(rect.X, short.MinValue), short.MaxValue),
                            Y = (short)Math.Min(Math.Max(rect.Y + rect.Height, short.MinValue), short.MaxValue)
                        };

                        using var spotLoc = new Utf8Buffer(XNames.XNSpotLocation);
                        var list = XVaCreateNestedList(0, spotLoc, ref pt, IntPtr.Zero);
                        XSetICValues(_parent._xic, XNames.XNPreeditAttributes, list, IntPtr.Zero);
                        XFree(list);
                    }, DispatcherPriority.Background);
            }
            
            public void SetWindowActive(bool active)
            {
                _windowActive = active;
                UpdateActive();
            }

            public void SetActive(bool active)
            {
                _controlActive = active;
                UpdateActive();
            }

            private void UpdateActive()
            {
                var active = _windowActive && _controlActive;
                if(_parent._xic == IntPtr.Zero)
                    return;
                if (active != _imeActive)
                {
                    _imeActive = active;
                    if (active)
                    {
                        Reset();
                        XSetICFocus(_parent._xic);
                    }
                    else
                        XUnsetICFocus(_parent._xic);
                }
            }
            
            public void UpdateWindowInfo(PixelPoint position, double scaling)
            {
                // No-op
            }
            
            public void SetOptions(TextInputOptionsQueryEventArgs options)
            {
                // No-op
            }

            public void Reset()
            {
                if(_parent._xic == IntPtr.Zero)
                    return;
                
                var data = XmbResetIC(_parent._xic);
                if (data != IntPtr.Zero)
                    XFree(data);
            }

            public void Dispose()
            {
                // No-op
            }

            public bool IsEnabled => false;

            public ValueTask<bool> HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode) =>
                new ValueTask<bool>(false);

            public event Action<string> Commit { add { } remove { } }
            public event Action<X11InputMethodForwardedKey> ForwardKey { add { } remove { } }

        }
        
    }
}
