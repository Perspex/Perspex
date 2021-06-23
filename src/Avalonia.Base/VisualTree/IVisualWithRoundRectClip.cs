using System;

namespace Avalonia.VisualTree
{
    [Obsolete("Internal API, will be removed in future versions, you've been warned")]
    public interface IVisualWithRoundRectClip
    {
        /// <summary>
        /// Gets a value indicating the corner radius of control's clip bounds
        /// </summary>
        [Obsolete("Internal API, will be removed in future versions, you've been warned")]
        CornerRadius ClipToBoundsRadius { get; }

    }
}
