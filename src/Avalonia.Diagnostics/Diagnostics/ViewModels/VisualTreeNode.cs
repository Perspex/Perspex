using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual, TreeNode? parent, string? customName = null)
            : base(visual, parent, customName)
        {
            Children = new VisualTreeNodeCollection(this, visual);

            if (Visual is IStyleable styleable)
                IsInTemplate = styleable.TemplatedParent != null;
        }

        public bool IsInTemplate { get; }

        public override TreeNodeCollection Children { get; }

        public static VisualTreeNode[] Create(object control)
        {
            return control is IVisual visual ?
                new[] { new VisualTreeNode(visual, null) } :
                Array.Empty<VisualTreeNode>();
        }

        internal class VisualTreeNodeCollection : TreeNodeCollection
        {
            private readonly IVisual _control;
            private readonly CompositeDisposable _subscriptions = new CompositeDisposable(2);

            public VisualTreeNodeCollection(TreeNode owner, IVisual control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                _subscriptions.Dispose();
            }

            private static IObservable<PopupRoot?>? GetHostedPopupRootObservable(IVisual visual)
            {
                static IObservable<PopupRoot?> GetPopupHostObservable(
                    IPopupHostProvider popupHostProvider,
                    string? providerName = null)
                {
                    return Observable.FromEvent<IPopupHost?>(
                            x => popupHostProvider.PopupHostChanged += x,
                            x => popupHostProvider.PopupHostChanged -= x)
                        .StartWith(popupHostProvider.PopupHost)
                        .Select(popupHost =>
                        {
                            if (popupHost is IControl control)
                                return new PopupRoot(
                                    control,
                                    providerName != null ? $"{providerName} ({control.GetType().Name})" : null);

                            return (PopupRoot?)null;
                        });
                }

                return visual switch
                {
                    Popup p => GetPopupHostObservable(p),
                    Control c => Observable.CombineLatest(
                            c.GetObservable(Control.ContextFlyoutProperty),
                            c.GetObservable(Control.ContextMenuProperty),
                            c.GetObservable(FlyoutBase.AttachedFlyoutProperty),
                            c.GetObservable(ToolTipDiagnostics.ToolTipProperty),
                            (ContextFlyout, ContextMenu, AttachedFlyout, ToolTip) =>
                            {
                                if (ContextMenu != null)
                                    //Note: ContextMenus are special since all the items are added as visual children.
                                    //So we don't need to go via Popup
                                    return Observable.Return<PopupRoot?>(new PopupRoot(ContextMenu));

                                if (ContextFlyout != null)
                                    return GetPopupHostObservable(ContextFlyout, "ContextFlyout");

                                if (AttachedFlyout != null)
                                    return GetPopupHostObservable(AttachedFlyout, "AttachedFlyout");

                                if (ToolTip != null)
                                    return GetPopupHostObservable(ToolTip, "ToolTip");

                                return Observable.Return<PopupRoot?>(null);
                            })
                        .Switch(),
                    _ => null
                };
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                _subscriptions.Clear();

                if (GetHostedPopupRootObservable(_control) is { } popupRootObservable)
                {
                    VisualTreeNode? childNode = null;

                    _subscriptions.Add(
                        popupRootObservable
                            .Subscribe(popupRoot =>
                            {
                                if (popupRoot != null)
                                {
                                    childNode = new VisualTreeNode(
                                        popupRoot.Value.Root,
                                        Owner,
                                        popupRoot.Value.CustomName);

                                    nodes.Add(childNode);
                                }
                                else if (childNode != null)
                                {
                                    nodes.Remove(childNode);
                                }
                            }));
                }

                _subscriptions.Add(
                    _control.VisualChildren.ForEachItem(
                        (i, item) => nodes.Insert(i, new VisualTreeNode(item, Owner)),
                        (i, item) => nodes.RemoveAt(i),
                        () => nodes.Clear()));
            }

            private struct PopupRoot
            {
                public PopupRoot(IControl root, string? customName = null)
                {
                    Root = root;
                    CustomName = customName;
                }

                public IControl Root { get; }
                public string? CustomName { get; }
            }
        }
    }
}
