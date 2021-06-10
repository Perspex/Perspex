using System;
using Avalonia.Controls;
using Avalonia.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;

#nullable enable

namespace Avalonia.Win32
{
    public partial class WindowImpl
    {
        // Hit test the frame for resizing and moving.
        private HitTestValues HitTestNCA(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // Get the point coordinates for the hit test (screen space).
            var ptMouse = PointFromLParam(lParam);

            // Get the window rectangle.
            GetWindowRect(hWnd, out var rcWindow);

            // Get the frame rectangle, adjusted for the style without a caption.
            var rcFrame = new RECT();
            AdjustWindowRectEx(ref rcFrame, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW & ~WindowStyles.WS_CAPTION), false, 0);

            var borderThickness = new RECT();
            if (GetStyle().HasAllFlags(WindowStyles.WS_THICKFRAME))
            {
                AdjustWindowRectEx(ref borderThickness, (uint)(GetStyle()), false, 0);
                borderThickness.left *= -1;
                borderThickness.top *= -1;
            }
            else if (GetStyle().HasAllFlags(WindowStyles.WS_BORDER))
            {
                borderThickness = new RECT { bottom = 1, left = 1, right = 1, top = 1 };
            }

            if (_extendTitleBarHint >= 0)
            {
                borderThickness.top = (int)(_extendedMargins.Top * RenderScaling);
            }

            // Determine if the hit test is for resizing. Default middle (1,1).
            ushort uRow = 1;
            ushort uCol = 1;
            bool onResizeBorder = false;

            // Determine if the point is at the left or right of the window.
            if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + borderThickness.left)
            {
                uCol = 0; // left side
            }
            else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - borderThickness.right)
            {
                uCol = 2; // right side
            }

            // Determine if the point is at the top or bottom of the window.
            if (ptMouse.Y >= rcWindow.top && ptMouse.Y < rcWindow.top + borderThickness.top)
            {
                onResizeBorder = (ptMouse.Y < (rcWindow.top - rcFrame.top));

                // Two cases where we have a valid row 0 hit test:
                // - window resize border (top resize border hit)
                // - area below resize border that is actual titlebar (caption hit).
                if (onResizeBorder || uCol == 1)
                {
                    uRow = 0;
                }
            }
            else if (ptMouse.Y < rcWindow.bottom && ptMouse.Y >= rcWindow.bottom - borderThickness.bottom)
            {
                uRow = 2;
            }

            ReadOnlySpan<HitTestValues> hitZones = stackalloc HitTestValues[]
            {
                HitTestValues.HTTOPLEFT, onResizeBorder ? HitTestValues.HTTOP : HitTestValues.HTCAPTION,
                HitTestValues.HTTOPRIGHT, HitTestValues.HTLEFT, HitTestValues.HTNOWHERE, HitTestValues.HTRIGHT,
                HitTestValues.HTBOTTOMLEFT, HitTestValues.HTBOTTOM, HitTestValues.HTBOTTOMRIGHT
            };

            var zoneIndex = uRow * 3 + uCol;

            return hitZones[zoneIndex];
        }

        protected virtual IntPtr CustomCaptionProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool callDwp)
        {
            IntPtr lRet = IntPtr.Zero;

            callDwp = !DwmDefWindowProc(hWnd, msg, wParam, lParam, ref lRet);

            switch ((WindowsMessage)msg)
            {
                case WindowsMessage.WM_DWMCOMPOSITIONCHANGED:
                    // TODO handle composition changed.
                    break;

                case WindowsMessage.WM_NCHITTEST:
                    if (lRet == IntPtr.Zero)
                    {
                        if(WindowState == WindowState.FullScreen)
                        {
                            return (IntPtr)HitTestValues.HTCLIENT;
                        }
                        var hittestResult = HitTestNCA(hWnd, wParam, lParam);

                        lRet = (IntPtr)hittestResult;

                        uint timestamp = unchecked((uint)GetMessageTime());

                        if (hittestResult == HitTestValues.HTCAPTION)
                        {
                            var position = PointToClient(PointFromLParam(lParam));

                            if (_owner is Window window)
                            {
                                var visual = window.Renderer.HitTestFirst(position, _owner as Window, x =>
                                {
                                    if (x is IInputElement ie && (!ie.IsHitTestVisible || !ie.IsVisible))
                                    {
                                        return false;
                                    }

                                    return true;
                                });

                                if (visual != null)
                                {
                                    hittestResult = HitTestValues.HTCLIENT;
                                    lRet = (IntPtr)hittestResult;
                                }
                            }
                        }

                        if (hittestResult != HitTestValues.HTNOWHERE)
                        {
                            callDwp = false;
                        }
                    }
                    break;
            }

            return lRet;
        }
    }
}
