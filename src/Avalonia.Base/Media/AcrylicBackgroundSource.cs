namespace Avalonia.Media
{
    /// <summary>
    /// Background Sources for Acrylic.
    /// </summary>
    public enum AcrylicBackgroundSource
    {
        /// <summary>
        /// The acrylic has no background.
        /// </summary>
        None,

        /// <summary>
        /// Cuts through all render layers to reveal the window background.
        /// This means if your window is transparent or blurred it 
        /// will be blended with the material.
        /// </summary>
        Digger
    }
}
