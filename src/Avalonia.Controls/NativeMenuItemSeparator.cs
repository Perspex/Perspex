using System;

namespace Avalonia.Controls
{

    [Obsolete("This class exists to maintain backwards compatibility with existing code. Use NativeMenuItemSeparator instead")]
    public class NativeMenuItemSeperator : NativeMenuItemSeparator 
    {
    }

    public class NativeMenuItemSeparator : NativeMenuItemBase
    {
        [Obsolete("This is a temporary hack to make our MenuItem recognize this as a separator, don't use", true)]
        public string Header => "-";
    }
}
