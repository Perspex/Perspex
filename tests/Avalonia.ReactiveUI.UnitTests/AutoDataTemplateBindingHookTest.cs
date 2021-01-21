using Xunit;
using ReactiveUI;
using Avalonia.ReactiveUI;
using Avalonia.UnitTests;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Controls.Presenters;
using Splat;
using System.Threading.Tasks;
using System;
using Avalonia.Layout;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AutoDataTemplateBindingHookTest
    {
        public class NestedViewModel : ReactiveObject { }

        public class NestedView : ReactiveUserControl<NestedViewModel> { }

        public class ExampleViewModel : ReactiveObject 
        {
            public ObservableCollection<NestedViewModel> Items { get; } = new ObservableCollection<NestedViewModel>();
        }

        public class ExampleView : ReactiveUserControl<ExampleViewModel>
        {
            public ItemsControl List { get; } = new ItemsControl
            {
                Template = GetTemplate()
            };

            public ExampleView(Action<ItemsControl> adjustItemsControl = null)
            {
                adjustItemsControl?.Invoke(List);
                List.ApplyTemplate();
                List.Presenter.ApplyTemplate();
                
                Content = List;
                ViewModel = new ExampleViewModel();
                this.OneWayBind(ViewModel, x => x.Items, x => x.List.Items);
            }
        }

        public AutoDataTemplateBindingHookTest() 
        {
            Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.Register(() => new NestedView(), typeof(IViewFor<NestedViewModel>));
        }

        [Fact]
        public void Should_Apply_Data_Template_Binding_When_No_Template_Is_Set()
        {
            var view = new ExampleView();
            Assert.NotNull(view.List.ItemTemplate);
            Assert.IsType<FuncDataTemplate<object>>(view.List.ItemTemplate);
        }

        [Fact]
        public void Should_Use_ViewModelViewHost_As_Data_Template_By_Default()
        {
            var view = new ExampleView();
            view.ViewModel.Items.Add(new NestedViewModel());

            view.List.Template = GetTemplate();
            view.List.ApplyTemplate();
            Layout(view.List.Presenter);

            var child = view.List.Presenter.RealizedElements.First();
            var container = (ContentPresenter) child;
            container.UpdateChild();

            Assert.IsType<ViewModelViewHost>(container.Child);
        }

        [Fact]
        public void ViewModelViewHost_Should_Resolve_And_Embedd_Appropriate_View_Model()
        {
            var view = new ExampleView();
            view.ViewModel.Items.Add(new NestedViewModel());

            view.List.Template = GetTemplate();
            view.List.ApplyTemplate();
            Layout(view.List.Presenter);

            var child = view.List.Presenter.RealizedElements.First();
            var container = (ContentPresenter) child;
            container.UpdateChild();

            var host = (ViewModelViewHost) container.Child;
            Assert.IsType<NestedViewModel>(host.ViewModel);
            Assert.IsType<NestedViewModel>(host.DataContext);

            host.DataContext = "changed context";
            Assert.IsType<string>(host.ViewModel);
            Assert.IsType<string>(host.DataContext);
        }

        [Fact]
        public void Should_Not_Override_Data_Template_Binding_When_Item_Template_Is_Set()
        {
            var view = new ExampleView(control => control.ItemTemplate = GetItemTemplate());
            Assert.NotNull(view.List.ItemTemplate);
            Assert.IsType<FuncDataTemplate<TextBlock>>(view.List.ItemTemplate);
        }

        [Fact]
        public void Should_Not_Use_View_Model_View_Host_When_Item_Template_Is_Set()
        {
            var view = new ExampleView(control => control.ItemTemplate = GetItemTemplate());
            view.ViewModel.Items.Add(new NestedViewModel());

            Layout(view.List.Presenter);

            var child = view.List.Presenter.RealizedElements.ElementAt(0);
            var container = (ContentPresenter) child;
            container.UpdateChild();

            Assert.IsType<TextBlock>(container.Child);
        }
        
        [Fact]
        public void Should_Not_Use_View_Model_View_Host_When_Data_Templates_Are_Not_Empty()
        {
            var view = new ExampleView(control => control.DataTemplates.Add(GetItemTemplate()));
            view.ViewModel.Items.Add(new NestedViewModel());

            Layout(view.List.Presenter);

            var child = view.List.Presenter.RealizedElements.ElementAt(0);
            var container = (ContentPresenter) child;
            container.UpdateChild();

            Assert.IsType<TextBlock>(container.Child);
        }

        private static void Layout(ILayoutable target)
        {
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
        }

        private static FuncDataTemplate GetItemTemplate()
        {
            return new FuncDataTemplate<TextBlock>((parent, scope) => new TextBlock());
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) => new Border
            {
                Background = new Media.SolidColorBrush(0xffffffff),
                Child = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsViewProperty] = parent[~ItemsControl.ItemsViewProperty],
                }.RegisterInNameScope(scope)
            });
        }
    }
}
