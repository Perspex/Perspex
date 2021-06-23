using System;
using Avalonia.Rendering;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a render target
    /// </summary>
    /// <remarks>
    /// The interface used for obtaining drawing context from surfaces you can render on.
    /// </remarks>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>
        /// Creates an <see cref="IDrawingContextImpl"/> for a rendering session.
        /// </summary>
        /// <param name="visualBrushRenderer">
        /// A render to be used to render visual brushes. May be null if no visual brushes are
        /// to be drawn.
        /// </param>
        IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer);
    }

    public interface IRenderTargetWithCorruptionInfo : IRenderTarget
    {
        bool IsCorrupted { get; }
    }
}
