namespace Avalonia.Media
{
    public abstract class PathSegment : AvaloniaObject
    {
        protected internal abstract void ApplyTo(StreamGeometryContext ctx);
    }
}