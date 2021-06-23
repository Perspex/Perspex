using System;

namespace Avalonia.Controls
{
    public class NameScopeEventArgs : EventArgs
    {
        public NameScopeEventArgs(string name, object element)
        {
            Name = name;
            Element = element;
        }

        public string Name { get; }
        public object Element { get; }
    }
}
