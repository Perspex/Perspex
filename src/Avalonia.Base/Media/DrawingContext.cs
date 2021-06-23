using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    public sealed class DrawingContext : IDisposable
    {
        private readonly bool _ownsImpl;
        private int _currentLevel;


        private static ThreadSafeObjectPool<Stack<PushedState>> StateStackPool { get; } =
            ThreadSafeObjectPool<Stack<PushedState>>.Default;

        private static ThreadSafeObjectPool<Stack<TransformContainer>> TransformStackPool { get; } =
            ThreadSafeObjectPool<Stack<TransformContainer>>.Default;

        private Stack<PushedState> _states = StateStackPool.Get();

        private Stack<TransformContainer> _transformContainers = TransformStackPool.Get();

        readonly struct TransformContainer
        {
            public readonly Matrix LocalTransform;
            public readonly Matrix ContainerTransform;

            public TransformContainer(Matrix localTransform, Matrix containerTransform)
            {
                LocalTransform = localTransform;
                ContainerTransform = containerTransform;
            }
        }

        public DrawingContext(IDrawingContextImpl impl)
        {
            PlatformImpl = impl;
            _ownsImpl = true;
        }
        
        public DrawingContext(IDrawingContextImpl impl, bool ownsImpl)
        {
            _ownsImpl = ownsImpl;
            PlatformImpl = impl;
        }

        public IDrawingContextImpl PlatformImpl { get; }

        private Matrix _currentTransform = Matrix.Identity;

        private Matrix _currentContainerTransform = Matrix.Identity;

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return _currentTransform; }
            private set
            {
                _currentTransform = value;
                var transform = _currentTransform * _currentContainerTransform;
                PlatformImpl.Transform = transform;
            }
        }

        //HACK: This is a temporary hack that is used in the render loop 
        //to update TransformedBounds property
        [Obsolete("HACK for render loop, don't use")]
        public Matrix CurrentContainerTransform => _currentContainerTransform;

        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="source">The image.</param>
        /// <param name="rect">The rect in the output to draw to.</param>
        public void DrawImage(IImage source, Rect rect)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            DrawImage(source, new Rect(source.Size), rect);
        }

        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="source">The image.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        public void DrawImage(IImage source, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode = default)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            source.Draw(this, sourceRect, destRect, bitmapInterpolationMode);
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            if (PenIsVisible(pen))
            {
                PlatformImpl.DrawLine(pen, p1, p2);
            }
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush brush, IPen pen, Geometry geometry)
        {
            DrawGeometry(brush, pen, geometry.PlatformImpl);
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            Contract.Requires<ArgumentNullException>(geometry != null);

            if (brush != null || PenIsVisible(pen))
            {
                PlatformImpl.DrawGeometry(brush, pen, geometry);
            }
        }

        /// <summary>
        /// Draws a rectangle with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the rectangle, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the rectangle, or <c>null</c> for no stroke.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="radiusX">The radius in the X dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Width/2
        /// </param>
        /// <param name="radiusY">The radius in the Y dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Height/2
        /// </param>
        /// <param name="boxShadows">Box shadow effect parameters</param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawRectangle(IBrush brush, IPen pen, Rect rect, double radiusX = 0, double radiusY = 0,
            BoxShadows boxShadows = default)
        {
            if (brush == null && !PenIsVisible(pen))
            {
                return;
            }

            if (!MathUtilities.IsZero(radiusX))
            {
                radiusX = Math.Min(radiusX, rect.Width / 2);
            }

            if (!MathUtilities.IsZero(radiusY))
            {
                radiusY = Math.Min(radiusY, rect.Height / 2);
            }

            PlatformImpl.DrawRectangle(brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void DrawRectangle(IPen pen, Rect rect, float cornerRadius = 0.0f)
        {
            DrawRectangle(null, pen, rect, cornerRadius, cornerRadius);
        }

        /// <summary>
        /// Draws a custom drawing operation
        /// </summary>
        /// <param name="custom">custom operation</param>
        public void Custom(ICustomDrawOperation custom) => PlatformImpl.Custom(custom);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(IBrush foreground, Point origin, FormattedText text)
        {
            Contract.Requires<ArgumentNullException>(text != null);

            if (foreground != null)
            {
                PlatformImpl.DrawText(foreground, origin, text.PlatformImpl);
            }
        }

        /// <summary>
        /// Draws a glyph run.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="glyphRun">The glyph run.</param>
        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
            Contract.Requires<ArgumentNullException>(glyphRun != null);

            if (foreground != null)
            {
                PlatformImpl.DrawGlyphRun(foreground, glyphRun);
            }
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0.0f)
        {
            DrawRectangle(brush, null, rect, cornerRadius, cornerRadius);
        }

        public readonly struct PushedState : IDisposable
        {
            private readonly int _level;
            private readonly DrawingContext _context;
            private readonly Matrix _matrix;
            private readonly PushedStateType _type;

            public enum PushedStateType
            {
                None,
                Matrix,
                Opacity,
                Clip,
                MatrixContainer,
                GeometryClip,
                OpacityMask
            }

            public PushedState(DrawingContext context, PushedStateType type, Matrix matrix = default(Matrix))
            {
                _context = context;
                _type = type;
                _matrix = matrix;
                _level = context._currentLevel += 1;
                context._states.Push(this);
            }

            public void Dispose()
            {
                if (_type == PushedStateType.None)
                    return;
                if (_context._currentLevel != _level)
                    throw new InvalidOperationException("Wrong Push/Pop state order");
                _context._currentLevel--;
                _context._states.Pop();
                if (_type == PushedStateType.Matrix)
                    _context.CurrentTransform = _matrix;
                else if (_type == PushedStateType.Clip)
                    _context.PlatformImpl.PopClip();
                else if (_type == PushedStateType.Opacity)
                    _context.PlatformImpl.PopOpacity();
                else if (_type == PushedStateType.GeometryClip)
                    _context.PlatformImpl.PopGeometryClip();
                else if (_type == PushedStateType.OpacityMask)
                    _context.PlatformImpl.PopOpacityMask();
                else if (_type == PushedStateType.MatrixContainer)
                {
                    var cont = _context._transformContainers.Pop();
                    _context._currentContainerTransform = cont.ContainerTransform;
                    _context.CurrentTransform = cont.LocalTransform;
                }
            }
        }


        public PushedState PushClip(RoundedRect clip)
        {
            PlatformImpl.PushClip(clip);
            return new PushedState(this, PushedState.PushedStateType.Clip);
        }

        /// <summary>
        /// Pushes a clip rectangle.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public PushedState PushClip(Rect clip)
        {
            PlatformImpl.PushClip(clip);
            return new PushedState(this, PushedState.PushedStateType.Clip);
        }

        /// <summary>
        /// Pushes a clip geometry.
        /// </summary>
        /// <param name="clip">The clip geometry.</param>
        /// <returns>A disposable used to undo the clip geometry.</returns>
        public PushedState PushGeometryClip(Geometry clip)
        {
            Contract.Requires<ArgumentNullException>(clip != null);
            PlatformImpl.PushGeometryClip(clip.PlatformImpl);
            return new PushedState(this, PushedState.PushedStateType.GeometryClip);
        }

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public PushedState PushOpacity(double opacity)
        //TODO: Eliminate platform-specific push opacity call
        {
            PlatformImpl.PushOpacity(opacity);
            return new PushedState(this, PushedState.PushedStateType.Opacity);
        }

        /// <summary>
        /// Pushes an opacity mask.
        /// </summary>
        /// <param name="mask">The opacity mask.</param>
        /// <param name="bounds">
        /// The size of the brush's target area. TODO: Are we sure this is needed?
        /// </param>
        /// <returns>A disposable to undo the opacity mask.</returns>
        public PushedState PushOpacityMask(IBrush mask, Rect bounds)
        {
            PlatformImpl.PushOpacityMask(mask, bounds);
            return new PushedState(this, PushedState.PushedStateType.OpacityMask);
        }

        /// <summary>
        /// Pushes a matrix post-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPostTransform(Matrix matrix) => PushSetTransform(CurrentTransform * matrix);

        /// <summary>
        /// Pushes a matrix pre-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPreTransform(Matrix matrix) => PushSetTransform(matrix * CurrentTransform);

        /// <summary>
        /// Sets the current matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushSetTransform(Matrix matrix)
        {
            var oldMatrix = CurrentTransform;
            CurrentTransform = matrix;

            return new PushedState(this, PushedState.PushedStateType.Matrix, oldMatrix);
        }

        /// <summary>
        /// Pushes a new transform context.
        /// </summary>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushTransformContainer()
        {
            _transformContainers.Push(new TransformContainer(CurrentTransform, _currentContainerTransform));
            _currentContainerTransform = CurrentTransform * _currentContainerTransform;
            _currentTransform = Matrix.Identity;
            return new PushedState(this, PushedState.PushedStateType.MatrixContainer);
        }

        /// <summary>
        /// Disposes of any resources held by the <see cref="DrawingContext"/>.
        /// </summary>
        public void Dispose()
        {
            while (_states.Count != 0)
                _states.Peek().Dispose();
            StateStackPool.Return(_states);
            _states = null;
            if (_transformContainers.Count != 0)
                throw new InvalidOperationException("Transform container stack is non-empty");
            TransformStackPool.Return(_transformContainers);
            _transformContainers = null;
            if (_ownsImpl)
                PlatformImpl.Dispose();
        }

        private static bool PenIsVisible(IPen pen)
        {
            return pen?.Brush != null && pen.Thickness > 0;
        }
    }
}
