namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.WriteableBitmap"/>.
    /// </summary>
    public interface IWriteableBitmapImpl : IBitmapImpl
    {
        ILockedFramebuffer Lock();
    }
}
