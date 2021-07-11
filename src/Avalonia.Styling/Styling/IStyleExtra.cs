using System.Collections.Generic;

namespace Avalonia.Styling
{
    public interface IStyleExtra : IStyle
    {

        /// <summary>
        /// Attaches the style and any child styles to a control if the style's selector matches.
        /// </summary>
        /// <param name="target">The control to attach to.</param>
        /// <param name="host">The element that hosts the style.</param>
        /// <param name="cancelStylesFromBelow">The styles to cancel from below in the logical tree</param>
        /// <returns>
        /// A <see cref="SelectorMatchResult"/> describing how the style matches the control.
        /// </returns>
        SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host, IEnumerable<Style> cancelStylesFromBelow);
    }
}
