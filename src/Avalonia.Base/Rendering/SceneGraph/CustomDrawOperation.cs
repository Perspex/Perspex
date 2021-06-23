using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    internal sealed class CustomDrawOperation : DrawOperation
    {
        public Matrix Transform { get; }
        public ICustomDrawOperation Custom { get; }
        public CustomDrawOperation(ICustomDrawOperation custom, Matrix transform) 
            : base(custom.Bounds, transform)
        {
            Transform = transform;
            Custom = custom;
        }

        public override bool HitTest(Point p)
        {
            return Custom.HitTest(p * Transform);
        }

        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            Custom.Render(context);
        }

        public override void Dispose() => Custom.Dispose();

        public bool Equals(Matrix transform, ICustomDrawOperation custom) =>
            Transform == transform && Custom?.Equals(custom) == true;
    }

    public interface ICustomDrawOperation : IDrawOperation, IEquatable<ICustomDrawOperation>
    {
        
    }
}
