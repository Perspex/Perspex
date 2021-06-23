using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    public static class SkiaSharpExtensions
    {
        public static SKFilterQuality ToSKFilterQuality(this BitmapInterpolationMode interpolationMode)
        {
            switch (interpolationMode)
            {
                case BitmapInterpolationMode.LowQuality:
                    return SKFilterQuality.Low;
                case BitmapInterpolationMode.MediumQuality:
                    return SKFilterQuality.Medium;
                case BitmapInterpolationMode.HighQuality:
                    return SKFilterQuality.High;
                case BitmapInterpolationMode.Default:
                    return SKFilterQuality.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interpolationMode), interpolationMode, null);
            }
        }

        public static SKBlendMode ToSKBlendMode(this BitmapBlendingMode blendingMode)
        {
            switch (blendingMode)
            {
                case BitmapBlendingMode.SourceOver:
                    return SKBlendMode.SrcOver;
                case BitmapBlendingMode.Source:
                    return SKBlendMode.Src;
                case BitmapBlendingMode.SourceIn:
                    return SKBlendMode.SrcIn;
                case BitmapBlendingMode.SourceOut:
                    return SKBlendMode.SrcOut;
                case BitmapBlendingMode.SourceAtop:
                    return SKBlendMode.SrcATop;
                case BitmapBlendingMode.Destination:
                    return SKBlendMode.Dst;
                case BitmapBlendingMode.DestinationIn:
                    return SKBlendMode.DstIn;
                case BitmapBlendingMode.DestinationOut:
                    return SKBlendMode.DstOut;
                case BitmapBlendingMode.DestinationOver:
                    return SKBlendMode.DstOver;
                case BitmapBlendingMode.DestinationAtop:
                    return SKBlendMode.DstATop;
                case BitmapBlendingMode.Xor:
                    return SKBlendMode.Xor;
                case BitmapBlendingMode.Plus:
                    return SKBlendMode.Plus;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blendingMode), blendingMode, null);
            }
        }

        public static SKPoint ToSKPoint(this Point p)
        {
            return new SKPoint((float)p.X, (float)p.Y);
        }
        
        public static SKPoint ToSKPoint(this Vector p)
        {
            return new SKPoint((float)p.X, (float)p.Y);
        }

        public static SKRect ToSKRect(this Rect r)
        {
            return new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
        }

        public static SKRoundRect ToSKRoundRect(this RoundedRect r)
        {
            var rc = r.Rect.ToSKRect();
            var result = new SKRoundRect();

            result.SetRectRadii(rc,
                   new[]
                   {
                        r.RadiiTopLeft.ToSKPoint(), r.RadiiTopRight.ToSKPoint(),
                        r.RadiiBottomRight.ToSKPoint(), r.RadiiBottomLeft.ToSKPoint(),
                   });            

            return result;
        }

        public static Rect ToAvaloniaRect(this SKRect r)
        {
            return new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

        public static SKMatrix ToSKMatrix(this Matrix m)
        {
            var sm = new SKMatrix
            {
                ScaleX = (float)m.M11,
                SkewX = (float)m.M21,
                TransX = (float)m.M31,
                SkewY = (float)m.M12,
                ScaleY = (float)m.M22,
                TransY = (float)m.M32,
                Persp0 = 0,
                Persp1 = 0,
                Persp2 = 1
            };

            return sm;
        }

        public static SKColor ToSKColor(this Media.Color c)
        {
            return new SKColor(c.R, c.G, c.B, c.A);
        }

        public static SKColorType ToSkColorType(this PixelFormat fmt)
        {
            if (fmt == PixelFormat.Rgb565)
                return SKColorType.Rgb565;
            if (fmt == PixelFormat.Bgra8888)
                return SKColorType.Bgra8888;
            if (fmt == PixelFormat.Rgba8888)
                return SKColorType.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static PixelFormat ToPixelFormat(this SKColorType fmt)
        {
            if (fmt == SKColorType.Rgb565)
                return PixelFormat.Rgb565;
            if (fmt == SKColorType.Bgra8888)
                return PixelFormat.Bgra8888;
            if (fmt == SKColorType.Rgba8888)
                return PixelFormat.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static SKAlphaType ToSkAlphaType(this AlphaFormat fmt)
        {
            return fmt switch
            {
                AlphaFormat.Premul => SKAlphaType.Premul,
                AlphaFormat.Unpremul => SKAlphaType.Unpremul,
                AlphaFormat.Opaque => SKAlphaType.Opaque,
                _ => throw new ArgumentException($"Unknown alpha format: {fmt}")
            };
        }

        public static AlphaFormat ToAlphaFormat(this SKAlphaType fmt)
        {
            return fmt switch
            {
                SKAlphaType.Premul => AlphaFormat.Premul,
                SKAlphaType.Unpremul => AlphaFormat.Unpremul,
                SKAlphaType.Opaque => AlphaFormat.Opaque,
                _ => throw new ArgumentException($"Unknown alpha format: {fmt}")
            };
        }

        public static SKShaderTileMode ToSKShaderTileMode(this Media.GradientSpreadMethod m)
        {
            switch (m)
            {
                default:
                case Media.GradientSpreadMethod.Pad: return SKShaderTileMode.Clamp;
                case Media.GradientSpreadMethod.Reflect: return SKShaderTileMode.Mirror;
                case Media.GradientSpreadMethod.Repeat: return SKShaderTileMode.Repeat;
            }
        }

        public static SKTextAlign ToSKTextAlign(this TextAlignment a)
        {
            switch (a)
            {
                default:
                case TextAlignment.Left: return SKTextAlign.Left;
                case TextAlignment.Center: return SKTextAlign.Center;
                case TextAlignment.Right: return SKTextAlign.Right;
            }
        }

        public static TextAlignment ToAvalonia(this SKTextAlign a)
        {
            switch (a)
            {
                default:
                case SKTextAlign.Left: return TextAlignment.Left;
                case SKTextAlign.Center: return TextAlignment.Center;
                case SKTextAlign.Right: return TextAlignment.Right;
            }
        }

        public static FontStyle ToAvalonia(this SKFontStyleSlant slant)
        {
            return slant switch
            {
                SKFontStyleSlant.Upright => FontStyle.Normal,
                SKFontStyleSlant.Italic => FontStyle.Italic,
                SKFontStyleSlant.Oblique => FontStyle.Oblique,
                _ => throw new ArgumentOutOfRangeException(nameof (slant), slant, null)
            };
        }

        public static SKPath Clone(this SKPath src)
        {
            return src != null ? new SKPath(src) : null;
        }
    }
}
