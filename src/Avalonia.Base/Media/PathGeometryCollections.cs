using Avalonia.Collections;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media
{
    public sealed class PathFigures : AvaloniaList<PathFigure>
    {
        /// <summary>
        /// Parses the specified path data to a <see cref="PathFigures"/>.
        /// </summary>
        /// <param name="pathData">The s.</param>
        /// <returns></returns>
        public static PathFigures Parse(string pathData)
        {
            var pathGeometry = new PathGeometry();
            
            using (var context = new PathGeometryContext(pathGeometry))
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(pathData);
            }

            return pathGeometry.Figures;
        }
    }

    public sealed class PathSegments : AvaloniaList<PathSegment>
    {
    }
}