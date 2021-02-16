using System.ComponentModel;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
#if !BUILDTASK
    [TypeConverter(typeof(BrushConverter))]
    public
#endif
    interface IBrush
    {
        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        double Opacity { get; }
    }
}
