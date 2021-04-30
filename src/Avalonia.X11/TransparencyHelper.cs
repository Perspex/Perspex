using System;
using Avalonia.Controls;

namespace Avalonia.X11
{
    class TransparencyHelper :  IDisposable, X11Globals.IGlobalsSubscriber
    {
        private readonly X11Info _x11;
        private readonly IntPtr _window;
        private readonly X11Globals _globals;
        private WindowTransparencyLevel _currentLevel;
        private WindowTransparencyLevel _requestedLevel;
        private bool _blurAtomsAreSet;
        
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }
        public WindowTransparencyLevel CurrentLevel => _currentLevel;

        public TransparencyHelper(X11Info x11, IntPtr window, X11Globals globals)
        {
            _x11 = x11;
            _window = window;
            _globals = globals;
            _globals.AddSubscriber(this);
        }

        public void SetTransparencyRequest(WindowTransparencyLevel level)
        {
            _requestedLevel = level;
            UpdateTransparency();
        }

        private void UpdateTransparency()
        {
            var newLevel = UpdateAtomsAndGetTransparency();
            if (newLevel != _currentLevel)
            {
                _currentLevel = newLevel;
                TransparencyLevelChanged?.Invoke(newLevel);
            }
        }
        
        private WindowTransparencyLevel UpdateAtomsAndGetTransparency()
        {
            if (_requestedLevel >= WindowTransparencyLevel.Blur)
            {
                if (!_blurAtomsAreSet)
                {
                    IntPtr value = IntPtr.Zero;
                    XLib.XChangeProperty(_x11.Display, _window, _x11.Atoms._KDE_NET_WM_BLUR_BEHIND_REGION,
                        _x11.Atoms.XA_CARDINAL, 32, PropertyMode.Replace, ref value, 1);
                    _blurAtomsAreSet = true;
                }
            }
            else
            {
                if (_blurAtomsAreSet)
                {
                    XLib.XDeleteProperty(_x11.Display, _window, _x11.Atoms._KDE_NET_WM_BLUR_BEHIND_REGION);
                    _blurAtomsAreSet = false;
                }
            }

            if (!_globals.IsCompositionEnabled)
                return WindowTransparencyLevel.None;
            if (_requestedLevel >= WindowTransparencyLevel.Blur && CanBlur)
                return WindowTransparencyLevel.Blur;
            return WindowTransparencyLevel.Transparent;
        }

        private bool CanBlur => _globals.WmName == "KWin" && _globals.IsCompositionEnabled;
        
        public void Dispose()
        {
            _globals.RemoveSubscriber(this);
        }

        void X11Globals.IGlobalsSubscriber.WmChanged(string wmName) => UpdateTransparency();

        void X11Globals.IGlobalsSubscriber.CompositionChanged(bool compositing) => UpdateTransparency();
    }
}
