using System;
using Moq;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Styling;
using Avalonia.Themes.Default;
using Avalonia.Rendering;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Reflection;
using Avalonia.Animation;

namespace Avalonia.UnitTests
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new AssetLoader(),
            platform: new AppBuilder().RuntimePlatform,
            renderInterface: new MockPlatformRenderInterface(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            styler: new Styler(),
            theme: () => CreateDefaultTheme(),
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            fontManagerImpl: new MockFontManagerImpl(),
            textShaperImpl: new MockTextShaperImpl(),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            assetLoader: new AssetLoader(),
            renderInterface: new MockPlatformRenderInterface(),
            fontManagerImpl: new MockFontManagerImpl(),
            textShaperImpl: new MockTextShaperImpl());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockStyler = new TestServices(
            styler: Mock.Of<IStyler>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true));

        public static readonly TestServices MockWindowingPlatform = new TestServices(
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: new KeyboardNavigationHandler(),
            inputManager: new InputManager());

        public static readonly TestServices RealStyler = new TestServices(
            styler: new Styler());

        public TestServices(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IGlobalClock globalClock = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderTimer renderLoop = null,
            IScheduler scheduler = null,
            ICursorFactory standardCursorFactory = null,
            IStyler styler = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null)
        {
            AssetLoader = assetLoader;
            FocusManager = focusManager;
            GlobalClock = globalClock;
            InputManager = inputManager;
            KeyboardDevice = keyboardDevice;
            KeyboardNavigation = keyboardNavigation;
            MouseDevice = mouseDevice;
            Platform = platform;
            RenderInterface = renderInterface;
            FontManagerImpl = fontManagerImpl;
            TextShaperImpl = textShaperImpl;
            Scheduler = scheduler;
            StandardCursorFactory = standardCursorFactory;
            Styler = styler;
            Theme = theme;
            ThreadingInterface = threadingInterface;
            WindowImpl = windowImpl;
            WindowingPlatform = windowingPlatform;
        }

        public IAssetLoader AssetLoader { get; }
        public IInputManager InputManager { get; }
        public IFocusManager FocusManager { get; }
        public IGlobalClock GlobalClock { get; }
        public Func<IKeyboardDevice> KeyboardDevice { get; }
        public IKeyboardNavigationHandler KeyboardNavigation { get; }
        public Func<IMouseDevice> MouseDevice { get; }
        public IRuntimePlatform Platform { get; }
        public IPlatformRenderInterface RenderInterface { get; }
        public IFontManagerImpl FontManagerImpl { get; }
        public ITextShaperImpl TextShaperImpl { get; }
        public IScheduler Scheduler { get; }
        public ICursorFactory StandardCursorFactory { get; }
        public IStyler Styler { get; }
        public Func<Styles> Theme { get; }
        public IPlatformThreadingInterface ThreadingInterface { get; }
        public IWindowImpl WindowImpl { get; }
        public IWindowingPlatform WindowingPlatform { get; }

        public TestServices With(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IGlobalClock globalClock = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderTimer renderLoop = null,
            IScheduler scheduler = null,
            ICursorFactory standardCursorFactory = null,
            IStyler styler = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null)
        {
            return new TestServices(
                assetLoader: assetLoader ?? AssetLoader,
                focusManager: focusManager ?? FocusManager,
                globalClock: globalClock ?? GlobalClock,
                inputManager: inputManager ?? InputManager,
                keyboardDevice: keyboardDevice ?? KeyboardDevice,
                keyboardNavigation: keyboardNavigation ?? KeyboardNavigation,
                mouseDevice: mouseDevice ?? MouseDevice,
                platform: platform ?? Platform,
                renderInterface: renderInterface ?? RenderInterface,
                fontManagerImpl: fontManagerImpl ?? FontManagerImpl,
                textShaperImpl: textShaperImpl ?? TextShaperImpl,
                scheduler: scheduler ?? Scheduler,
                standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
                styler: styler ?? Styler,
                theme: theme ?? Theme,
                threadingInterface: threadingInterface ?? ThreadingInterface,
                windowingPlatform: windowingPlatform ?? WindowingPlatform,
                windowImpl: windowImpl ?? WindowImpl);
        }

        private static Styles CreateDefaultTheme()
        {
            var result = new Styles
            {
                new DefaultTheme(),
            };

            var baseLight = (IStyle)AvaloniaXamlLoader.Load(
                new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml"));
            result.Add(baseLight);

            return result;
        }

        private static IPlatformRenderInterface CreateRenderInterfaceMock()
        {
            return Mock.Of<IPlatformRenderInterface>(x =>
                x.CreateFormattedText(
                    It.IsAny<string>(),
                    It.IsAny<Typeface>(),
                    It.IsAny<double>(),
                    It.IsAny<TextAlignment>(),
                    It.IsAny<TextWrapping>(),
                    It.IsAny<Size>(),
                    It.IsAny<IReadOnlyList<FormattedTextStyleSpan>>()) == Mock.Of<IFormattedTextImpl>() &&
                x.CreateStreamGeometry() == Mock.Of<IStreamGeometryImpl>(
                    y => y.Open() == Mock.Of<IStreamGeometryContextImpl>()));
        }
    }

    public class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder()
            : base(new StandardRuntimePlatform(),
                  builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType()
                      ?.GetTypeInfo().Assembly))
        {
        }

        protected override bool CheckSetup => false;
    }
}
