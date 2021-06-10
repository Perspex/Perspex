using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LinuxFramebuffer;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.LinuxFramebuffer.Input.EvDev;
using Avalonia.LinuxFramebuffer.Input.LibInput;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using JetBrains.Annotations;

namespace Avalonia.LinuxFramebuffer
{
    class LinuxFramebufferPlatform
    {
        IOutputBackend _fb;
        private static readonly Stopwatch St = Stopwatch.StartNew();
        internal static uint Timestamp => (uint)St.ElapsedTicks;
        public static InternalPlatformThreadingInterface Threading;
        LinuxFramebufferPlatform(IOutputBackend backend)
        {
            _fb = backend;
        }


        void Initialize()
        {
            Threading = new InternalPlatformThreadingInterface();
            if (_fb is IGlOutputBackend gl)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(gl.PlatformOpenGlInterface);
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        }

       
        internal static LinuxFramebufferLifetime Initialize<T>(T builder, IOutputBackend outputBackend) where T : AppBuilderBase<T>, new()
        {
            var platform = new LinuxFramebufferPlatform(outputBackend);
            builder.UseSkia().UseWindowingSubsystem(platform.Initialize, "fbdev");
            return new LinuxFramebufferLifetime(platform._fb);
        }
    }

    class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {
        private readonly IOutputBackend _fb;
        [CanBeNull] private readonly IInputBackend _inputBackend;
        private TopLevel _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(IOutputBackend fb)
        {
            _fb = fb;
        }
        
        public LinuxFramebufferLifetime(IOutputBackend fb, IInputBackend input)
        {
            _fb = fb;
            _inputBackend = input;
        }
        
        public Control MainView
        {
            get => (Control)_topLevel?.Content;
            set
            {
                if (_topLevel == null)
                {
                    var inputBackend = _inputBackend;
                    if (inputBackend == null)
                    {
                        if (Environment.GetEnvironmentVariable("AVALONIA_USE_EVDEV") == "1")
                            inputBackend = EvDevBackend.CreateFromEnvironment();
                        else
                            inputBackend = new LibInputBackend();
                    }

                    var tl = new EmbeddableControlRoot(new FramebufferToplevelImpl(_fb, inputBackend));
                    tl.Prepare();
                    _topLevel = tl;
                    _topLevel.Renderer.Start();

                    if (_topLevel is IFocusScope scope)
                    {
                        FocusManager.Instance?.SetFocusScope(scope);
                    }
                }

                _topLevel.Content = value;
            }
        }

        public int ExitCode { get; private set; }
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

        public void Start(string[] args)
        {
            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));
        }
        
        public void Shutdown(int exitCode)
        {
            ExitCode = exitCode;
            var e = new ControlledApplicationLifetimeExitEventArgs(exitCode);
            Exit?.Invoke(this, e);
            ExitCode = e.ApplicationExitCode;
            _cts.Cancel();
        }
    }
}

public static class LinuxFramebufferPlatformExtensions
{
    public static int StartLinuxFbDev<T>(this T builder, string[] args, string fbdev = null, double scaling = 1)
        where T : AppBuilderBase<T>, new() =>
        StartLinuxDirect(builder, args, new FbdevOutput(fbdev) {Scaling = scaling});

    public static int StartLinuxDrm<T>(this T builder, string[] args, string card = null, double scaling = 1)
        where T : AppBuilderBase<T>, new() => StartLinuxDirect(builder, args, new DrmOutput(card) {Scaling = scaling});
    
    public static int StartLinuxDirect<T>(this T builder, string[] args, IOutputBackend backend)
        where T : AppBuilderBase<T>, new()
    {
        var lifetime = LinuxFramebufferPlatform.Initialize(builder, backend);
        builder.SetupWithLifetime(lifetime);
        lifetime.Start(args);
        builder.Instance.Run(lifetime.Token);
        return lifetime.ExitCode;
    }
}

