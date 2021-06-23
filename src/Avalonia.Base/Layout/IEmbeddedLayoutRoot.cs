namespace Avalonia.Layout
{
    /// <summary>
    /// A special layout root with enforced size for Arrange pass
    /// </summary>
    public interface IEmbeddedLayoutRoot : ILayoutRoot
    {
        Size AllocatedSize { get; }
    }
}