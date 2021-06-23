using System.ComponentModel;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    [TypeConverter(typeof(BrushConverter))]
    public interface IBrush
    {
        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        double Opacity { get; }
    }
}
