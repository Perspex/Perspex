using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.MicroCom;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Native
{
    class AvaloniaNativePlatform : IPlatformSettings, IWindowingPlatform
    {
        private readonly IAvaloniaNativeFactory _factory;
        private AvaloniaNativePlatformOptions _options;
        private AvaloniaNativePlatformOpenGlInterface _platformGl;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500); //TODO

        public static AvaloniaNativePlatform Initialize(IntPtr factory, AvaloniaNativePlatformOptions options)
        {
            var result =  new AvaloniaNativePlatform(MicroComRuntime.CreateProxyFor<IAvaloniaNativeFactory>(factory, true));
            result.DoInitialize(options);

            return result;
        }

        delegate IntPtr CreateAvaloniaNativeDelegate();

        public static AvaloniaNativePlatform Initialize(AvaloniaNativePlatformOptions options)
        {
            if (options.AvaloniaNativeLibraryPath != null)
            {
                var loader = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    (IDynLoader)new Win32Loader() :
                    new UnixLoader();

                var lib = loader.LoadLibrary(options.AvaloniaNativeLibraryPath);
                var proc = loader.GetProcAddress(lib, "CreateAvaloniaNative", false);
                var d = Marshal.GetDelegateForFunctionPointer<CreateAvaloniaNativeDelegate>(proc);


                return Initialize(d(), options);
            }
            else
                return Initialize(CreateAvaloniaNative(), options);
        }

        public void SetupApplicationMenuExporter ()
        {
            var exporter = new AvaloniaNativeMenuExporter(_factory);
        }

        public void SetupApplicationName ()
        {
            if(!string.IsNullOrWhiteSpace(Application.Current.Name))
            {
                _factory.MacOptions.SetApplicationTitle(Application.Current.Name);
            }
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        class GCHandleDeallocator : CallbackBase, IAvnGCHandleDeallocatorCallback
        {
            public void FreeGCHandle(IntPtr handle)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }
        
        void DoInitialize(AvaloniaNativePlatformOptions options)
        {
            _options = options;
            
            var applicationPlatform = new AvaloniaNativeApplicationPlatform();
            
            _factory.Initialize(new GCHandleDeallocator(), applicationPlatform);
            if (_factory.MacOptions != null)
            {
                var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>();

                _factory.MacOptions.SetShowInDock(macOpts?.ShowInDock != false ? 1 : 0);
                _factory.MacOptions.SetDisableDefaultApplicationMenuItems(
                    macOpts?.DisableDefaultApplicationMenuItems == true ? 1 : 0);
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(new PlatformThreadingInterface(_factory.CreatePlatformThreadingInterface()))
                .Bind<ICursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogs(_factory.CreateSystemDialogs()))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Meta))
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider())
                .Bind<IPlatformDragSource>().ToConstant(new AvaloniaNativeDragSource(_factory))
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(applicationPlatform);

            if (_options.UseGpu)
            {
                try
                {
                    AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>()
                        .ToConstant(_platformGl = new AvaloniaNativePlatformOpenGlInterface(_factory.ObtainGlDisplay()));
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory, _options, _platformGl);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }
    }

    public class AvaloniaNativeMacOptions
    {
        private readonly IAvnMacOptions _opts;
        private bool _showInDock;
        internal AvaloniaNativeMacOptions(IAvnMacOptions opts)
        {
            _opts = opts;
            ShowInDock = true;
        }

        public bool ShowInDock 
        { 
            get => _showInDock;
            set
            {
                _showInDock = value;
                _opts.SetShowInDock(value ? 1 : 0);
            }
        }
    }
}
