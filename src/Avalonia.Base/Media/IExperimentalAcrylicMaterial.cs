namespace Avalonia.Media
{
    /// <summary>
    /// Experimental Interface for producing Acrylic-like materials.
    /// </summary>
    public interface IExperimentalAcrylicMaterial
    {
        /// <summary>
        /// Gets the <see cref="AcrylicBackgroundSource"/> of the material.
        /// </summary>
        AcrylicBackgroundSource BackgroundSource { get; }

        /// <summary>
        /// Gets the TintColor of the material.
        /// </summary>
        Color TintColor { get; }

        /// <summary>
        /// Gets the TintOpacity of the material.
        /// </summary>
        double TintOpacity { get; }

        /// <summary>
        /// Gets the effective material color.
        /// </summary>
        Color MaterialColor { get; }        

        /// <summary>
        /// Gets the fallback color.
        /// </summary>
        Color FallbackColor { get; }
    }
}
