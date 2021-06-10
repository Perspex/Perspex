#pragma warning disable IDE1006 // Naming Styles

using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Models
{
    public class ConsoleContext
    {
        private readonly ConsoleViewModel _owner;

        internal ConsoleContext(ConsoleViewModel owner) => _owner = owner;

        public readonly string help = @"Welcome to Avalonia DevTools. Here you can execute arbitrary C# code using Roslyn scripting.

The following variables are available:

e: The control currently selected in the logical or visual tree view
root: The root of the visual tree

The following commands are available:

clear(): Clear the output history
";

        public dynamic? e { get; internal set; }
        public dynamic? root { get; internal set; }

        internal static object NoOutput { get; } = new object();

        public object clear()
        {
            _owner.History.Clear();
            return NoOutput;
        }
    }
}
