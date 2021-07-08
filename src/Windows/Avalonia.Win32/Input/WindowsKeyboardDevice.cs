using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    class WindowsKeyboardDevice : KeyboardDevice
    {
        private readonly byte[] _keyStates = new byte[256];

        public new static WindowsKeyboardDevice Instance { get; } = new WindowsKeyboardDevice();

        public RawInputModifiers Modifiers
        {
            get
            {
                UpdateKeyStatesCache();
                RawInputModifiers result = 0;

                if (IsDown(Key.LeftAlt) || IsDown(Key.RightAlt))
                {
                    result |= RawInputModifiers.Alt;
                }

                if (IsDown(Key.LeftCtrl) || IsDown(Key.RightCtrl))
                {
                    result |= RawInputModifiers.Control;
                }

                if (IsDown(Key.LeftShift) || IsDown(Key.RightShift))
                {
                    result |= RawInputModifiers.Shift;
                }

                if (IsDown(Key.LWin) || IsDown(Key.RWin))
                {
                    result |= RawInputModifiers.Meta;
                }

                return result;
            }
        }

        public void WindowActivated(Window window)
        {
            SetFocusedElement(window, NavigationMethod.Unspecified, KeyModifiers.None);
        }

        public string StringFromVirtualKey(uint virtualKey)
        {
            StringBuilder result = new StringBuilder(256);
            int length = UnmanagedMethods.ToUnicode(
                virtualKey,
                0,
                _keyStates,
                result,
                256,
                0);
            return result.ToString();
        }

        public override KeyStates NumLock 
        {
          get 
          {
            return GetKeyStatesFromApi(Key.NumLock);
          }
        }

        public override KeyStates CapsLock
        {
          get
          {
            return GetKeyStatesFromApi(Key.CapsLock);
          }
        }

        public override KeyStates ScrollLock
        {
          get
          {
            return GetKeyStatesFromApi(Key.Scroll);
          }
        }

        private void UpdateKeyStatesCache()
        {
            UnmanagedMethods.GetKeyboardState(_keyStates);
        }

        private bool IsDown(Key key)
        {
            return (GetKeyStatesFromCache(key) & KeyStates.Down) != 0;
        }

        private KeyStates GetKeyStatesFromApi(Key key)
        {
            int vk = KeyInterop.VirtualKeyFromKey(key);
            short state = UnmanagedMethods.GetKeyState(vk);
            KeyStates result = 0;

            if ((state & 0x8000) != 0)
            {
                result |= KeyStates.Down;
            }

            if ((state & 0x01) != 0)
            {
                result |= KeyStates.Toggled;
            }

            return result;
        }

        private KeyStates GetKeyStatesFromCache(Key key)
        {
            int vk = KeyInterop.VirtualKeyFromKey(key);
            byte state = _keyStates[vk];
            KeyStates result = 0;

            if ((state & 0x80) != 0)
            {
                result |= KeyStates.Down;
            }

            if ((state & 0x01) != 0)
            {
                result |= KeyStates.Toggled;
            }

            return result;
        }
    }
}
