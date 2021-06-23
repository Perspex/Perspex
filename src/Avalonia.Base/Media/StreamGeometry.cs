using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an arbitrarily complex shape.
    /// </summary>
    public class StreamGeometry : Geometry
    {
        IStreamGeometryImpl _impl;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        public StreamGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        private StreamGeometry(IStreamGeometryImpl impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Creates a <see cref="StreamGeometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
        public static new StreamGeometry Parse(string s)
        {
            var streamGeometry = new StreamGeometry();

            using (var context = streamGeometry.Open())
            using (var parser = new PathMarkupParser(context))
            {               
                parser.Parse(s);
            }

            return streamGeometry;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new StreamGeometry(((IStreamGeometryImpl)PlatformImpl).Clone());
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)PlatformImpl).Open());
        }

        /// <inheritdoc/>
        protected override IGeometryImpl CreateDefiningGeometry()
        {
            if (_impl == null)
            {
                var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                _impl = factory.CreateStreamGeometry();
            }

            return _impl;
        }
    }
}
