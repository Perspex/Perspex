namespace Avalonia.Platform
{
    /// <summary>
    /// Represents a geometry with a transform applied.
    /// </summary>
    /// <remarks>
    /// An <see cref="ITransformedGeometryImpl"/> transforms a geometry without transforming its
    /// stroke thickness.
    /// </remarks>
    public interface ITransformedGeometryImpl : IGeometryImpl
    {
        /// <summary>
        /// Gets the source geometry that the <see cref="Transform"/> is applied to.
        /// </summary>
        IGeometryImpl SourceGeometry { get; }

        /// <summary>
        /// Gets the applied transform.
        /// </summary>
        Matrix Transform { get; }
    }
}
