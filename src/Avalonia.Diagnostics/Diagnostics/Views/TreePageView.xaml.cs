using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    internal class TreePageView : UserControl
    {
        private readonly Panel _adorner;
        private AdornerLayer? _currentLayer;
        private TreeView _tree;

        public TreePageView()
        {
            InitializeComponent();
            _tree = this.FindControl<TreeView>("tree");
            _tree.ItemContainerGenerator.Index.Materialized += TreeViewItemMaterialized;

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

        protected void AddAdorner(object? sender, PointerEventArgs e)
        {
            var node = (TreeNode?)((Control)sender!).DataContext;
            var vm = (TreePageViewModel?)DataContext;
            if (node is null || vm is null)
            {
                return;
            }

            var visual = (Visual)node.Visual;

            _currentLayer = AdornerLayer.GetAdornerLayer(visual);

            if (_currentLayer == null ||
                _currentLayer.Children.Contains(_adorner))
            {
                return;
            }

            _currentLayer.Children.Add(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, visual);

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

        protected void RemoveAdorner(object? sender, PointerEventArgs e)
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
        }

        private void TreeViewItemMaterialized(object? sender, ItemContainerEventArgs e)
        {
            var item = (TreeViewItem)e.Containers[0].ContainerControl;
            item.TemplateApplied += TreeViewItemTemplateApplied;
        }

        private void TreeViewItemTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            var item = (TreeViewItem)sender!;

            // This depends on the default tree item template.
            // We want to handle events in the item header but exclude events coming from children.
            var header = item.FindDescendantOfType<Border>();

            Debug.Assert(header != null);

            if (header != null)
            {
                header.PointerEnter += AddAdorner;
                header.PointerLeave += RemoveAdorner;
            }

            item.TemplateApplied -= TreeViewItemTemplateApplied;
        }
    }
}
