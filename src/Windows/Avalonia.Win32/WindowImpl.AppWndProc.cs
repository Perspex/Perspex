using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Win32.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public partial class WindowImpl
    {
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
            Justification = "Using Win32 naming for consistency.")]
        protected virtual unsafe IntPtr AppWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const double wheelDelta = 120.0;
            uint timestamp = unchecked((uint)GetMessageTime());

            RawInputEventArgs e = null;
            var shouldTakeFocus = false;

            switch ((WindowsMessage)msg)
            {
                case WindowsMessage.WM_ACTIVATE:
                    {
                        var wa = (WindowActivate)(ToInt32(wParam) & 0xffff);

                        switch (wa)
                        {
                            case WindowActivate.WA_ACTIVE:
                            case WindowActivate.WA_CLICKACTIVE:
                                {
                                    Activated?.Invoke();
                                    break;
                                }

                            case WindowActivate.WA_INACTIVE:
                                {
                                    Deactivated?.Invoke();
                                    break;
                                }
                        }

                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_NCCALCSIZE:
                    {
                        if (ToInt32(wParam) == 1 && !HasFullDecorations || _isClientAreaExtended)
                        {
                            return IntPtr.Zero;
                        }

                        break;
                    }

                case WindowsMessage.WM_CLOSE:
                    {
                        bool? preventClosing = Closing?.Invoke();
                        if (preventClosing == true)
                        {
                            return IntPtr.Zero;
                        }

                        BeforeCloseCleanup(false);

                        // Used to distinguish between programmatic and regular close requests.
                        _isCloseRequested = true;

                        break;
                    }

                case WindowsMessage.WM_DESTROY:
                    {
                        //Window doesn't exist anymore
                        _hwnd = IntPtr.Zero;
                        //Remove root reference to this class, so unmanaged delegate can be collected
                        s_instances.Remove(this);
                        Closed?.Invoke();

                        _mouseDevice.Dispose();
                        _touchDevice?.Dispose();
                        //Free other resources
                        Dispose();
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_DPICHANGED:
                    {
                        var dpi = ToInt32(wParam) & 0xffff;
                        var newDisplayRect = Marshal.PtrToStructure<RECT>(lParam);
                        _scaling = dpi / 96.0;
                        ScalingChanged?.Invoke(_scaling);
                        SetWindowPos(hWnd,
                            IntPtr.Zero,
                            newDisplayRect.left,
                            newDisplayRect.top,
                            newDisplayRect.right - newDisplayRect.left,
                            newDisplayRect.bottom - newDisplayRect.top,
                            SetWindowPosFlags.SWP_NOZORDER |
                            SetWindowPosFlags.SWP_NOACTIVATE);
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_KEYDOWN:
                case WindowsMessage.WM_SYSKEYDOWN:
                    {
                        e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            _owner,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey(ToInt32(wParam), ToInt32(lParam)),
                            WindowsKeyboardDevice.Instance.Modifiers);
                        break;
                    }

                case WindowsMessage.WM_MENUCHAR:
                    {
                        // mute the system beep
                        return (IntPtr)((int)MenuCharParam.MNC_CLOSE << 16);
                    }

                case WindowsMessage.WM_KEYUP:
                case WindowsMessage.WM_SYSKEYUP:
                    {
                        e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            _owner,
                            RawKeyEventType.KeyUp,
                            KeyInterop.KeyFromVirtualKey(ToInt32(wParam), ToInt32(lParam)),
                            WindowsKeyboardDevice.Instance.Modifiers);
                        break;
                    }
                case WindowsMessage.WM_CHAR:
                    {
                        // Ignore control chars
                        if (ToInt32(wParam) >= 32)
                        {
                            e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _owner,
                                new string((char)ToInt32(wParam), 1));
                        }

                        break;
                    }

                case WindowsMessage.WM_LBUTTONDOWN:
                case WindowsMessage.WM_RBUTTONDOWN:
                case WindowsMessage.WM_MBUTTONDOWN:
                case WindowsMessage.WM_XBUTTONDOWN:
                    {
                        shouldTakeFocus = ShouldTakeFocusOnClick;
                        if (ShouldIgnoreTouchEmulatedMessage())
                        {
                            break;
                        }

                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            (WindowsMessage)msg switch
                            {
                                WindowsMessage.WM_LBUTTONDOWN => RawPointerEventType.LeftButtonDown,
                                WindowsMessage.WM_RBUTTONDOWN => RawPointerEventType.RightButtonDown,
                                WindowsMessage.WM_MBUTTONDOWN => RawPointerEventType.MiddleButtonDown,
                                WindowsMessage.WM_XBUTTONDOWN =>
                                HighWord(ToInt32(wParam)) == 1 ?
                                    RawPointerEventType.XButton1Down :
                                    RawPointerEventType.XButton2Down,
                                _ => throw new AvaloniaInternalException(
                                    "No raw event defined for button down message.")
                            },
                            DipFromLParam(lParam), GetMouseModifiers(wParam));
                        ;
                        break;
                    }

                case WindowsMessage.WM_LBUTTONUP:
                case WindowsMessage.WM_RBUTTONUP:
                case WindowsMessage.WM_MBUTTONUP:
                case WindowsMessage.WM_XBUTTONUP:
                    {
                        shouldTakeFocus = ShouldTakeFocusOnClick;
                        if (ShouldIgnoreTouchEmulatedMessage())
                        {
                            break;
                        }

                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            (WindowsMessage)msg switch
                            {
                                WindowsMessage.WM_LBUTTONUP => RawPointerEventType.LeftButtonUp,
                                WindowsMessage.WM_RBUTTONUP => RawPointerEventType.RightButtonUp,
                                WindowsMessage.WM_MBUTTONUP => RawPointerEventType.MiddleButtonUp,
                                WindowsMessage.WM_XBUTTONUP =>
                                HighWord(ToInt32(wParam)) == 1 ?
                                    RawPointerEventType.XButton1Up :
                                    RawPointerEventType.XButton2Up,
                                _ => throw new AvaloniaInternalException(
                                    "No raw event defined for button up message.")
                            },
                            DipFromLParam(lParam), GetMouseModifiers(wParam));
                        break;
                    }
                // Mouse capture is lost
                case WindowsMessage.WM_CANCELMODE:
                    _mouseDevice.Capture(null);
                    break;

                case WindowsMessage.WM_MOUSEMOVE:
                    {
                        if (ShouldIgnoreTouchEmulatedMessage())
                        {
                            break;
                        }

                        if (!_trackingMouse)
                        {
                            var tm = new TRACKMOUSEEVENT
                            {
                                cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
                                dwFlags = 2,
                                hwndTrack = _hwnd,
                                dwHoverTime = 0,
                            };

                            TrackMouseEvent(ref tm);
                        }

                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            RawPointerEventType.Move,
                            DipFromLParam(lParam), GetMouseModifiers(wParam));

                        break;
                    }

                case WindowsMessage.WM_MOUSEWHEEL:
                    {
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(0, (ToInt32(wParam) >> 16) / wheelDelta), GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSEHWHEEL:
                    {
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(-(ToInt32(wParam) >> 16) / wheelDelta, 0), GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSELEAVE:
                    {
                        _trackingMouse = false;
                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            RawPointerEventType.LeaveWindow,
                            new Point(-1, -1), WindowsKeyboardDevice.Instance.Modifiers);
                        break;
                    }

                case WindowsMessage.WM_NCLBUTTONDOWN:
                case WindowsMessage.WM_NCRBUTTONDOWN:
                case WindowsMessage.WM_NCMBUTTONDOWN:
                case WindowsMessage.WM_NCXBUTTONDOWN:
                    {
                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            (WindowsMessage)msg switch
                            {
                                WindowsMessage.WM_NCLBUTTONDOWN => RawPointerEventType
                                    .NonClientLeftButtonDown,
                                WindowsMessage.WM_NCRBUTTONDOWN => RawPointerEventType.RightButtonDown,
                                WindowsMessage.WM_NCMBUTTONDOWN => RawPointerEventType.MiddleButtonDown,
                                WindowsMessage.WM_NCXBUTTONDOWN =>
                                HighWord(ToInt32(wParam)) == 1 ?
                                    RawPointerEventType.XButton1Down :
                                    RawPointerEventType.XButton2Down,
                                _ => throw new AvaloniaInternalException(
                                    "No raw event defined for button down message.")
                            },
                            PointToClient(PointFromLParam(lParam)), GetMouseModifiers(wParam));
                        break;
                    }
                case WindowsMessage.WM_TOUCH:
                    {
                        var touchInputCount = wParam.ToInt32();

                        var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
                        var touchInputs = new Span<TOUCHINPUT>(pTouchInputs, touchInputCount);

                        if (GetTouchInputInfo(lParam, (uint)touchInputCount, pTouchInputs, Marshal.SizeOf<TOUCHINPUT>()))
                        {
                            foreach (var touchInput in touchInputs)
                            {
                                Input?.Invoke(new RawTouchEventArgs(_touchDevice, touchInput.Time,
                                    _owner,
                                    touchInput.Flags.HasAllFlags(TouchInputFlags.TOUCHEVENTF_UP) ?
                                        RawPointerEventType.TouchEnd :
                                        touchInput.Flags.HasAllFlags(TouchInputFlags.TOUCHEVENTF_DOWN) ?
                                            RawPointerEventType.TouchBegin :
                                            RawPointerEventType.TouchUpdate,
                                    PointToClient(new PixelPoint(touchInput.X / 100, touchInput.Y / 100)),
                                    WindowsKeyboardDevice.Instance.Modifiers,
                                    touchInput.Id));
                            }

                            CloseTouchInputHandle(lParam);
                            return IntPtr.Zero;
                        }

                        break;
                    }
                case WindowsMessage.WM_NCPAINT:
                    {
                        if (!HasFullDecorations)
                        {
                            return IntPtr.Zero;
                        }

                        break;
                    }

                case WindowsMessage.WM_NCACTIVATE:
                    {
                        if (!HasFullDecorations)
                        {
                            return new IntPtr(1);
                        }

                        break;
                    }

                case WindowsMessage.WM_PAINT:
                {
                    using(NonPumpingSyncContext.Use())
                    using (_rendererLock.Lock())
                    {
                        if (BeginPaint(_hwnd, out PAINTSTRUCT ps) != IntPtr.Zero)
                        {
                            var f = RenderScaling;
                            var r = ps.rcPaint;
                            Paint?.Invoke(new Rect(r.left / f, r.top / f, (r.right - r.left) / f,
                                (r.bottom - r.top) / f));
                            EndPaint(_hwnd, ref ps);
                        }
                    }

                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_SIZE:
                    {
                        using(NonPumpingSyncContext.Use())
                        using (_rendererLock.Lock())
                        {
                            // Do nothing here, just block until the pending frame render is completed on the render thread
                        }

                        var size = (SizeCommand)wParam;

                        if (Resized != null &&
                            (size == SizeCommand.Restored ||
                             size == SizeCommand.Maximized))
                        {
                            var clientSize = new Size(ToInt32(lParam) & 0xffff, ToInt32(lParam) >> 16);
                            Resized(clientSize / RenderScaling);
                        }

                        var windowState = size == SizeCommand.Maximized ?
                            WindowState.Maximized :
                            (size == SizeCommand.Minimized ? WindowState.Minimized : WindowState.Normal);

                        if (windowState != _lastWindowState)
                        {
                            _lastWindowState = windowState;

                            WindowStateChanged?.Invoke(windowState);

                            if (_isClientAreaExtended)
                            {
                                UpdateExtendMargins();

                                ExtendClientAreaToDecorationsChanged?.Invoke(true);
                            }
                        }

                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_MOVE:
                    {
                        PositionChanged?.Invoke(new PixelPoint((short)(ToInt32(lParam) & 0xffff),
                            (short)(ToInt32(lParam) >> 16)));
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_GETMINMAXINFO:
                    {
                        MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                        
                        _maxTrackSize = mmi.ptMaxTrackSize;

                        if (_minSize.Width > 0)
                        {
                            mmi.ptMinTrackSize.X =
                                (int)((_minSize.Width * RenderScaling) + BorderThickness.Left + BorderThickness.Right);
                        }

                        if (_minSize.Height > 0)
                        {
                            mmi.ptMinTrackSize.Y =
                                (int)((_minSize.Height * RenderScaling) + BorderThickness.Top + BorderThickness.Bottom);
                        }

                        if (!double.IsInfinity(_maxSize.Width) && _maxSize.Width > 0)
                        {
                            mmi.ptMaxTrackSize.X =
                                (int)((_maxSize.Width * RenderScaling) + BorderThickness.Left + BorderThickness.Right);
                        }

                        if (!double.IsInfinity(_maxSize.Height) && _maxSize.Height > 0)
                        {
                            mmi.ptMaxTrackSize.Y =
                                (int)((_maxSize.Height * RenderScaling) + BorderThickness.Top + BorderThickness.Bottom);
                        }

                        Marshal.StructureToPtr(mmi, lParam, true);
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_DISPLAYCHANGE:
                    {
                        (Screen as ScreenImpl)?.InvalidateScreensCache();
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_KILLFOCUS:
                    LostFocus?.Invoke();
                    break;
            }

#if USE_MANAGED_DRAG
            if (_managedDrag.PreprocessInputEvent(ref e))
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
#endif
            
            if(shouldTakeFocus)
            {
                SetFocus(_hwnd);
            }

            if (e != null && Input != null)
            {
                Input(e);

                if (e.Handled)
                {
                    return IntPtr.Zero;
                }
            }

            using (_rendererLock.Lock())
            {
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        private static int ToInt32(IntPtr ptr)
        {
            if (IntPtr.Size == 4)
                return ptr.ToInt32();

            return (int)(ptr.ToInt64() & 0xffffffff);
        }

        private static int HighWord(int param) => param >> 16;

        private Point DipFromLParam(IntPtr lParam)
        {
            return new Point((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16)) / RenderScaling;
        }

        private PixelPoint PointFromLParam(IntPtr lParam)
        {
            return new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16));
        }

        private bool ShouldIgnoreTouchEmulatedMessage()
        {
            if (!_multitouch)
            {
                return false;
            }

            // MI_WP_SIGNATURE
            // https://docs.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages
            const long marker = 0xFF515700L;

            var info = GetMessageExtraInfo().ToInt64();
            return (info & marker) == marker;
        }

        private static RawInputModifiers GetMouseModifiers(IntPtr wParam)
        {
            var keys = (ModifierKeys)ToInt32(wParam);
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;

            if (keys.HasAllFlags(ModifierKeys.MK_LBUTTON))
            {
                modifiers |= RawInputModifiers.LeftMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_RBUTTON))
            {
                modifiers |= RawInputModifiers.RightMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_MBUTTON))
            {
                modifiers |= RawInputModifiers.MiddleMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_XBUTTON1))
            {
                modifiers |= RawInputModifiers.XButton1MouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_XBUTTON2))
            {
                modifiers |= RawInputModifiers.XButton2MouseButton;
            }

            return modifiers;
        }
    }
}
