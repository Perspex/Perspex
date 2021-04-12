using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SharpGen.Runtime;
using BitmapInterpolationMode = Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode;
using GlyphRun = Avalonia.Media.GlyphRun;
using TextAlignment = Avalonia.Media.TextAlignment;

namespace Avalonia
{
    public static class Direct2DApplicationExtensions
    {
        public static T UseDirect2D1<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseRenderingSubsystem(Direct2D1.Direct2D1Platform.Initialize, "Direct2D1");
            return builder;
        }
    }
}

namespace Avalonia.Direct2D1
{
    public class Direct2D1Platform : IPlatformRenderInterface
    {
        private static readonly Direct2D1Platform s_instance = new Direct2D1Platform();

        public static Vortice.Direct3D11.ID3D11Device Direct3D11Device { get; private set; }

        public static Vortice.Direct2D1.ID2D1Factory1 Direct2D1Factory { get; private set; }

        public static Vortice.Direct2D1.ID2D1Device Direct2D1Device { get; private set; }

        public static Vortice.DirectWrite.IDWriteFactory1 DirectWriteFactory { get; private set; }

        public static Vortice.WIC.IWICImagingFactory ImagingFactory { get; private set; }

        public static Vortice.DXGI.IDXGIDevice1 DxgiDevice { get; private set; }

        private static readonly object s_initLock = new object();
        private static bool s_initialized = false;

        internal static void InitializeDirect2D()
        {
            lock (s_initLock)
            {
                if (s_initialized)
                {
                    return;
                }

                Vortice.Direct2D1.ID2D1Factory1 direct2D1Factory;
                Result result;

#if DEBUG
                result = Vortice.Direct2D1.D2D1.D2D1CreateFactory(
                    Vortice.Direct2D1.FactoryType.MultiThreaded, new Vortice.Direct2D1.FactoryOptions
                    {
                        DebugLevel = Vortice.Direct2D1.DebugLevel.Error
                    },
                    out direct2D1Factory
                );
#endif

                if (result.Failure)
                {
                    result = Vortice.Direct2D1.D2D1.D2D1CreateFactory(
                        Vortice.Direct2D1.FactoryType.MultiThreaded, new Vortice.Direct2D1.FactoryOptions
                        {
                            DebugLevel = Vortice.Direct2D1.DebugLevel.None
                        },
                        out direct2D1Factory
                    );
                }

                if (result.Success)
                    Direct2D1Factory = direct2D1Factory;
                else
                    throw new AvaloniaInternalException("Failed to initialize Direct2D");

                result = Vortice.DirectWrite.DWrite.DWriteCreateFactory(out Vortice.DirectWrite.IDWriteFactory1 factory);

                if (result.Success)
                    DirectWriteFactory = factory;
                else
                    throw new AvaloniaInternalException("Failed to initialize DirectWrite");

                ImagingFactory = new Vortice.WIC.IWICImagingFactory();

                var featureLevels = new[]
                {
                    Vortice.Direct3D.FeatureLevel.Level_11_1,
                    Vortice.Direct3D.FeatureLevel.Level_11_0,
                    Vortice.Direct3D.FeatureLevel.Level_10_1,
                    Vortice.Direct3D.FeatureLevel.Level_10_0,
                    Vortice.Direct3D.FeatureLevel.Level_9_3,
                    Vortice.Direct3D.FeatureLevel.Level_9_2,
                    Vortice.Direct3D.FeatureLevel.Level_9_1,
                };

                result = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
                    IntPtr.Zero,
                    Vortice.Direct3D.DriverType.Hardware,
                    Vortice.Direct3D11.DeviceCreationFlags.BgraSupport |
                    Vortice.Direct3D11.DeviceCreationFlags.VideoSupport,
                    featureLevels,
                    out var d3d11Device
                );

                if (result.Success)
                    Direct3D11Device = d3d11Device;
                else
                    throw new AvaloniaInternalException("Failed to initialize Direct3D 11");

                DxgiDevice = Direct3D11Device.QueryInterface<Vortice.DXGI.IDXGIDevice1>();

                Direct2D1Device = direct2D1Factory.CreateDevice(DxgiDevice);

                s_initialized = true;
            }
        }

        public static void Initialize()
        {
#if DEBUG
            Configuration.EnableObjectTracking = true;
#endif

            InitializeDirect2D();
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
                .Bind<IFontManagerImpl>().ToConstant(new FontManagerImpl())
                .Bind<ITextShaperImpl>().ToConstant(new TextShaperImpl());
            Configuration.EnableReleaseOnFinalizer = true;
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextImpl(
                text,
                typeface,
                fontSize,
                textAlignment,
                wrapping,
                constraint,
                spans);
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var s in surfaces)
            {
                if (s is IPlatformHandle nativeWindow)
                {
                    if (nativeWindow.HandleDescriptor != "HWND")
                    {
                        throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from " +
                                                        nativeWindow.HandleDescriptor);
                    }

                    return new HwndRenderTarget(nativeWindow);
                }
                if (s is IExternalDirect2DRenderTargetSurface external)
                {
                    return new ExternalRenderTarget(external);
                }

                if (s is IFramebufferPlatformSurface fb)
                {
                    return new FramebufferShimRenderTarget(fb);
                }
            }
            throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from any of provided surfaces");
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return new WicRenderTargetBitmapImpl(size, dpi);
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new WriteableWicBitmapImpl(size, dpi, format, alphaFormat);
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new EllipseGeometryImpl(rect);
        public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => new LineGeometryImpl(p1, p2);
        public IGeometryImpl CreateRectangleGeometry(Rect rect) => new RectangleGeometryImpl(rect);
        public IStreamGeometryImpl CreateStreamGeometry() => new StreamGeometryImpl();

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new WicBitmapImpl(fileName);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new WicBitmapImpl(stream);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WicBitmapImpl(stream, width, true, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WicBitmapImpl(stream, height, false, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            // https://github.com/sharpdx/SharpDX/issues/959 blocks implementation.
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new WicBitmapImpl(format, alphaFormat, data, size, dpi, stride);
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun, out double width)
        {
            var glyphTypeface = (GlyphTypefaceImpl)glyphRun.GlyphTypeface.PlatformImpl;

            var glyphCount = glyphRun.GlyphIndices.Length;

            var run = new Vortice.DirectWrite.GlyphRun
            {
                FontFace = glyphTypeface.FontFace,
                FontEmSize = (float)glyphRun.FontRenderingEmSize
            };

            var indices = new ushort[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                indices[i] = glyphRun.GlyphIndices[i];
            }

            run.GlyphIndices = indices;

            run.GlyphAdvances = new float[glyphCount];

            width = 0;

            var scale = (float)(glyphRun.FontRenderingEmSize / glyphTypeface.DesignEmHeight);

            if (glyphRun.GlyphAdvances.IsEmpty)
            {
                for (var i = 0; i < glyphCount; i++)
                {
                    var advance = glyphTypeface.GetGlyphAdvance(glyphRun.GlyphIndices[i]) * scale;

                    run.GlyphAdvances[i] = advance;

                    width += advance;
                }
            }
            else
            {
                for (var i = 0; i < glyphCount; i++)
                {
                    var advance = (float)glyphRun.GlyphAdvances[i];

                    run.GlyphAdvances[i] = advance;

                    width += advance;
                }
            }

            if (glyphRun.GlyphOffsets.IsEmpty)
            {
                return new GlyphRunImpl(run);
            }

            run.GlyphOffsets = new Vortice.DirectWrite.GlyphOffset[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                var (x, y) = glyphRun.GlyphOffsets[i];

                run.GlyphOffsets[i] = new Vortice.DirectWrite.GlyphOffset
                {
                    AdvanceOffset = (float)x,
                    AscenderOffset = (float)y
                };
            }

            return new GlyphRunImpl(run);
        }

        public bool SupportsIndividualRoundRects => false;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Bgra8888;
    }
}
