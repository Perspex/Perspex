using System;

namespace Avalonia.Media
{
    public static class MaterialExtensions
    {
        /// <summary>
        /// Converts a brush to an immutable brush.
        /// </summary>
        /// <param name="material">The brush.</param>
        /// <returns>
        /// The result of calling <see cref="IMutableBrush.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="material"/>.
        /// </returns>
        public static IExperimentalAcrylicMaterial ToImmutable(this IExperimentalAcrylicMaterial material)
        {
            Contract.Requires<ArgumentNullException>(material != null);

            return (material as IMutableExperimentalAcrylicMaterial)?.ToImmutable() ?? material;
        }
    }
}
