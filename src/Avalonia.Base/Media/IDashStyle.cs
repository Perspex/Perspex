using System.Collections.Generic;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the sequence of dashes and gaps that will be applied by a <see cref="Pen"/>.
    /// </summary>
    public interface IDashStyle
    {
        /// <summary>
        /// Gets or sets the length of alternating dashes and gaps.
        /// </summary>
        IReadOnlyList<double> Dashes { get; }

        /// <summary>
        /// Gets or sets how far in the dash sequence the stroke will start.
        /// </summary>
        double Offset { get; }
    }
}
