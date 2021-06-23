using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public sealed class ImageBrushImpl : BrushImpl
    {
        private readonly OptionalDispose<Bitmap> _bitmap;

        private readonly Avalonia.Media.Imaging.BitmapInterpolationMode _bitmapInterpolationMode;

        public ImageBrushImpl(
            ITileBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            Size targetSize)
        {
            var dpi = new Vector(target.DotsPerInch.Width, target.DotsPerInch.Height);
            var calc = new TileBrushCalculator(brush, bitmap.PixelSize.ToSizeWithDpi(dpi), targetSize);

            if (!calc.NeedsIntermediate)
            {
                _bitmap = bitmap.GetDirect2DBitmap(target);
                PlatformBrush = new BitmapBrush(
                    target,
                    _bitmap.Value,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect));
            }
            else
            {
                using (var intermediate = RenderIntermediate(target, bitmap, calc))
                {
                    PlatformBrush = new BitmapBrush(
                        target,
                        intermediate.Bitmap,
                        GetBitmapBrushProperties(brush),
                        GetBrushProperties(brush, calc.DestinationRect));
                }
            }

            _bitmapInterpolationMode = brush.BitmapInterpolationMode;
        }

        public override void Dispose()
        {
            _bitmap.Dispose();
            base.Dispose();
        }

        private static BitmapBrushProperties GetBitmapBrushProperties(ITileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = tileTransform.ToDirect2D(),
            };
        }

        private static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private BitmapRenderTarget RenderIntermediate(
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            TileBrushCalculator calc)
        {
            var result = new BitmapRenderTarget(
                target,
                CompatibleRenderTargetOptions.None,
                calc.IntermediateSize.ToSharpDX());

            using (var context = new RenderTarget(result).CreateDrawingContext(null))
            {
                var dpi = new Vector(target.DotsPerInch.Width, target.DotsPerInch.Height);
                var rect = new Rect(bitmap.PixelSize.ToSizeWithDpi(dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                
                context.DrawBitmap(RefCountable.CreateUnownedNotClonable(bitmap), 1, rect, rect, _bitmapInterpolationMode);
                context.PopClip();
            }

            return result;
        }
    }
}
