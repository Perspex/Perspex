using System;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class TreeNode : ViewModelBase, IDisposable
    {
        private readonly IDisposable? _classesSubscription;
        private string _classes;
        private bool _isExpanded;

        protected TreeNode(IVisual visual, TreeNode? parent, string? customName = null)
        {
            _classes = string.Empty;
            Parent = parent;
            Type = customName ?? visual.GetType().Name;
            Visual = visual;
            FontWeight = IsRoot ? FontWeight.Bold : FontWeight.Normal;

            if (visual is IControl control)
            {
                ElementName = control.Name;

                var removed = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                    x => control.DetachedFromLogicalTree += x,
                    x => control.DetachedFromLogicalTree -= x);
                var classesChanged = Observable.FromEventPattern<
                        NotifyCollectionChangedEventHandler,
                        NotifyCollectionChangedEventArgs>(
                        x => control.Classes.CollectionChanged += x,
                        x => control.Classes.CollectionChanged -= x)
                    .TakeUntil(removed);

                _classesSubscription = classesChanged.Select(_ => Unit.Default)
                    .StartWith(Unit.Default)
                    .Subscribe(_ =>
                    {
                        if (control.Classes.Count > 0)
                        {
                            Classes = "(" + string.Join(" ", control.Classes) + ")";
                        }
                        else
                        {
                            Classes = string.Empty;
                        }
                    });
            }
        }

        private bool IsRoot => Visual is TopLevel || 
                               Visual is Popup ||
                               Visual is ContextMenu ||
                               Visual is IPopupHost;

        public FontWeight FontWeight { get; }

        public abstract TreeNodeCollection Children
        {
            get;
        }

        public string Classes
        {
            get { return _classes; }
            private set { RaiseAndSetIfChanged(ref _classes, value); }
        }

        public string? ElementName
        {
            get;
        }

        public IVisual Visual
        {
            get;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { RaiseAndSetIfChanged(ref _isExpanded, value); }
        }

        public TreeNode? Parent
        {
            get;
        }

        public string Type
        {
            get;
            private set;
        }

        public void Dispose()
        {
            _classesSubscription?.Dispose();
            Children.Dispose();
        }
    }
}
