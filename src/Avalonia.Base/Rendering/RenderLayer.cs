using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayer
    {
        public RenderLayer(
            IDrawingContextImpl drawingContext,
            Size size,
            double scaling,
            IVisual layerRoot)
        {
            Bitmap = RefCountable.Create(drawingContext.CreateLayer(size));
            Size = size;
            Scaling = scaling;
            LayerRoot = layerRoot;
            IsEmpty = true;
        }

        public IRef<IDrawingContextLayerImpl> Bitmap { get; private set; }
        public bool IsEmpty { get; set; }
        public double Scaling { get; private set; }
        public Size Size { get; private set; }
        public IVisual LayerRoot { get; }

        public void RecreateBitmap(IDrawingContextImpl drawingContext, Size size, double scaling)
        {
            if (Size != size || Scaling != scaling)
            {
                var resized = RefCountable.Create(drawingContext.CreateLayer(size));

                using (var context = resized.Item.CreateDrawingContext(null))
                {
                    Bitmap.Dispose();
                    context.Clear(default);
                    
                    Bitmap = resized;
                    Scaling = scaling;
                    Size = size;
                    IsEmpty = true;
                }
            }
        }
    }
}
