namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IOutputBackend
    {
        /// <summary>
        /// Gets the size of the output display in device pixels.
        /// </summary>
        PixelSize PixelSize { get; }
        
        /// <summary>
        /// Gets or sets the scaling of the output display.
        /// </summary>
        double Scaling { get; set; }
        
        /// <summary>
        /// Gets the interface name for the display output implementation.
        /// </summary>
        string Name { get; }
    }
}
