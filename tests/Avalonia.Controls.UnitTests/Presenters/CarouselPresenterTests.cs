using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Moq;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class CarouselPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new Mock<IItemsPresenterHost>();
            var target = new CarouselPresenter();

            target.SetValue(Control.TemplatedParentProperty, host.Object);

            host.Verify(x => x.RegisterItemsPresenter(target));
        }

        [Fact]
        public void ItemTemplate_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new Carousel();
            var target = new CarouselPresenter
            {
                [StyledElement.TemplatedParentProperty] = parent,
            };

            Assert.NotNull(parent.ItemContainerGenerator);
            Assert.Same(parent.ItemContainerGenerator, target.ElementFactory);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Initially_Materialize_Selected_Container(bool isVirtualized)
        {
            using var app = Start();

            var (target, _) = CreateTarget(isVirtualized);

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Changing_SelectedIndex_Should_Show_Page(bool isVirtualized)
        {
            using var app = Start();

            var (target, root) = CreateTarget(isVirtualized);

            AssertState(target);

            target.SelectedIndex = 1;

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_At_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Insert(1, "baz");

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_Before_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Insert(0, "baz");

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_After_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items);

            items.Insert(1, "baz");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Selected_Item(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 0);

            items.RemoveAt(0);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Selected_Item_At_End(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.RemoveAt(1);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Item_After_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 0);

            items.RemoveAt(1);

            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Replacing_Selected_Item(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items[1] = "baz";

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Resetting_Items(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items);

            items.Clear();
            Assert.False(target.IsMeasureValid);

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Reassigning_Items(bool isVirtualized)
        {
            using var app = Start();
            var (target, root) = CreateTarget(isVirtualized);
            var owner = (Carousel)target.TemplatedParent!;

            AssertState(target);

            owner.Items = new[] { "new", "items" };
            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Replacing_Non_SelectedItem(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items[0] = "baz";

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Moving_SelectedItem(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Move(1, 0);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Switching_IsVirtualized_Should_Reset_Containers(bool isVirtualized)
        {
            using var app = Start();
            var (target, root) = CreateTarget(isVirtualized);

            target.IsVirtualized = !target.IsVirtualized;

            Assert.False(target.IsMeasureValid);
            Assert.Empty(target.Children);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Moving_SelectedIndex_Forwards_Initiates_Transition(bool isVirtualized)
        {
            using var app = Start();
            var transition = new Mock<IPageTransition>();
            var (target, root) = CreateTarget(isVirtualized, transition: transition.Object);

            transition.Verify(x =>
                x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>()),
                Times.Never);

            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(x =>
                x.Start(
                    It.Is<Visual>(x => IsContainer(x, "foo")),
                    It.Is<Visual>(x => IsContainer(x, "bar")), 
                    true),
                Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Moving_SelectedIndex_Backwards_Initiates_Transition(bool isVirtualized)
        {
            using var app = Start();
            var transition = new Mock<IPageTransition>();
            var (target, root) = CreateTarget(
                isVirtualized,
                selectedIndex: 1,
                transition: transition.Object);

            transition.Verify(x =>
                x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>()),
                Times.Never);

            target.SelectedIndex = 0;
            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(x =>
                x.Start(
                    It.Is<Visual>(x => IsContainer(x, "bar")),
                     It.Is<Visual>(x => IsContainer(x, "foo")),
                    false),
                Times.Once);
        }

        [Fact]
        public void Completing_Transition_Removes_Control_When_Virtualized()
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var transition = new Mock<IPageTransition>();
            var (target, root) = CreateTarget(true, transition: transition.Object);
            var tcs = new TaskCompletionSource<object?>();

            transition.Setup(x => x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            tcs.SetResult(null);
            sync.ExecutePostedCallbacks();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Changing_SelectedIndex_During_A_Transition_Queues_New_Transition(bool isVirtualized)
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var transition = new Mock<IPageTransition>();
            var items = new[] { "foo", "bar", "baz" };
            var (target, root) = CreateTarget(isVirtualized, items: items, transition: transition.Object);
            var tcs = new TaskCompletionSource<object?>();

            transition.Setup(x => x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            target.SelectedIndex = 2;
            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(x =>
                x.Start(
                    It.Is<Visual>(x => IsContainer(x, "foo")),
                    It.Is<Visual>(x => IsContainer(x, "bar")),
                    true),
                Times.Once);

            tcs.SetResult(null);
            sync.ExecutePostedCallbacks();

            tcs = new TaskCompletionSource<object?>();
            transition.Setup(x => x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(x =>
                x.Start(
                    It.Is<Visual>(x => IsContainer(x, "bar")),
                    It.Is<Visual>(x => IsContainer(x, "baz")),
                    true),
                Times.Once);
            tcs.SetResult(null);
            sync.ExecutePostedCallbacks();

            AssertState(target);
        }

        private static (CarouselPresenter, TestRoot) CreateTarget(
            bool isVirtualized,
            IEnumerable? items = null,
            int? selectedIndex = null,
            IPageTransition? transition = null)
        {
            RuntimeHelpers.RunClassConstructor(typeof(CarouselPresenter).TypeHandle);

            var carousel = new Carousel
            {
                Items = items ?? new[] { "foo", "bar" },
                SelectedIndex = selectedIndex ?? 0,
                IsVirtualized = isVirtualized,
                PageTransition = transition,
                Template = new FuncControlTemplate<Carousel>((c, ns) =>
                    new CarouselPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [!ItemsPresenter.ItemsViewProperty] = c[!ItemsControl.ItemsViewProperty],
                        [!CarouselPresenter.IsVirtualizedProperty] = c[!Carousel.IsVirtualizedProperty],
                        [!CarouselPresenter.PageTransitionProperty] = c[!Carousel.PageTransitionProperty],
                        [!CarouselPresenter.SelectedIndexProperty] = c[!Carousel.SelectedIndexProperty],
                    }),
            };

            var root = new TestRoot(carousel);
            root.LayoutManager.ExecuteInitialLayoutPass();

            return ((CarouselPresenter)carousel.Presenter!, root);
        }

        private static void AssertState(CarouselPresenter target)
        {
            if (target.IsVirtualized)
                AssertVirtualizedState(target);
            else
                AssertNonVirtualizedState(target);
        }

        private static void AssertVirtualizedState(CarouselPresenter target)
        {
            var items = (ItemsSourceView)target.ItemsView!;

            if (items.Count > 0)
            {
                var index = target.SelectedIndex;
                var content = items[index];
                var child = Assert.Single(target.Children);
                var presenter = Assert.IsType<ContentPresenter>(child);
                var visible = Assert.Single(target.RealizedElements.Where(x => x.IsVisible));

                Assert.Same(child, visible);
                Assert.Equal(content, presenter.Content);
                Assert.Single(target.RealizedElements);
            }
            else
            {
                Assert.Empty(target.Children);
                Assert.Empty(target.RealizedElements);
            }
        }

        private static void AssertNonVirtualizedState(CarouselPresenter target)
        {
            var items = (ItemsSourceView)target.ItemsView!;

            Assert.True(target.Children.Count <= items.Count);
            Assert.True(target.RealizedElements.Count() <= items.Count);

            for (var i = 0; i < items?.Count; ++i)
            {
                var content = items[i];
                var container = target.TryGetElement(i);
                var presenter = Assert.IsType<ContentPresenter>(container);

                Assert.Equal(i == target.SelectedIndex, presenter.IsVisible);
                Assert.Equal(content, presenter.Content);
                Assert.Equal(i, target.GetElementIndex(presenter));
            }
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface;
            return UnitTestApplication.Start(services);
        }

        private static bool IsContainer(Visual v, string expected)
        {
            return v is ContentPresenter cp &&
                cp.DataContext is string s &&
                s == expected;
        }
    }
}
