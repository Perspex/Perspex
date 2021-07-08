using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.FreeDesktop.DBusIme;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.X11;
using Avalonia.X11.Glx;
using Avalonia.X11.NativeDialogs;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    class AvaloniaX11Platform : IWindowingPlatform
    {
        private Lazy<KeyboardDevice> _keyboardDevice = new Lazy<KeyboardDevice>(() => new AvaloniaX11KeyboardDevice());
        public KeyboardDevice KeyboardDevice => _keyboardDevice.Value;
        public Dictionary<IntPtr, X11PlatformThreading.EventHandler> Windows =
            new Dictionary<IntPtr, X11PlatformThreading.EventHandler>();
        public XI2Manager XI2;
        public X11Info Info { get; private set; }
        public IX11Screens X11Screens { get; private set; }
        public IScreenImpl Screens { get; private set; }
        public X11PlatformOptions Options { get; private set; }
        public IntPtr OrphanedWindow { get; private set; }
        public X11Globals Globals { get; private set; }
        [DllImport("libc")]
        static extern void setlocale(int type, string s);
        public void Initialize(X11PlatformOptions options)
        {
            Options = options;
            
            bool useXim = false;
            if (EnableIme(options))
            {
                // Attempt to configure DBus-based input method and check if we can fall back to XIM
                if (!X11DBusImeHelper.DetectAndRegister() && ShouldUseXim())
                    useXim = true;
            }

            // XIM doesn't work at all otherwise
            if (useXim)
                setlocale(0, "");

            XInitThreads();
            Display = XOpenDisplay(IntPtr.Zero);
            DeferredDisplay = XOpenDisplay(IntPtr.Zero);
            OrphanedWindow = XCreateSimpleWindow(Display, XDefaultRootWindow(Display), 0, 0, 1, 1, 0, IntPtr.Zero,
                IntPtr.Zero);
            if (Display == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");
            XError.Init();
            
            Info = new X11Info(Display, DeferredDisplay, useXim);
            Globals = new X11Globals(this);
            //TODO: log
            if (options.UseDBusMenu)
                DBusHelper.TryInitialize();
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new X11PlatformThreading(this))
                .Bind<IRenderTimer>().ToConstant(new SleepLoopRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Control))
                .Bind<IKeyboardDevice>().ToFunc(() => KeyboardDevice)
                .Bind<ICursorFactory>().ToConstant(new X11CursorFactory(Display))
                .Bind<IClipboard>().ToConstant(new X11Clipboard(this))
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettingsStub())
                .Bind<IPlatformIconLoader>().ToConstant(new X11IconLoader(Info))
                .Bind<ISystemDialogImpl>().ToConstant(new GtkSystemDialog())
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider());
            
            X11Screens = Avalonia.X11.X11Screens.Init(this);
            Screens = new X11Screens(X11Screens);
            if (Info.XInputVersion != null)
            {
                var xi2 = new XI2Manager();
                if (xi2.Init(this))
                    XI2 = xi2;
            }

            if (options.UseGpu)
            {
                if (options.UseEGL)
                    EglPlatformOpenGlInterface.TryInitialize();
                else
                    GlxPlatformOpenGlInterface.TryInitialize(Info, Options.GlProfiles);
            }

            
        }

        public IntPtr DeferredDisplay { get; set; }
        public IntPtr Display { get; set; }
        public IWindowImpl CreateWindow()
        {
            return new X11Window(this, null);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        bool EnableIme(X11PlatformOptions options)
        {
            // Disable if explicitly asked by user
            var avaloniaImModule = Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE");
            if (avaloniaImModule == "none")
                return false;
            
            // Use value from options when specified
            if (options.EnableIme.HasValue)
                return options.EnableIme.Value;
            
            // Automatically enable for CJK locales
            var lang = Environment.GetEnvironmentVariable("LANG");
            var isCjkLocale = lang != null &&
                              (lang.Contains("zh")
                               || lang.Contains("ja")
                               || lang.Contains("vi")
                               || lang.Contains("ko"));

            return isCjkLocale;
        }
        
        bool ShouldUseXim()
        {
            // Check if we are forbidden from using IME
            if (Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE") == "none"
                || Environment.GetEnvironmentVariable("GTK_IM_MODULE") == "none"
                || Environment.GetEnvironmentVariable("QT_IM_MODULE") == "none")
                return true;
            
            // Check if XIM is configured
            var modifiers = Environment.GetEnvironmentVariable("XMODIFIERS");
            if (modifiers == null)
                return false;
            if (modifiers.Contains("@im=none") || modifiers.Contains("@im=null"))
                return false;
            if (!modifiers.Contains("@im="))
                return false;
            
            // Check if we are configured to use it
            if (Environment.GetEnvironmentVariable("GTK_IM_MODULE") == "xim"
                || Environment.GetEnvironmentVariable("QT_IM_MODULE") == "xim"
                || Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE") == "xim")
                return true;
            
            return false;
        }
    }
}

namespace Avalonia
{

    public class X11PlatformOptions
    {
        public bool UseEGL { get; set; }
        public bool UseGpu { get; set; } = true;
        public bool OverlayPopups { get; set; }
        public bool UseDBusMenu { get; set; }
        public bool UseDeferredRendering { get; set; } = true;
        public bool? EnableIme { get; set; }

        public IList<GlVersion> GlProfiles { get; set; } = new List<GlVersion>
        {
            new GlVersion(GlProfileType.OpenGL, 4, 0),
            new GlVersion(GlProfileType.OpenGL, 3, 2),
            new GlVersion(GlProfileType.OpenGL, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 3, 2),
            new GlVersion(GlProfileType.OpenGLES, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 2, 0)
        };

        public IList<string> GlxRendererBlacklist { get; set; } = new List<string>
        {
            // llvmpipe is a software GL rasterizer. If it's returned by glGetString,
            // that usually means that something in the system is horribly misconfigured
            // and sometimes attempts to use GLX might cause a segfault
            "llvmpipe"
        };
        public string WmClass { get; set; } = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "AvaloniaApplication";
        public bool? EnableMultiTouch { get; set; }
    }
    public static class AvaloniaX11PlatformExtensions
    {
        public static T UseX11<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() =>
                new AvaloniaX11Platform().Initialize(AvaloniaLocator.Current.GetService<X11PlatformOptions>() ??
                                                     new X11PlatformOptions()));
            return builder;
        }

        public static void InitializeX11Platform(X11PlatformOptions options = null) =>
            new AvaloniaX11Platform().Initialize(options ?? new X11PlatformOptions());
    }

}
