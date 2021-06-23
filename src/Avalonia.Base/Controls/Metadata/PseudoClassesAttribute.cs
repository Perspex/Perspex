using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PseudoClassesAttribute : Attribute
    {
        public PseudoClassesAttribute(params string[] pseudoClasses)
        {
            PseudoClasses = pseudoClasses;
        }

        public IReadOnlyList<string> PseudoClasses { get; }
    }
}
