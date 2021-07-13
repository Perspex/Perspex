using System;
using System.Collections.Generic;
using System.Text;

using Avalonia.Input.TextInput;
using Avalonia.Threading;

using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// A Windows input method editor based on Windows Input Method Manager (IMM32).
    /// </summary>
    class Imm32InputMethod : ITextInputMethodImpl, IDisposable
    {
        private bool _disposedValue;
        private IntPtr _hwnd;
        private WindowImpl _parent;
        private bool _active;
        private bool _systemCaret;
        private bool _showCompositionWindow;
        private bool _showCandidateList;
        private ushort _langId;
        private const int _caretMargin = 1;

        public Imm32InputMethod(WindowImpl parent, IntPtr hwnd, IntPtr HKL)
        {
            _parent = parent;
            _hwnd = hwnd;
            _disposedValue = false;
            _active = false;
            _langId = PRIMARYLANGID(LGID(HKL));
            _systemCaret = (_langId == LANG_ZH || _langId == LANG_JA);
            _showCompositionWindow = true;
            _showCandidateList = true;
            if (_systemCaret)
            {
                CreateCaret(_hwnd, IntPtr.Zero, 1, 1);
            }
            IsComposing = false;
        }

        public void Reset()
        {
            if (IsComposing)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IntPtr himc = ImmGetContext(_hwnd);
                    ImmNotifyIME(himc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                    ImmReleaseContext(_hwnd, himc);
                });
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            Dispatcher.UIThread.Post(() =>
            {
                IntPtr himc = ImmGetContext(_hwnd);
                ImmSetActiveContext(himc, active);
                ImmReleaseContext(_hwnd, himc);
            });
        }

        public void SetCursorRect(Rect rect)
        {
            var focused = GetActiveWindow() == _hwnd;
            if (!focused)
            {
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                IntPtr himc = ImmGetContext(_hwnd);
                if (himc == IntPtr.Zero)
                {
                    return;
                }

                // see: https://chromium.googlesource.com/experimental/chromium/src/+/bf09a5036ccfb77d2277247c66dc55daf41df3fe/chrome/browser/ime_input.cc
                // see: https://engine.chinmaygarde.com/window__win32_8cc_source.html

                var p1 = rect.TopLeft;
                var p2 = rect.BottomRight;
                var s = _parent.DesktopScaling;
                var (x1, y1, x2, y2) = ((int)(p1.X * s), (int)(p1.Y * s), (int)(p2.X * s), (int)(p2.Y * s));

                if (_showCompositionWindow)
                {
                    var compForm = new COMPOSITIONFORM
                    {
                        dwStyle = CFS_POINT,
                        ptCurrentPos = new POINT { X = x1, Y = y1 },
                    };
                    ImmSetCompositionWindow(himc, ref compForm);
                }

                if (_showCandidateList)
                {
                    var candidateForm = new CANDIDATEFORM
                    {
                        dwIndex = 0,
                        dwStyle = CFS_CANDIDATEPOS,
                        ptCurrentPos = new POINT { X = x2, Y = y2 }
                    };
                    ImmSetCandidateWindow(himc, ref candidateForm);

                    if (_systemCaret)
                    {
                        SetCaretPos(x2, y2);
                    }

                    if (_langId == LANG_KO)
                    {
                        y2 += _caretMargin;
                    }

                    candidateForm = new CANDIDATEFORM
                    {
                        dwIndex = 0,
                        dwStyle = CFS_EXCLUDE,
                        ptCurrentPos = new POINT { X = x2, Y = y2 },
                        rcArea = new RECT { left = x2, top = y2, right = x2, bottom = y2 + _caretMargin}
                    };
                }

                ImmReleaseContext(_hwnd, himc);
            });
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            // ???
        }

        protected void _dispose()
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
                if (_systemCaret)
                {
                    _systemCaret = false;
                    DestroyCaret();
                }
            }
        }

        public bool IsComposing { get; set; }

        ~Imm32InputMethod()
        {
            _dispose();
        }

        public void Dispose()
        {
            _dispose();
            GC.SuppressFinalize(this);
        }
    }
}
