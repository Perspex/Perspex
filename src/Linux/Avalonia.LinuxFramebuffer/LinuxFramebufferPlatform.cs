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
using Avalonia.LinuxFramebuffer.Input.LibInput;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.LinuxFramebuffer
{
    public class LinuxFramebufferPlatform
    {
        private static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();
        private static readonly MouseDevice MouseDevice = new MouseDevice();
        private static readonly InternalPlatformThreadingInterface Threading = new InternalPlatformThreadingInterface();

        private static readonly Stopwatch St = Stopwatch.StartNew();
        
        internal static uint Timestamp => (uint)St.ElapsedTicks;

        private LinuxFramebufferPlatform()
        {
        }

        private void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IMouseDevice>().ToConstant(MouseDevice)
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
        }

        internal static LinuxFramebufferLifetime Initialize<T>(T builder, IOutputBackend outputBackend) where T : AppBuilderBase<T>, new()
        {
            return Initialize(builder, outputBackend.Name, _ => new LinuxFramebufferLifetime(outputBackend));
        }

        public static ITopLevelImpl CreateTopLevelImpl(IOutputBackend output, IInputBackend input)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return new FramebufferToplevelImpl(output, input);
        }

        public static TLifetime Initialize<T, TLifetime>(T builder, string displaySubsystemName,
            Func<LinuxFramebufferPlatform, TLifetime> lifetimeFactory)
            where T : AppBuilderBase<T>, new() 
            where TLifetime : IControlledApplicationLifetime
        {
            var platform = new LinuxFramebufferPlatform();

            builder
                .UseSkia()
                .UseWindowingSubsystem(platform.Initialize, displaySubsystemName);

            return lifetimeFactory(platform);
        }
    }
    
    internal class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {
        private readonly IOutputBackend _fb;
        private TopLevel _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(IOutputBackend fb)
        {
            _fb = fb;
        }

        public Control MainView
        {
            get => (Control) _topLevel?.Content;
            set
            {
                if (_topLevel == null)
                {

                    var tl = new EmbeddableControlRoot(LinuxFramebufferPlatform.CreateTopLevelImpl(_fb, new LibInputBackend()));
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

            if (_fb is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

public static class LinuxFramebufferPlatformExtensions
{
    public static int StartLinuxFbDev<T>(this T builder, string[] args, string fbdev = null, double scaling = 1)
        where T : AppBuilderBase<T>, new() =>
        StartLinuxDirect(builder, args, new FbdevOutput(fbdev) { Scaling = scaling });

    public static int StartLinuxDrm<T>(this T builder, string[] args, string card = null, double scaling = 1)
        where T : AppBuilderBase<T>, new()
    {
        var platform = new DrmPlatform(card, scaling);
        return StartLinuxDirect(builder, args, platform.CreateOutput());
    }

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
