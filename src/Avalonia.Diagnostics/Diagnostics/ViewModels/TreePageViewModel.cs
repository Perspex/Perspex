using System;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDisposable
    {
        private TreeNode? _selectedNode;
        private ControlDetailsViewModel? _details;

        public TreePageViewModel(MainViewModel mainView, TreeNode[] nodes)
        {
            MainView = mainView;
            Nodes = nodes;

            PropertiesFilter = new FilterViewModel();
            PropertiesFilter.RefreshFilter += (s, e) => Details?.PropertiesView.Refresh();

            SettersFilter = new FilterViewModel();
            SettersFilter.RefreshFilter += (s, e) => Details?.UpdateStyleFilters();
        }

        public MainViewModel MainView { get; }

        public FilterViewModel PropertiesFilter { get; }

        public FilterViewModel SettersFilter { get; }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode? SelectedNode
        {
            get => _selectedNode;
            private set
            {
                if (RaiseAndSetIfChanged(ref _selectedNode, value))
                {
                    Details = value != null ?
                        new ControlDetailsViewModel(this, value.Visual) :
                        null;
                    Details?.UpdateStyleFilters();
                }
            }
        }

        public ControlDetailsViewModel? Details
        {
            get => _details;
            private set
            {
                var oldValue = _details;

                if (RaiseAndSetIfChanged(ref _details, value))
                {
                    oldValue?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            foreach (var node in Nodes)
            {
                node.Dispose();
            }

            _details?.Dispose();
        }

        public TreeNode? FindNode(IControl control)
        {
            foreach (var node in Nodes)
            {
                var result = FindNode(node, control);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public void SelectControl(IControl control)
        {
            var node = default(TreeNode);

            while (node == null && control != null)
            {
                node = FindNode(control);

                if (node == null)
                {
                    control = control.GetVisualParent<IControl>();
                }
            }

            if (node != null)
            {
                SelectedNode = node;
                ExpandNode(node.Parent);
            }
        }

        private void ExpandNode(TreeNode? node)
        {
            if (node != null)
            {
                node.IsExpanded = true;
                ExpandNode(node.Parent);
            }
        }

        private TreeNode? FindNode(TreeNode node, IControl control)
        {
            if (node.Visual == control)
            {
                return node;
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var result = FindNode(child, control);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
