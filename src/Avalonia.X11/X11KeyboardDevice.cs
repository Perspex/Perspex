using Avalonia.Input;

namespace Avalonia.X11
{
  class AvaloniaX11KeyboardDevice : KeyboardDevice
  {
    public override KeyStates NumLock 
    {
      get 
      {
        return KeyStates.None;
      }
    }

    public override KeyStates CapsLock
    {
      get
      {
        return KeyStates.None;
      }
    }

    public override KeyStates ScrollLock
    {
      get
      {
        return KeyStates.None;
      }
    }
  }
}
