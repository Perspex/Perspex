using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly TopLevel _root;
        private readonly TreePageViewModel _logicalTree;
        private readonly TreePageViewModel _visualTree;
        private readonly EventsPageViewModel _events;
        private readonly IDisposable _pointerOverSubscription;
        private ViewModelBase _content;
        private int _selectedTab;
        private string? _focusedControl;
        private string? _pointerOverElement;
        private bool _shouldVisualizeMarginPadding = true;
        private bool _shouldVisualizeDirtyRects;
        private bool _showFpsOverlay;

#nullable disable
        // Remove "nullable disable" after MemberNotNull will work on our CI.
        public MainViewModel(TopLevel root)
#nullable restore
        {
            _root = root;
            _logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root));
            _visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root));
            _events = new EventsPageViewModel(this);

            UpdateFocusedControl();
            KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
            SelectedTab = 0;
            _pointerOverSubscription = root.GetObservable(TopLevel.PointerOverElementProperty)
                .Subscribe(x => PointerOverElement = x?.GetType().Name);
            Console = new ConsoleViewModel(UpdateConsoleContext);
        }

        public bool ShouldVisualizeMarginPadding
        {
            get => _shouldVisualizeMarginPadding;
            set => RaiseAndSetIfChanged(ref _shouldVisualizeMarginPadding, value);
        }
        
        public bool ShouldVisualizeDirtyRects
        {
            get => _shouldVisualizeDirtyRects;
            set
            {
                _root.Renderer.DrawDirtyRects = value;
                RaiseAndSetIfChanged(ref _shouldVisualizeDirtyRects, value);
            }
        }

        public void ToggleVisualizeDirtyRects()
        {
            ShouldVisualizeDirtyRects = !ShouldVisualizeDirtyRects;
        }

        public void ToggleVisualizeMarginPadding()
        {
            ShouldVisualizeMarginPadding = !ShouldVisualizeMarginPadding;
        }

        public bool ShowFpsOverlay
        {
            get => _showFpsOverlay;
            set
            {
                _root.Renderer.DrawFps = value;
                RaiseAndSetIfChanged(ref _showFpsOverlay, value);
            }
        }

        public void ToggleFpsOverlay()
        {
            ShowFpsOverlay = !ShowFpsOverlay;
        }

        public ConsoleViewModel Console { get; }

        public ViewModelBase Content
        {
            get { return _content; }
            // [MemberNotNull(nameof(_content))]
            private set
            {
                if (_content is TreePageViewModel oldTree &&
                    value is TreePageViewModel newTree &&
                    oldTree?.SelectedNode?.Visual is IControl control)
                {
                    // HACK: We want to select the currently selected control in the new tree, but
                    // to select nested nodes in TreeView, currently the TreeView has to be able to
                    // expand the parent nodes. Because at this point the TreeView isn't visible,
                    // this will fail unless we schedule the selection to run after layout.
                    DispatcherTimer.RunOnce(
                        () =>
                        {
                            try
                            {
                                newTree.SelectControl(control);
                            }
                            catch { }
                        },
                        TimeSpan.FromMilliseconds(0),
                        DispatcherPriority.ApplicationIdle);
                }

                RaiseAndSetIfChanged(ref _content, value);
            }
        }

        public int SelectedTab
        {
            get { return _selectedTab; }
            // [MemberNotNull(nameof(_content))]
            set
            {
                _selectedTab = value;

                switch (value)
                {
                    case 1:
                        Content = _visualTree;
                        break;
                    case 2:
                        Content = _events;
                        break;
                    default:
                        Content = _logicalTree;
                        break;
                }

                RaisePropertyChanged();
            }
        }

        public string? FocusedControl
        {
            get { return _focusedControl; }
            private set { RaiseAndSetIfChanged(ref _focusedControl, value); }
        }

        public string? PointerOverElement
        {
            get { return _pointerOverElement; }
            private set { RaiseAndSetIfChanged(ref _pointerOverElement, value); }
        }

        private void UpdateConsoleContext(ConsoleContext context)
        {
            context.root = _root;

            if (Content is TreePageViewModel tree)
            {
                context.e = tree.SelectedNode?.Visual;
            }
        }

        public void SelectControl(IControl control)
        {
            var tree = Content as TreePageViewModel;

            tree?.SelectControl(control);
        }

        public void EnableSnapshotStyles(bool enable)
        {
            if (Content is TreePageViewModel treeVm && treeVm.Details != null)
            {
                treeVm.Details.SnapshotStyles = enable;
            }
        }

        public void Dispose()
        {
            KeyboardDevice.Instance.PropertyChanged -= KeyboardPropertyChanged;
            _pointerOverSubscription.Dispose();
            _logicalTree.Dispose();
            _visualTree.Dispose();
            _root.Renderer.DrawDirtyRects = false;
            _root.Renderer.DrawFps = false;
        }

        private void UpdateFocusedControl()
        {
            FocusedControl = KeyboardDevice.Instance.FocusedElement?.GetType().Name;
        }

        private void KeyboardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
            {
                UpdateFocusedControl();
            }
        }

        public void RequestTreeNavigateTo(IControl control, bool isVisualTree)
        {
            var tree = isVisualTree ? _visualTree : _logicalTree;

            var node = tree.FindNode(control);

            if (node != null)
            {
                SelectedTab = isVisualTree ? 1 : 0;

                tree.SelectControl(control);
            }
        }
    }
}
