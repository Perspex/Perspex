using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Moq;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.UnitTests
{
    public class MockWindowingPlatform : IWindowingPlatform
    {
        private static readonly Size s_screenSize = new Size(1280, 1024);
        private readonly Func<IWindowImpl> _windowImpl;
        private readonly Func<IWindowBaseImpl, IPopupImpl> _popupImpl;

        public MockWindowingPlatform(
            Func<IWindowImpl> windowImpl = null,
            Func<IWindowBaseImpl, IPopupImpl> popupImpl = null )
        {
            _windowImpl = windowImpl;
            _popupImpl = popupImpl;
        }

        public static Mock<IWindowImpl> CreateWindowMock()
        {
            var windowImpl = new Mock<IWindowImpl>();
            var position = new PixelPoint();
            var clientSize = new Size(800, 600);

            windowImpl.SetupAllProperties();
            windowImpl.Setup(x => x.ClientSize).Returns(() => clientSize);
            windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(s_screenSize);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.Screen).Returns(CreateScreenMock().Object);
            windowImpl.Setup(x => x.Position).Returns(() => position);
            SetupToplevel(windowImpl);

            windowImpl.Setup(x => x.CreatePopup()).Returns(() =>
            {
                return CreatePopupMock(windowImpl.Object).Object;
            });

            windowImpl.Setup(x => x.Dispose()).Callback(() =>
            {
                windowImpl.Object.Closed?.Invoke();
            });

            windowImpl.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback<PixelPoint>(x =>
            {
                position = x;
                windowImpl.Object.PositionChanged?.Invoke(x);
            });

            windowImpl.Setup(x => x.Resize(It.IsAny<Size>(), It.IsAny<PlatformResizeReason>()))
                .Callback<Size, PlatformResizeReason>((x, y) =>
            {
                clientSize = x.Constrain(s_screenSize);
                windowImpl.Object.Resized?.Invoke(clientSize, y);
            });

            windowImpl.Setup(x => x.Show(true)).Callback(() =>
            {
                windowImpl.Object.Activated?.Invoke();
            });

            return windowImpl;
        }

        public static Mock<IPopupImpl> CreatePopupMock(IWindowBaseImpl parent)
        {
            var popupImpl = new Mock<IPopupImpl>();
            var clientSize = new Size();

            var positionerHelper = new ManagedPopupPositionerPopupImplHelper(parent, (pos, size, scale) =>
            {
                clientSize = size.Constrain(s_screenSize);
                popupImpl.Object.PositionChanged?.Invoke(pos);
                popupImpl.Object.Resized?.Invoke(clientSize, PlatformResizeReason.Unspecified);
            });
            
            var positioner = new ManagedPopupPositioner(positionerHelper);

            popupImpl.SetupAllProperties();
            popupImpl.Setup(x => x.ClientSize).Returns(() => clientSize);
            popupImpl.Setup(x => x.MaxAutoSizeHint).Returns(s_screenSize);
            popupImpl.Setup(x => x.RenderScaling).Returns(1);
            popupImpl.Setup(x => x.PopupPositioner).Returns(positioner);
            
            SetupToplevel(popupImpl);
            
            return popupImpl;
        }

        public static Mock<IScreenImpl> CreateScreenMock()
        {
            var screenImpl = new Mock<IScreenImpl>();
            var bounds = new PixelRect(0, 0, (int)s_screenSize.Width, (int)s_screenSize.Height);
            var screen = new Screen(96, bounds, bounds, true);
            screenImpl.Setup(x => x.AllScreens).Returns(new[] { screen });
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            return screenImpl;
        }

        public IWindowImpl CreateWindow()
        {
            if (_windowImpl is object)
            {
                return _windowImpl();
            }
            else
            {
                var mock = CreateWindowMock();

                if (_popupImpl is object)
                {
                    mock.Setup(x => x.CreatePopup()).Returns(() => _popupImpl(mock.Object));
                }

                return mock.Object;
            }
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        private static void SetupToplevel<T>(Mock<T> mock) where T : class, ITopLevelImpl
        {
            mock.SetupGet(x => x.MouseDevice).Returns(new MouseDevice());
        }
    }
}
