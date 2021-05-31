using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.Native
{
    internal class WindowImpl : WindowBaseImpl, IWindowImpl, ITopLevelImplWithNativeMenuExporter
    {
        private readonly AvaloniaNativePlatformOptions _opts;
        private readonly AvaloniaNativePlatformOpenGlInterface _glFeature;
        IAvnWindow _native;
        private double _extendTitleBarHeight = -1;
        private DoubleClickHelper _doubleClickHelper;
        

        internal WindowImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts,
            AvaloniaNativePlatformOpenGlInterface glFeature) : base(factory, opts, glFeature)
        {
            _opts = opts;
            _glFeature = glFeature;
            _doubleClickHelper = new DoubleClickHelper();
            
            using (var e = new WindowEvents(this))
            {
                var context = _opts.UseGpu ? glFeature?.MainContext : null;
                Init(_native = factory.CreateWindow(e, context?.Context), factory.CreateScreens(), context);
            }

            NativeMenuExporter = new AvaloniaNativeMenuExporter(_native, factory);
        }

        class WindowEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly WindowImpl _parent;

            public WindowEvents(WindowImpl parent) : base(parent)
            {
                _parent = parent;
            }

            int IAvnWindowEvents.Closing()
            {
                if (_parent.Closing != null)
                {
                    return _parent.Closing().AsComBool();
                }

                return true.AsComBool();
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
                _parent.InvalidateExtendedMargins();

                _parent.WindowStateChanged?.Invoke((WindowState)state);
            }

            void IAvnWindowEvents.GotInputWhenDisabled()
            {
                _parent.GotInputWhenDisabled?.Invoke();
            }
        }

        public IAvnWindow Native => _native;

        public void CanResize(bool value)
        {
            _native.SetCanResize(value.AsComBool());
        }

        public void SetSystemDecorations(Controls.SystemDecorations enabled)
        {
            _native.SetDecorations((Interop.SystemDecorations)enabled);
        }

        public void SetTitleBarColor(Avalonia.Media.Color color)
        {
            _native.SetTitleBarColor(new AvnColor { Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B });
        }

        public void SetTitle(string title) => _native.SetTitle(title);

        public WindowState WindowState
        {
            get => (WindowState)_native.WindowState;
            set => _native.SetWindowState((AvnWindowState)value);
        }

        public Action<WindowState> WindowStateChanged { get; set; }        

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; private set; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        private bool _isExtended;
        public bool IsClientAreaExtendedToDecorations => _isExtended;

        protected override bool ChromeHitTest (RawPointerEventArgs e)
        {
            if(_isExtended)
            {
                if(e.Type == RawPointerEventType.LeftButtonDown)
                {
                    var visual = (_inputRoot as Window).Renderer.HitTestFirst(e.Position, _inputRoot as Window, x =>
                            {
                                if (x is IInputElement ie && !ie.IsHitTestVisible)
                                {
                                    return false;
                                }
                                return true;
                            });

                    if(visual == null)
                    {
                        if (_doubleClickHelper.IsDoubleClick(e.Timestamp, e.Position))
                        {
                            // TOGGLE WINDOW STATE.
                            if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
                            {
                                WindowState = WindowState.Normal;
                            }
                            else
                            {
                                WindowState = WindowState.Maximized;
                            }
                        }
                        else
                        {
                            _native.BeginMoveDrag();   
                        }
                    }
                }
            }

            return false;
        }
        
        private void InvalidateExtendedMargins()
        {
            if (WindowState ==  WindowState.FullScreen)
            {
                ExtendedMargins = new Thickness();
            }
            else
            {
                ExtendedMargins = _isExtended ? new Thickness(0, _extendTitleBarHeight == -1 ? _native.ExtendTitleBarHeight : _extendTitleBarHeight, 0, 0) : new Thickness();
            }

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            _isExtended = extendIntoClientAreaHint;

            _native.SetExtendClientArea(extendIntoClientAreaHint.AsComBool());

            InvalidateExtendedMargins();
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {   
            _native.SetExtendClientAreaHints ((AvnExtendClientAreaChromeHints)hints);
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHeight = titleBarHeight;
            _native.SetExtendTitleBarHeight(titleBarHeight);

            ExtendedMargins = _isExtended ? new Thickness(0, titleBarHeight == -1 ? _native.ExtendTitleBarHeight : titleBarHeight, 0, 0) : new Thickness();

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        /// <inheritdoc/>
        public bool NeedsManagedDecorations => false;

        public void ShowTaskbarIcon(bool value)
        {
            // NO OP On OSX
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            // NO OP on OSX
        }

        public Func<bool> Closing { get; set; }

        public ITopLevelNativeMenuExporter NativeMenuExporter { get; }

        public void Move(PixelPoint point) => Position = point;

        public override IPopupImpl CreatePopup() =>
            _opts.OverlayPopups ? null : new PopupImpl(_factory, _opts, _glFeature, this);

        public Action GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl parent)
        {
            _native.SetParent(((WindowImpl)parent).Native);
        }

        public void SetEnabled(bool enable)
        {
            _native.SetEnabled(enable.AsComBool());
        }
    }
}
