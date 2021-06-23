using Avalonia.Media;

namespace Avalonia.Platform
{
    public interface IDrawingContextWithAcrylicLikeSupport
    {
        void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect);
    }
}
