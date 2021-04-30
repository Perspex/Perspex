using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Avalonia.Direct2D1;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using Query = SharpDX.Direct3D11.Query;
using QueryType = SharpDX.Direct3D11.QueryType;
using RenderTarget = SharpDX.Direct2D1.RenderTarget;
using Surface = SharpDX.DXGI.Surface;
using Usage = SharpDX.Direct3D9.Usage;

namespace Avalonia.Win32.Interop.Wpf
{
    class Direct2DImageSurface : IExternalDirect2DRenderTargetSurface, IDisposable
    {
        class SwapBuffer: IDisposable
        {
            private readonly Query _event;
            private readonly SharpDX.Direct3D11.Resource _resource;
            private readonly SharpDX.Direct3D11.Resource _sharedResource;
            public SharpDX.Direct3D9.Surface Texture { get; }
            public RenderTarget Target { get;}
            public IntSize Size { get; }

            public SwapBuffer(IntSize size, Vector dpi)
            {
                int width = (int) size.Width;
                int height = (int) size.Height;
                _event = new Query(s_dxDevice, new QueryDescription {Type = QueryType.Event});
                using (var texture = new Texture2D(s_dxDevice, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SampleDescription(2, 0),
                    BindFlags = BindFlags.RenderTarget,
                }))
                using (var surface = texture.QueryInterface<Surface>())
                
                {
                    _resource = texture.QueryInterface<SharpDX.Direct3D11.Resource>();
                    
                    Target = new RenderTarget(Direct2D1Platform.Direct2D1Factory, surface,
                        new RenderTargetProperties
                        {
                            DpiX = (float) dpi.X,
                            DpiY = (float) dpi.Y,
                            MinLevel = FeatureLevel.Level_10,
                            PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),

                        });
                }
                using (var texture = new Texture2D(s_dxDevice, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.RenderTarget|BindFlags.ShaderResource,
                    OptionFlags = ResourceOptionFlags.Shared,
                }))
                using (var resource = texture.QueryInterface<SharpDX.DXGI.Resource>())
                {
                    _sharedResource = texture.QueryInterface<SharpDX.Direct3D11.Resource>();
                    var handle = resource.SharedHandle;
                    using (var texture9 = new Texture(s_d3DDevice, texture.Description.Width,
                        texture.Description.Height, 1,
                        Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8, Pool.Default, ref handle))
                        Texture = texture9.GetSurfaceLevel(0);
                }
                Size = size;
            }

            public void Dispose()
            {
                Texture?.Dispose();
                Target?.Dispose();
                _resource?.Dispose();
                _sharedResource?.Dispose();
                _event?.Dispose();
            }

            public void Flush()
            {
                s_dxDevice.ImmediateContext.ResolveSubresource(_resource, 0, _sharedResource, 0, Format.B8G8R8A8_UNorm);
                s_dxDevice.ImmediateContext.Flush();
                s_dxDevice.ImmediateContext.End(_event);
                s_dxDevice.ImmediateContext.GetData(_event).Dispose();
            }
        }

        private D3DImage _image;
        private SwapBuffer _backBuffer;
        private readonly WpfTopLevelImpl _impl;
        private static Device s_dxDevice;
        private static Direct3DEx s_d3DContext;
        private static DeviceEx s_d3DDevice;
        private Vector _oldDpi;


        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();
        void EnsureDirectX()
        {
            if(s_d3DDevice != null)
                return;
            s_d3DContext = new Direct3DEx();

            SharpDX.Direct3D9.PresentParameters presentparams = new SharpDX.Direct3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = GetDesktopWindow(),
                PresentationInterval = PresentInterval.Default
            };
            s_dxDevice = s_dxDevice ?? AvaloniaLocator.Current.GetService<SharpDX.DXGI.Device>()
                             .QueryInterface<SharpDX.Direct3D11.Device>();
            s_d3DDevice = new DeviceEx(s_d3DContext, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, presentparams);

        }

        public Direct2DImageSurface(WpfTopLevelImpl impl)
        {
            _impl = impl;
        }

        public RenderTarget GetOrCreateRenderTarget()
        {
            EnsureDirectX();
            var scale = _impl.GetScaling();
            var size = new IntSize(_impl.ActualWidth * scale.X, _impl.ActualHeight * scale.Y);
            var dpi = scale * 96;

            if (_backBuffer!=null && _backBuffer.Size == size)
                return _backBuffer.Target;

            if (_image == null || _oldDpi.X != dpi.X || _oldDpi.Y != dpi.Y)
            {
                _image = new D3DImage(dpi.X, dpi.Y);
                _oldDpi = dpi;
            }
            _impl.ImageSource = _image;
            
            RemoveAndDispose(ref _backBuffer);
            if (size == default(IntSize))
            {
                _image.Lock();
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                _image.Unlock();
                return null;
            }
            _backBuffer = new SwapBuffer(size, dpi);

            return _backBuffer.Target;
        }

        void RemoveAndDispose<T>(ref T d) where T : IDisposable
        {
            d?.Dispose();
            d = default(T);
        }

        void Swap()
        {
            _backBuffer.Flush();
            _image.Lock();
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBuffer?.Texture?.NativePointer ?? IntPtr.Zero, true);
            _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            _image.Unlock();
        }

        public void DestroyRenderTarget()
        {
            RemoveAndDispose(ref _backBuffer);
        }

        public void BeforeDrawing()
        {
            
        }

        public void AfterDrawing() => Swap();
        public void Dispose()
        {
            RemoveAndDispose(ref _backBuffer);
        }
    }
}
