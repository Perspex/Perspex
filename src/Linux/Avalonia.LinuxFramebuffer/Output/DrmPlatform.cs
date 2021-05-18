using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform.Interop;

namespace Avalonia.LinuxFramebuffer.Output
{
    /// <summary>
    ///     Provides the base handle to the DRM output and its associated EGL interface.
    /// </summary>
    /// <remarks>
    ///     Shared resource creates a separation of the <see cref="IPlatformOpenGlInterface" /> instance, and
    ///     the <see cref="DrmOutput" /> instances, which provide the <see cref="IGlPlatformSurface" /> for each DRM connector.
    /// </remarks>
    public class DrmPlatform
    {
        public DrmPlatform(string path = null, double defaultScaling = 1.0)
            : this(new DrmCard(path), defaultScaling)
        {
        }

        public DrmPlatform(DrmCard card, double defaultScaling = 1.0)
        {
            DefaultScaling = defaultScaling;
            Card = card;
            EglPlatformInterface = new EglPlatformOpenGlInterface(new EglDisplay(new EglInterface(eglGetProcAddress),
                false, 0x31D7, Card.GbmDevice.Handle, null));
            
            // Register the platform OpenGL interface.
            AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(EglPlatformInterface);
        }

        public double DefaultScaling { get; set; }

        public DrmCard Card { get; }

        internal EglPlatformOpenGlInterface EglPlatformInterface { get; }

        public DrmOutput CreateOutput()
        {
            var resources = Card.GetResources();

            var connector = resources.Connectors.FirstOrDefault(x => x.IsConnected);
            if (connector == null)
                throw new InvalidOperationException("Unable to find connected DRM connector");

            var mode = connector.Modes.OrderByDescending(x => x.IsPreferred)
                .ThenByDescending(x => x.Resolution.Width * x.Resolution.Height)
                .FirstOrDefault();

            if (mode == null)
                throw new InvalidOperationException("Unable to find a usable DRM mode");

            return CreateOutput(connector, mode, resources);
        }

        /// <summary>
        /// Create a DRM display output instance.
        /// </summary>
        /// <param name="connector">The DRM connector the display output is to use.</param>
        /// <param name="mode">The display mode to use.</param>
        /// <param name="resources">All DRM resources for the card.</param>
        /// <returns>A <see cref="DrmOutput"/> instance for the provided configuration.</returns>
        public DrmOutput CreateOutput(DrmConnector connector, DrmModeInfo mode, DrmResources resources)
        {
            return new DrmOutput(this, resources, connector, mode)
            {
                Scaling = DefaultScaling
            };
        }

        /// <summary>
        /// Create a DRM display output instance.
        /// </summary>
        /// <param name="connector">The DRM connector the display output is to use.</param>
        /// <param name="mode">The display mode to use.</param>
        /// <param name="resources">All DRM resources for the card.</param>
        /// <param name="scaling">The sale to use for this DRM display output.</param>
        /// <returns>A <see cref="DrmOutput"/> instance for the provided configuration.</returns>
        public DrmOutput CreateOutput(DrmConnector connector, DrmModeInfo mode, DrmResources resources, double scaling)
        {
            return new DrmOutput(this, resources, connector, mode)
            {
                Scaling = scaling
            };
        }

        [DllImport("libEGL.so.1")]
        private static extern IntPtr eglGetProcAddress(Utf8Buffer proc);
    }
}
