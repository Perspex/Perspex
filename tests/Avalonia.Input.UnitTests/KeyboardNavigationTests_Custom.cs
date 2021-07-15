using Avalonia.Controls;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class KeyboardNavigationTests_Custom
    {
        [Fact]
        public void Tab_Should_Custom_Navigate_Within_Children()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    (current = new Button { Content = "Button 1" }),
                    new Button { Content = "Button 2" },
                    (next = new Button { Content = "Button 3" }),
                },
                NextControl = next,
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Same(next, result);
        }

        [Fact]
        public void Right_Should_Custom_Navigate_Within_Children()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    (current = new Button { Content = "Button 1" }),
                    new Button { Content = "Button 2" },
                    (next = new Button { Content = "Button 3" }),
                },
                NextControl = next,
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Right);

            Assert.Same(next, result);
        }

        [Fact]
        public void Tab_Should_Custom_Navigate_From_Outside()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    new Button { Content = "Button 1" },
                    new Button { Content = "Button 2" },
                    (next = new Button { Content = "Button 3" }),
                },
                NextControl = next,
            };

            var root = new StackPanel
            {
                Children =
                {
                    (current = new Button { Content = "Outside" }),
                    target,
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Same(next, result);
        }

        [Fact]
        public void Tab_Should_Custom_Navigate_From_Outside_When_Wrapping()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    new Button { Content = "Button 1" },
                    new Button { Content = "Button 2" },
                    (next = new Button { Content = "Button 3" }),
                },
                NextControl = next,
            };

            var root = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    target,
                    (current = new Button { Content = "Outside" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Same(next, result);
        }

        [Fact]
        public void ShiftTab_Should_Custom_Navigate_From_Outside()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    new Button { Content = "Button 1" },
                    new Button { Content = "Button 2" },
                    (next = new Button { Content = "Button 3" }),
                },
                NextControl = next,
            };

            var root = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    (current = new Button { Content = "Outside" }),
                    target,
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Same(next, result);
        }

        [Fact]
        public void ShiftTab_Should_Navigate_Outside_When_Null_Returned_As_Next()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    new Button { Content = "Button 1" },
                    (current = new Button { Content = "Button 2" }),
                    new Button { Content = "Button 3" },
                },
            };

            var root = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    target,
                    (next = new Button { Content = "Outside" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Same(next, result);
        }

        [Fact]
        public void Tab_Should_Navigate_Outside_When_Null_Returned_As_Next()
        {
            Button current;
            Button next;
            var target = new CustomNavigatingStackPanel
            {
                Children =
                {
                    new Button { Content = "Button 1" },
                    (current = new Button { Content = "Button 2" }),
                    new Button { Content = "Button 3" },
                },
            };

            var root = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    target,
                    (next = new Button { Content = "Outside" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Same(next, result);
        }

        private class CustomNavigatingStackPanel : StackPanel, ICustomKeyboardNavigation
        {
            public bool CustomNavigates { get; set; } = true;
            public IInputElement NextControl { get; set; }

            public (bool handled, IInputElement next) GetNext(IInputElement element, NavigationDirection direction)
            {
                return (CustomNavigates, NextControl);
            }
        }
    }
}
