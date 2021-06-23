using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// A collection of <see cref="GradientStop"/>s.
    /// </summary>
    public class GradientStops : AvaloniaList<GradientStop>
    {
        public GradientStops()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public IReadOnlyList<ImmutableGradientStop> ToImmutable()
        {
            return this.Select(x => new ImmutableGradientStop(x.Offset, x.Color)).ToList();
        }
    }
}
