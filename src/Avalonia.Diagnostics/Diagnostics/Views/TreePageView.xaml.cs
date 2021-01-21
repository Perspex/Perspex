using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Views
{
    internal class TreePageView : UserControl
    {
        private readonly Panel _adorner;
        private AdornerLayer _currentLayer;
        private TreeView _tree;

        public TreePageView()
        {
            this.InitializeComponent();
            _tree.TreeContainerPrepared += TreeContainerPrepared;

            _adorner = new Panel
            {
                ClipToBounds = false,
                Children =
                {
                    //Padding frame
                    new Border { BorderBrush = new SolidColorBrush(Colors.Green, 0.5) },
                    //Content frame
                    new Border { Background = new SolidColorBrush(Color.FromRgb(160, 197, 232), 0.5) },
                    //Margin frame
                    new Border { BorderBrush = new SolidColorBrush(Colors.Yellow, 0.5) }
                },
            };
        }

        protected void AddAdorner(object sender, PointerEventArgs e)
        {
            var node = (TreeNode)((Control)sender).DataContext;
            var visual = (Visual)node.Visual;

            _currentLayer = AdornerLayer.GetAdornerLayer(visual);

            if (_currentLayer == null ||
                _currentLayer.Children.Contains(_adorner))
            {
                return;
            }

            _currentLayer.Children.Add(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, visual);

            var vm = (TreePageViewModel) DataContext;

            if (vm.MainView.ShouldVisualizeMarginPadding)
            {
                var paddingBorder = (Border)_adorner.Children[0];
                paddingBorder.BorderThickness = visual.GetValue(PaddingProperty);

                var contentBorder = (Border)_adorner.Children[1];
                contentBorder.Margin = visual.GetValue(PaddingProperty);

                var marginBorder = (Border)_adorner.Children[2];
                marginBorder.BorderThickness = visual.GetValue(MarginProperty);
                marginBorder.Margin = InvertThickness(visual.GetValue(MarginProperty));
            }
        }

        private static Thickness InvertThickness(Thickness input)
        {
            return new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
        }

        protected void RemoveAdorner(object sender, PointerEventArgs e)
        {
            foreach (var border in _adorner.Children.OfType<Border>())
            {
                border.Margin = default;
                border.Padding = default;
                border.BorderThickness = default;
            }

            _currentLayer?.Children.Remove(_adorner);
            _currentLayer = null;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _tree = this.FindControl<TreeView>("tree");
        }

        private void TreeContainerPrepared(object sender, TreeElementPreparedEventArgs e)
        {
            var item = (TreeViewItem)e.Element;
            item.TemplateApplied += TreeViewItemTemplateApplied;
        }

        private void TreeViewItemTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            var headerPresenter = item.HeaderPresenter;
            headerPresenter.ApplyTemplate();

            var header = headerPresenter.Child;
            header.PointerEnter += AddAdorner;
            header.PointerLeave += RemoveAdorner;
            item.TemplateApplied -= TreeViewItemTemplateApplied;
        }
    }
}
