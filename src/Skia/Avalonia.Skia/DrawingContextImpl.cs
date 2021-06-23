using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based drawing context.
    /// </summary>
    internal class DrawingContextImpl : IDrawingContextImpl, ISkiaDrawingContextImpl, IDrawingContextWithAcrylicLikeSupport
    {
        private IDisposable[] _disposables;
        private readonly Vector _dpi;
        private readonly Stack<PaintWrapper> _maskStack = new Stack<PaintWrapper>();
        private readonly Stack<double> _opacityStack = new Stack<double>();
        private readonly Stack<BitmapBlendingMode> _blendingModeStack = new Stack<BitmapBlendingMode>();
        private readonly Matrix? _postTransform;
        private readonly IVisualBrushRenderer _visualBrushRenderer;
        private double _currentOpacity = 1.0f;
        private BitmapBlendingMode _currentBlendingMode = BitmapBlendingMode.SourceOver;
        private readonly bool _canTextUseLcdRendering;
        private Matrix _currentTransform;
        private bool _disposed;
        private GRContext _grContext;
        public GRContext GrContext => _grContext;
        private ISkiaGpu _gpu;
        private readonly SKPaint _strokePaint = new SKPaint();
        private readonly SKPaint _fillPaint = new SKPaint();
        private readonly SKPaint _boxShadowPaint = new SKPaint();
        private static SKShader s_acrylicNoiseShader;
        private readonly ISkiaGpuRenderSession _session; 

        /// <summary>
        /// Context create info.
        /// </summary>
        public struct CreateInfo
        {
            /// <summary>
            /// Canvas to draw to.
            /// </summary>
            public SKCanvas Canvas;

            /// <summary>
            /// Surface to draw to.
            /// </summary>
            public SKSurface Surface;

            /// <summary>
            /// Dpi of drawings.
            /// </summary>
            public Vector Dpi;

            /// <summary>
            /// Visual brush renderer.
            /// </summary>
            public IVisualBrushRenderer VisualBrushRenderer;

            /// <summary>
            /// Render text without Lcd rendering.
            /// </summary>
            public bool DisableTextLcdRendering;

            /// <summary>
            /// GPU-accelerated context (optional)
            /// </summary>
            public GRContext GrContext;

            /// <summary>
            /// Skia GPU provider context (optional)
            /// </summary>
            public ISkiaGpu Gpu;

            public ISkiaGpuRenderSession CurrentSession;
        }

        /// <summary>
        /// Create new drawing context.
        /// </summary>
        /// <param name="createInfo">Create info.</param>
        /// <param name="disposables">Array of elements to dispose after drawing has finished.</param>
        public DrawingContextImpl(CreateInfo createInfo, params IDisposable[] disposables)
        {
            _dpi = createInfo.Dpi;
            _visualBrushRenderer = createInfo.VisualBrushRenderer;
            _disposables = disposables;
            _canTextUseLcdRendering = !createInfo.DisableTextLcdRendering;
            _grContext = createInfo.GrContext;
            _gpu = createInfo.Gpu;
            if (_grContext != null)
                Monitor.Enter(_grContext);
            Surface = createInfo.Surface;
            Canvas = createInfo.Canvas ?? createInfo.Surface?.Canvas;

            _session = createInfo.CurrentSession;

            if (Canvas == null)
            {
                throw new ArgumentException("Invalid create info - no Canvas provided", nameof(createInfo));
            }

            if (!_dpi.NearlyEquals(SkiaPlatform.DefaultDpi))
            {
                _postTransform =
                    Matrix.CreateScale(_dpi.X / SkiaPlatform.DefaultDpi.X, _dpi.Y / SkiaPlatform.DefaultDpi.Y);
            }

            Transform = Matrix.Identity;
        }
        
        /// <summary>
        /// Skia canvas.
        /// </summary>
        public SKCanvas Canvas { get; }
        public SKSurface Surface { get; }

        SKCanvas ISkiaDrawingContextImpl.SkCanvas => Canvas;
        SKSurface ISkiaDrawingContextImpl.SkSurface => Surface;
        GRContext ISkiaDrawingContextImpl.GrContext => _grContext;

        /// <inheritdoc />
        public void Clear(Color color)
        {
            Canvas.Clear(color.ToSKColor());
        }

        /// <inheritdoc />
        public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            var drawableImage = (IDrawableBitmapImpl)source.Item;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();

            using (var paint =
                new SKPaint
                {
                    Color = new SKColor(255, 255, 255, (byte)(255 * opacity * _currentOpacity))
                })
            {
                paint.FilterQuality = bitmapInterpolationMode.ToSKFilterQuality();
                paint.BlendMode = _currentBlendingMode.ToSKBlendMode();

                drawableImage.Draw(this, s, d, paint);
            }
        }

        /// <inheritdoc />
        public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            PushOpacityMask(opacityMask, opacityMaskRect);
            DrawBitmap(source, 1, new Rect(0, 0, source.Item.PixelSize.Width, source.Item.PixelSize.Height), destRect, BitmapInterpolationMode.Default);
            PopOpacityMask();
        }

        /// <inheritdoc />
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            using (var paint = CreatePaint(_strokePaint, pen, new Rect(p1, p2).Normalize()))
            {
                if (paint.Paint is object)
                {
                    Canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            var impl = (GeometryImpl) geometry;
            var rect = geometry.Bounds;

            using (var fill = brush != null ? CreatePaint(_fillPaint, brush, rect) : default(PaintWrapper))
            using (var stroke = pen?.Brush != null ? CreatePaint(_strokePaint, pen, rect) : default(PaintWrapper))
            {
                if (fill.Paint != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, fill.Paint);
                }

                if (stroke.Paint != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, stroke.Paint);
                }
            }
        }

        struct BoxShadowFilter : IDisposable
        {
            public SKPaint Paint;
            private SKImageFilter _filter;
            public SKClipOperation ClipOperation;

            static float SkBlurRadiusToSigma(double radius) {
                if (radius <= 0)
                    return 0.0f;
                return 0.288675f * (float)radius + 0.5f;
            }
            public static BoxShadowFilter Create(SKPaint paint, BoxShadow shadow, double opacity)
            {
                var ac = shadow.Color;

                SKImageFilter filter = null;
                filter = SKImageFilter.CreateBlur(SkBlurRadiusToSigma(shadow.Blur), SkBlurRadiusToSigma(shadow.Blur));
                var color = new SKColor(ac.R, ac.G, ac.B, (byte)(ac.A * opacity));

                paint.Reset();
                paint.IsAntialias = true;
                paint.Color = color;
                paint.ImageFilter = filter;
                
                return new BoxShadowFilter
                {
                    Paint = paint, _filter = filter,
                    ClipOperation = shadow.IsInset ? SKClipOperation.Intersect : SKClipOperation.Difference
                };
            }

            public void Dispose()
            {
                Paint.Reset();
                Paint = null;
                _filter?.Dispose();
            }
        }

        SKRect AreaCastingShadowInHole(
            SKRect hole_rect,
            float shadow_blur,
            float shadow_spread,
            float offsetX, float offsetY)
        {
            // Adapted from Chromium
            var bounds = hole_rect;

            bounds.Inflate(shadow_blur, shadow_blur);

            if (shadow_spread < 0)
                bounds.Inflate(-shadow_spread, -shadow_spread);

            var offset_bounds = bounds;
            offset_bounds.Offset(-offsetX, -offsetY);
            bounds.Union(offset_bounds);
            return bounds;
        }

        /// <inheritdoc />
        public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
        {
            if (rect.Rect.Height <= 0 || rect.Rect.Width <= 0)
                return;
            
            var rc = rect.Rect.ToSKRect();
            var isRounded = rect.IsRounded;
            var needRoundRect = rect.IsRounded;
            using var skRoundRect = needRoundRect ? new SKRoundRect() : null;

            if (needRoundRect)
                skRoundRect.SetRectRadii(rc,
                    new[]
                    {
                        rect.RadiiTopLeft.ToSKPoint(), rect.RadiiTopRight.ToSKPoint(),
                        rect.RadiiBottomRight.ToSKPoint(), rect.RadiiBottomLeft.ToSKPoint(),
                    });

            if (material != null)
            {
                using (var paint = CreateAcrylicPaint(_fillPaint, material))
                {
                    if (isRounded)
                    {
                        Canvas.DrawRoundRect(skRoundRect, paint.Paint);
                    }
                    else
                    {
                        Canvas.DrawRect(rc, paint.Paint);
                    }

                }
            }
        }

        /// <inheritdoc />
        public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadows = default)
        {
            if (rect.Rect.Height <= 0 || rect.Rect.Width <= 0)
                return;
            // Arbitrary chosen values
            // On OSX Skia breaks OpenGL context when asked to draw, e. g. (0, 0, 623, 6666600) rect
            if (rect.Rect.Height > 8192 || rect.Rect.Width > 8192)
                boxShadows = default;

            var rc = rect.Rect.ToSKRect();
            var isRounded = rect.IsRounded;
            var needRoundRect = rect.IsRounded || (boxShadows.HasInsetShadows);
            using var skRoundRect = needRoundRect ? new SKRoundRect() : null;
            if (needRoundRect)
                skRoundRect.SetRectRadii(rc,
                    new[]
                    {
                        rect.RadiiTopLeft.ToSKPoint(), rect.RadiiTopRight.ToSKPoint(),
                        rect.RadiiBottomRight.ToSKPoint(), rect.RadiiBottomLeft.ToSKPoint(),
                    });

            foreach (var boxShadow in boxShadows)
            {
                if (!boxShadow.IsEmpty && !boxShadow.IsInset)
                {
                    using (var shadow = BoxShadowFilter.Create(_boxShadowPaint, boxShadow, _currentOpacity))
                    {
                        var spread = (float)boxShadow.Spread;
                        if (boxShadow.IsInset)
                            spread = -spread;

                        Canvas.Save();
                        if (isRounded)
                        {
                            using var shadowRect = new SKRoundRect(skRoundRect);
                            if (spread != 0)
                                shadowRect.Inflate(spread, spread);
                            Canvas.ClipRoundRect(skRoundRect,
                                shadow.ClipOperation, true);
                            
                            var oldTransform = Transform;
                            Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                            Canvas.DrawRoundRect(shadowRect, shadow.Paint);
                            Transform = oldTransform;
                        }
                        else
                        {
                            var shadowRect = rc;
                            if (spread != 0)
                                shadowRect.Inflate(spread, spread);
                            Canvas.ClipRect(rc, shadow.ClipOperation);
                            var oldTransform = Transform;
                            Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                            Canvas.DrawRect(shadowRect, shadow.Paint);
                            Transform = oldTransform;
                        }

                        Canvas.Restore();
                    }
                }
            }

            if (brush != null)
            {
                using (var paint = CreatePaint(_fillPaint, brush, rect.Rect))
                {
                    if (isRounded)
                    {
                        Canvas.DrawRoundRect(skRoundRect, paint.Paint);
                    }
                    else
                    {
                        Canvas.DrawRect(rc, paint.Paint);
                    }
                }
            }

            foreach (var boxShadow in boxShadows)
            {
                if (!boxShadow.IsEmpty && boxShadow.IsInset)
                {
                    using (var shadow = BoxShadowFilter.Create(_boxShadowPaint, boxShadow, _currentOpacity))
                    {
                        var spread = (float)boxShadow.Spread;
                        var offsetX = (float)boxShadow.OffsetX;
                        var offsetY = (float)boxShadow.OffsetY;
                        var outerRect = AreaCastingShadowInHole(rc, (float)boxShadow.Blur, spread, offsetX, offsetY);

                        Canvas.Save();
                        using var shadowRect = new SKRoundRect(skRoundRect);
                        if (spread != 0)
                            shadowRect.Deflate(spread, spread);
                        Canvas.ClipRoundRect(skRoundRect,
                            shadow.ClipOperation, true);
                        
                        var oldTransform = Transform;
                        Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                        using (var outerRRect = new SKRoundRect(outerRect))
                            Canvas.DrawRoundRectDifference(outerRRect, shadowRect, shadow.Paint);
                        Transform = oldTransform;
                        Canvas.Restore();
                    }
                }
            }

            if (pen?.Brush != null)
            {
                using (var paint = CreatePaint(_strokePaint, pen, rect.Rect))
                {
                    if (paint.Paint is object)
                    {
                        if (isRounded)
                        {
                            Canvas.DrawRoundRect(skRoundRect, paint.Paint);
                        }
                        else
                        {
                            Canvas.DrawRect(rc, paint.Paint);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            using (var paint = CreatePaint(_fillPaint, foreground, text.Bounds))
            {
                var textImpl = (FormattedTextImpl) text;
                textImpl.Draw(this, Canvas, origin.ToSKPoint(), paint, _canTextUseLcdRendering);
            }
        }

        /// <inheritdoc />
        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
            using (var paintWrapper = CreatePaint(_fillPaint, foreground, new Rect(glyphRun.Size)))
            {
                var glyphRunImpl = (GlyphRunImpl)glyphRun.GlyphRunImpl;

                ConfigureTextRendering(paintWrapper);

                Canvas.DrawText(glyphRunImpl.TextBlob, (float)glyphRun.BaselineOrigin.X,
                    (float)glyphRun.BaselineOrigin.Y, paintWrapper.Paint);
            }
        }

        /// <inheritdoc />
        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return CreateRenderTarget(size, true);
        }

        /// <inheritdoc />
        public void PushClip(Rect clip)
        {
            Canvas.Save();
            Canvas.ClipRect(clip.ToSKRect());
        }

        public void PushClip(RoundedRect clip)
        {
            Canvas.Save();
            Canvas.ClipRoundRect(clip.ToSKRoundRect());
        }

        /// <inheritdoc />
        public void PopClip()
        {
            Canvas.Restore();
        }

        /// <inheritdoc />
        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(_currentOpacity);
            _currentOpacity *= opacity;
        }

        /// <inheritdoc />
        public void PopOpacity()
        {
            _currentOpacity = _opacityStack.Pop();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if(_disposed)
                return;
            try
            {
                if (_grContext != null)
                {
                    Monitor.Exit(_grContext);
                    _grContext = null;
                }

                if (_disposables != null)
                {
                    foreach (var disposable in _disposables)
                        disposable?.Dispose();
                    _disposables = null;
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void PushGeometryClip(IGeometryImpl clip)
        {
            Canvas.Save();
            Canvas.ClipPath(((GeometryImpl)clip).EffectivePath);
        }

        /// <inheritdoc />
        public void PopGeometryClip()
        {
            Canvas.Restore();
        }

        /// <inheritdoc />
        public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
        {
            _blendingModeStack.Push(_currentBlendingMode);
            _currentBlendingMode = blendingMode;
        }

        /// <inheritdoc />
        public void PopBitmapBlendMode()
        {
            _currentBlendingMode = _blendingModeStack.Pop();
        }

        public void Custom(ICustomDrawOperation custom) => custom.Render(this);

        /// <inheritdoc />
        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            // TODO: This should be disposed
            var paint = new SKPaint();

            Canvas.SaveLayer(paint);
            _maskStack.Push(CreatePaint(paint, mask, bounds, true));
        }

        /// <inheritdoc />
        public void PopOpacityMask()
        {
            using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn })
            {
                Canvas.SaveLayer(paint);
                using (var paintWrapper = _maskStack.Pop())
                {
                    Canvas.DrawPaint(paintWrapper.Paint);
                }
                Canvas.Restore();
            }

            Canvas.Restore();
        }

        /// <inheritdoc />
        public Matrix Transform
        {
            get { return _currentTransform; }
            set
            {
                if (_currentTransform == value)
                    return;

                _currentTransform = value;

                var transform = value;

                if (_postTransform.HasValue)
                {
                    transform *= _postTransform.Value;
                }

                Canvas.SetMatrix(transform.ToSKMatrix());
            }
        }

        internal void ConfigureTextRendering(PaintWrapper wrapper)
        {
            var paint = wrapper.Paint;

            paint.IsEmbeddedBitmapText = true;
            paint.SubpixelText = true;
            paint.LcdRenderText = _canTextUseLcdRendering;
        }

        /// <summary>
        /// Configure paint wrapper for using gradient brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetSize">Target size.</param>
        /// <param name="gradientBrush">Gradient brush.</param>
        private void ConfigureGradientBrush(ref PaintWrapper paintWrapper, Rect targetRect, IGradientBrush gradientBrush)
        {
            var tileMode = gradientBrush.SpreadMethod.ToSKShaderTileMode();
            var stopColors = gradientBrush.GradientStops.Select(s => s.Color.ToSKColor()).ToArray();
            var stopOffsets = gradientBrush.GradientStops.Select(s => (float)s.Offset).ToArray();
            var position = targetRect.Position.ToSKPoint();

            switch (gradientBrush)
            {
                case ILinearGradientBrush linearGradient:
                {
                    var start = position + linearGradient.StartPoint.ToPixels(targetRect.Size).ToSKPoint();
                    var end = position + linearGradient.EndPoint.ToPixels(targetRect.Size).ToSKPoint();

                    // would be nice to cache these shaders possibly?
                    using (var shader =
                        SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode))
                    {
                        paintWrapper.Paint.Shader = shader;
                    }

                    break;
                }
                case IRadialGradientBrush radialGradient:
                {
                    var center = position + radialGradient.Center.ToPixels(targetRect.Size).ToSKPoint();
                    var radius = (float)(radialGradient.Radius * targetRect.Width);

                    var origin = position + radialGradient.GradientOrigin.ToPixels(targetRect.Size).ToSKPoint();

                    if (origin.Equals(center))
                    {
                        // when the origin is the same as the center the Skia RadialGradient acts the same as D2D
                        using (var shader =
                            SKShader.CreateRadialGradient(center, radius, stopColors, stopOffsets, tileMode))
                        {
                            paintWrapper.Paint.Shader = shader;
                        }
                    }
                    else
                    {
                        // when the origin is different to the center use a two point ConicalGradient to match the behaviour of D2D

                        // reverse the order of the stops to match D2D
                        var reversedColors = new SKColor[stopColors.Length];
                        Array.Copy(stopColors, reversedColors, stopColors.Length);
                        Array.Reverse(reversedColors);

                        // and then reverse the reference point of the stops
                        var reversedStops = new float[stopOffsets.Length];
                        for (var i = 0; i < stopOffsets.Length; i++)
                        {
                            reversedStops[i] = stopOffsets[i];
                            if (reversedStops[i] > 0 && reversedStops[i] < 1)
                            {
                                reversedStops[i] = Math.Abs(1 - stopOffsets[i]);
                            }
                        }
                            
                        // compose with a background colour of the final stop to match D2D's behaviour of filling with the final color
                        using (var shader = SKShader.CreateCompose(
                            SKShader.CreateColor(reversedColors[0]),
                            SKShader.CreateTwoPointConicalGradient(center, radius, origin, 0, reversedColors, reversedStops, tileMode)
                        ))
                        {
                            paintWrapper.Paint.Shader = shader;
                        }
                    }

                    break;
                }
                case IConicGradientBrush conicGradient:
                {
                    var center = position + conicGradient.Center.ToPixels(targetRect.Size).ToSKPoint();

                    // Skia's default is that angle 0 is from the right hand side of the center point
                    // but we are matching CSS where the vertical point above the center is 0.
                    var angle = (float)(conicGradient.Angle - 90);
                    var rotation = SKMatrix.CreateRotationDegrees(angle, center.X, center.Y);

                    using (var shader = 
                        SKShader.CreateSweepGradient(center, stopColors, stopOffsets, rotation))
                    {
                        paintWrapper.Paint.Shader = shader;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Configure paint wrapper for using tile brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetSize">Target size.</param>
        /// <param name="tileBrush">Tile brush to use.</param>
        /// <param name="tileBrushImage">Tile brush image.</param>
        private void ConfigureTileBrush(ref PaintWrapper paintWrapper, Size targetSize, ITileBrush tileBrush, IDrawableBitmapImpl tileBrushImage)
        {
            var calc = new TileBrushCalculator(tileBrush, tileBrushImage.PixelSize.ToSizeWithDpi(_dpi), targetSize);
            var intermediate = CreateRenderTarget(calc.IntermediateSize, false);

            paintWrapper.AddDisposable(intermediate);

            using (var context = intermediate.CreateDrawingContext(null))
            {
                var sourceRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(96));
                var targetRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(_dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                context.DrawBitmap(
                    RefCountable.CreateUnownedNotClonable(tileBrushImage),
                    1,
                    sourceRect,
                    targetRect,
                    tileBrush.BitmapInterpolationMode);
                context.PopClip();
            }

            var tileTransform =
                tileBrush.TileMode != TileMode.None
                    ? SKMatrix.CreateTranslation(-(float)calc.DestinationRect.X, -(float)calc.DestinationRect.Y)
                    : SKMatrix.CreateIdentity();

            SKShaderTileMode tileX =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Clamp
                    : tileBrush.TileMode == TileMode.FlipX || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;

            SKShaderTileMode tileY =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Clamp
                    : tileBrush.TileMode == TileMode.FlipY || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;


            var image = intermediate.SnapshotImage();
            paintWrapper.AddDisposable(image);

            var paintTransform = default(SKMatrix);

            SKMatrix.Concat(
                ref paintTransform,
                tileTransform,
                SKMatrix.CreateScale((float)(96.0 / _dpi.X), (float)(96.0 / _dpi.Y)));

            using (var shader = image.ToShader(tileX, tileY, paintTransform))
            {
                paintWrapper.Paint.Shader = shader;
            }
        }

        /// <summary>
        /// Configure paint wrapper to use visual brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="visualBrush">Visual brush.</param>
        /// <param name="visualBrushRenderer">Visual brush renderer.</param>
        /// <param name="tileBrushImage">Tile brush image.</param>
        private void ConfigureVisualBrush(ref PaintWrapper paintWrapper, IVisualBrush visualBrush, IVisualBrushRenderer visualBrushRenderer, ref IDrawableBitmapImpl tileBrushImage)
        {
            if (_visualBrushRenderer == null)
            {
                throw new NotSupportedException("No IVisualBrushRenderer was supplied to DrawingContextImpl.");
            }

            var intermediateSize = visualBrushRenderer.GetRenderTargetSize(visualBrush);

            if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
            {
                var intermediate = CreateRenderTarget(intermediateSize, false);

                using (var ctx = intermediate.CreateDrawingContext(visualBrushRenderer))
                {
                    ctx.Clear(Colors.Transparent);

                    visualBrushRenderer.RenderVisualBrush(ctx, visualBrush);
                }

                tileBrushImage = intermediate;
                paintWrapper.AddDisposable(tileBrushImage);
            }
        }

        static SKColorFilter CreateAlphaColorFilter(double opacity)
        {
            if (opacity > 1)
                opacity = 1;
            var c = new byte[256];
            var a = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                c[i] = (byte)i;
                a[i] = (byte)(i * opacity);
            }

            return SKColorFilter.CreateTable(a, c, c, c);
        }

        static byte Blend(byte leftColor, byte leftAlpha, byte rightColor, byte rightAlpha)
        {
            var ca = leftColor / 255d;
            var aa = leftAlpha / 255d;
            var cb = rightColor / 255d;
            var ab = rightAlpha / 255d;
            var r = (ca * aa + cb * ab * (1 - aa)) / (aa + ab * (1 - aa));
            return (byte)(r * 255);
        }

        static Color Blend(Color left, Color right)
        {
            var aa = left.A / 255d;
            var ab = right.A / 255d;
            return new Color(
                (byte)((aa + ab * (1 - aa)) * 255),
                Blend(left.R, left.A, right.R, right.A),
                Blend(left.G, left.A, right.G, right.A),
                Blend(left.B, left.A, right.B, right.A)                
            );
        }

        internal PaintWrapper CreateAcrylicPaint (SKPaint paint, IExperimentalAcrylicMaterial material, bool disposePaint = false)
        {
            var paintWrapper = new PaintWrapper(paint, disposePaint);

            paint.IsAntialias = true;

            double opacity = _currentOpacity;

            var tintOpacity =
                material.BackgroundSource == AcrylicBackgroundSource.Digger ?
                material.TintOpacity : 1;

            const double noiseOpcity = 0.0225;

            var tintColor = material.TintColor;
            var tint = new SKColor(tintColor.R, tintColor.G, tintColor.B, tintColor.A);

            if (s_acrylicNoiseShader == null)
            {
                using (var stream = typeof(DrawingContextImpl).Assembly.GetManifestResourceStream("Avalonia.Skia.Assets.NoiseAsset_256X256_PNG.png"))
                using (var bitmap = SKBitmap.Decode(stream))
                {
                    s_acrylicNoiseShader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
                        .WithColorFilter(CreateAlphaColorFilter(noiseOpcity));
                }
            }

            using (var backdrop = SKShader.CreateColor(new SKColor(material.MaterialColor.R, material.MaterialColor.G, material.MaterialColor.B, material.MaterialColor.A)))
            using (var tintShader = SKShader.CreateColor(tint))
            using (var effectiveTint = SKShader.CreateCompose(backdrop, tintShader))
            using (var compose = SKShader.CreateCompose(effectiveTint, s_acrylicNoiseShader))
            {
                paint.Shader = compose;

                if (material.BackgroundSource == AcrylicBackgroundSource.Digger)
                {
                    paint.BlendMode = SKBlendMode.Src;
                }

                return paintWrapper;
            }
        }

        /// <summary>
        /// Creates paint wrapper for given brush.
        /// </summary>
        /// <param name="paint">The paint to wrap.</param>
        /// <param name="brush">Source brush.</param>
        /// <param name="targetRect">Target rect.</param>
        /// <param name="disposePaint">Optional dispose of the supplied paint.</param>
        /// <returns>Paint wrapper for given brush.</returns>
        internal PaintWrapper CreatePaint(SKPaint paint, IBrush brush, Rect targetRect, bool disposePaint = false)
        {
            var paintWrapper = new PaintWrapper(paint, disposePaint);

            paint.IsAntialias = true;

            double opacity = brush.Opacity * _currentOpacity;

            if (brush is ISolidColorBrush solid)
            {
                paint.Color = new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, (byte) (solid.Color.A * opacity));

                return paintWrapper;
            }

            paint.Color = new SKColor(255, 255, 255, (byte) (255 * opacity));

            if (brush is IGradientBrush gradient)
            {
                ConfigureGradientBrush(ref paintWrapper, targetRect, gradient);

                return paintWrapper;
            }

            var tileBrush = brush as ITileBrush;
            var visualBrush = brush as IVisualBrush;
            var tileBrushImage = default(IDrawableBitmapImpl);

            if (visualBrush != null)
            {
                ConfigureVisualBrush(ref paintWrapper, visualBrush, _visualBrushRenderer, ref tileBrushImage);
            }
            else
            {
                tileBrushImage = (IDrawableBitmapImpl)(tileBrush as IImageBrush)?.Source?.PlatformImpl.Item;
            }

            if (tileBrush != null && tileBrushImage != null)
            {
                ConfigureTileBrush(ref paintWrapper, targetRect.Size, tileBrush, tileBrushImage);
            }
            else
            {
                paint.Color = new SKColor(255, 255, 255, 0);
            }

            return paintWrapper;
        }

        /// <summary>
        /// Creates paint wrapper for given pen.
        /// </summary>
        /// <param name="paint">The paint to wrap.</param>
        /// <param name="pen">Source pen.</param>
        /// <param name="targetRect">Target rect.</param>
        /// <param name="disposePaint">Optional dispose of the supplied paint.</param>
        /// <returns></returns>
        private PaintWrapper CreatePaint(SKPaint paint, IPen pen, Rect targetRect, bool disposePaint = false)
        {
            // In Skia 0 thickness means - use hairline rendering
            // and for us it means - there is nothing rendered.
            if (pen.Thickness == 0d)
            {
                return default;
            }

            var rv = CreatePaint(paint, pen.Brush, targetRect, disposePaint);

            paint.IsStroke = true;
            paint.StrokeWidth = (float) pen.Thickness;

            // Need to modify dashes due to Skia modifying their lengths
            // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/paths/dots
            // TODO: Still something is off, dashes are now present, but don't look the same as D2D ones.

            switch (pen.LineCap)
            {
                case PenLineCap.Round:
                    paint.StrokeCap = SKStrokeCap.Round;
                    break;
                case PenLineCap.Square:
                    paint.StrokeCap = SKStrokeCap.Square;
                    break;
                default:
                    paint.StrokeCap = SKStrokeCap.Butt;
                    break;
            }

            switch (pen.LineJoin)
            {
                case PenLineJoin.Miter:
                    paint.StrokeJoin = SKStrokeJoin.Miter;
                    break;
                case PenLineJoin.Round:
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    break;
                default:
                    paint.StrokeJoin = SKStrokeJoin.Bevel;
                    break;
            }

            paint.StrokeMiter = (float) pen.MiterLimit;

            if (pen.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var srcDashes = pen.DashStyle.Dashes;
                var dashesArray = new float[srcDashes.Count];

                for (var i = 0; i < srcDashes.Count; ++i)
                {
                    dashesArray[i] = (float) srcDashes[i] * paint.StrokeWidth;
                }

                var offset = (float)(pen.DashStyle.Offset * pen.Thickness);

                var pe = SKPathEffect.CreateDash(dashesArray, offset);

                paint.PathEffect = pe;
                rv.AddDisposable(pe);
            }

            return rv;
        }

        /// <summary>
        /// Create new render target compatible with this drawing context.
        /// </summary>
        /// <param name="size">The size of the render target in DIPs.</param>
        /// <param name="isLayer">Whether the render target is being created for a layer.</param>
        /// <param name="format">Pixel format.</param>
        /// <returns></returns>
        private SurfaceRenderTarget CreateRenderTarget(Size size, bool isLayer, PixelFormat? format = null)
        {
            var pixelSize = PixelSize.FromSizeWithDpi(size, _dpi);
            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = pixelSize.Width,
                Height = pixelSize.Height,
                Dpi = _dpi,
                Format = format,
                DisableTextLcdRendering = !_canTextUseLcdRendering,
                GrContext = _grContext,
                Gpu = _gpu,
                Session = _session,
                DisableManualFbo = !isLayer,
            };

            return new SurfaceRenderTarget(createInfo);
        }        

        /// <summary>
        /// Skia cached paint state.
        /// </summary>
        private readonly struct PaintState : IDisposable
        {
            private readonly SKColor _color;
            private readonly SKShader _shader;
            private readonly SKPaint _paint;
            
            public PaintState(SKPaint paint, SKColor color, SKShader shader)
            {
                _paint = paint;
                _color = color;
                _shader = shader;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _paint.Color = _color;
                _paint.Shader = _shader;
            }
        }

        /// <summary>
        /// Skia paint wrapper.
        /// </summary>
        internal struct PaintWrapper : IDisposable
        {
            //We are saving memory allocations there
            public readonly SKPaint Paint;
            private readonly bool _disposePaint;

            private IDisposable _disposable1;
            private IDisposable _disposable2;
            private IDisposable _disposable3;

            public PaintWrapper(SKPaint paint, bool disposePaint)
            {
                Paint = paint;
                _disposePaint = disposePaint;

                _disposable1 = null;
                _disposable2 = null;
                _disposable3 = null;
            }

            public IDisposable ApplyTo(SKPaint paint)
            {
                var state = new PaintState(paint, paint.Color, paint.Shader);

                paint.Color = Paint.Color;
                paint.Shader = Paint.Shader;

                return state;
            }

            /// <summary>
            /// Add new disposable to a wrapper.
            /// </summary>
            /// <param name="disposable">Disposable to add.</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (_disposable1 == null)
                {
                    _disposable1 = disposable;
                }
                else if (_disposable2 == null)
                {
                    _disposable2 = disposable;
                }
                else if (_disposable3 == null)
                {
                    _disposable3 = disposable;
                }
                else
                {
                    Debug.Assert(false);

                    // ReSharper disable once HeuristicUnreachableCode
                    throw new InvalidOperationException(
                        "PaintWrapper disposable object limit reached. You need to add extra struct fields to support more disposables.");
                }
            }
            
            /// <inheritdoc />
            public void Dispose()
            {
                if (_disposePaint)
                {
                    Paint?.Dispose();
                }
                else
                {
                    Paint?.Reset();
                }

                _disposable1?.Dispose();
                _disposable2?.Dispose();
                _disposable3?.Dispose();
            }
        }
    }
}
