using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            if (target is IStyleHost styleHost)
            {
                ApplyStyles(target, styleHost, new Style[] { });
            }
        }

        private void ApplyStyles(IStyleable target, IStyleHost host, Style[]? cancelStylesFromBelow)
        {
            var parent = host.StylingParent;

            var currentCancelStyles = (host as IStyleHostExtra)?.CanceledStyles;

            var cancelStylesForAbove = (cancelStylesFromBelow?.Union(currentCancelStyles ?? Enumerable.Empty<Style>())).ToArray();

            if (parent != null)
            {
                ApplyStyles(target, parent, cancelStylesForAbove);
            }

            if (host.IsStylesInitialized)
            {
                host.Styles.TryAttach(target, host, cancelStylesForAbove);
            }
        }
    }
}
