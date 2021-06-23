namespace Avalonia.Media
{
    /// <summary>
    /// Represents a mutable brush which can return an immutable clone of itself.
    /// </summary>
    public interface IMutableExperimentalAcrylicMaterial : IExperimentalAcrylicMaterial, IAffectsRender
    {
        /// <summary>
        /// Creates an immutable clone of the brush.
        /// </summary>
        /// <returns>The immutable clone.</returns>
        IExperimentalAcrylicMaterial ToImmutable();
    }
}
