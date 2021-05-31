using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop.Automation;
using AAP = Avalonia.Automation.Provider;

#nullable enable

namespace Avalonia.Win32.Automation
{
    [ComVisible(true)]
    internal partial class AutomationNode : MarshalByRefObject,
        IAutomationNode,
        IRawElementProviderSimple,
        IRawElementProviderSimple2,
        IRawElementProviderFragment,
        IRawElementProviderAdviseEvents,
        IInvokeProvider
    {
        private static Dictionary<AutomationProperty, UiaPropertyId> s_propertyMap = new Dictionary<AutomationProperty, UiaPropertyId>()
        {
            { AutomationElementIdentifiers.BoundingRectangleProperty, UiaPropertyId.BoundingRectangle },
            { AutomationElementIdentifiers.ClassNameProperty, UiaPropertyId.ClassName },
            { AutomationElementIdentifiers.NameProperty, UiaPropertyId.Name },
            { ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, UiaPropertyId.ExpandCollapseExpandCollapseState },
            { RangeValuePatternIdentifiers.IsReadOnlyProperty, UiaPropertyId.RangeValueIsReadOnly},
            { RangeValuePatternIdentifiers.MaximumProperty, UiaPropertyId.RangeValueMaximum },
            { RangeValuePatternIdentifiers.MinimumProperty, UiaPropertyId.RangeValueMinimum },
            { RangeValuePatternIdentifiers.ValueProperty, UiaPropertyId.RangeValueValue },
            { ScrollPatternIdentifiers.HorizontallyScrollableProperty, UiaPropertyId.ScrollHorizontallyScrollable },
            { ScrollPatternIdentifiers.HorizontalScrollPercentProperty, UiaPropertyId.ScrollHorizontalScrollPercent },
            { ScrollPatternIdentifiers.HorizontalViewSizeProperty, UiaPropertyId.ScrollHorizontalViewSize },
            { ScrollPatternIdentifiers.VerticallyScrollableProperty, UiaPropertyId.ScrollVerticallyScrollable },
            { ScrollPatternIdentifiers.VerticalScrollPercentProperty, UiaPropertyId.ScrollVerticalScrollPercent },
            { ScrollPatternIdentifiers.VerticalViewSizeProperty, UiaPropertyId.ScrollVerticalViewSize },
            { SelectionPatternIdentifiers.CanSelectMultipleProperty, UiaPropertyId.SelectionCanSelectMultiple },
            { SelectionPatternIdentifiers.IsSelectionRequiredProperty, UiaPropertyId.SelectionIsSelectionRequired },
            { SelectionPatternIdentifiers.SelectionProperty, UiaPropertyId.SelectionSelection },
        };

        private readonly int[] _runtimeId;
        private int _raiseFocusChanged;
        private int _raisePropertyChanged;

        public AutomationNode(AutomationPeer peer)
        {
            _runtimeId = new int[] { 3, GetHashCode() };
            Peer = peer;
        }

        protected AutomationNode(Func<IAutomationNode, AutomationPeer> peerGetter)
        {
            _runtimeId = new int[] { 3, GetHashCode() };
            Peer = peerGetter(this);
        }

        public AutomationPeer Peer { get; protected set; }
        public IAutomationNodeFactory Factory => AutomationNodeFactory.Instance;

        public Rect BoundingRectangle
        {
            get => InvokeSync(() =>
            {
                if (GetRoot()?.Node is RootAutomationNode root)
                    return root.ToScreen(Peer.GetBoundingRectangle());
                return default;
            });
        }

        public virtual IRawElementProviderFragmentRoot? FragmentRoot
        {
            get => InvokeSync(() => GetRoot())?.Node as IRawElementProviderFragmentRoot;
        }

        public virtual IRawElementProviderSimple? HostRawElementProvider => null;
        public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;

        public void ChildrenChanged()
        {
            UiaCoreProviderApi.UiaRaiseStructureChangedEvent(
                this,
                StructureChangeType.ChildrenInvalidated,
                null,
                0);
        }

        public void PropertyChanged(AutomationProperty property, object? oldValue, object? newValue) 
        {
            if (_raisePropertyChanged > 0 && s_propertyMap.TryGetValue(property, out var id))
            {
                UiaCoreProviderApi.UiaRaiseAutomationPropertyChangedEvent(this, (int)id, oldValue, newValue);
            }
        }

        [return: MarshalAs(UnmanagedType.IUnknown)]
        public virtual object? GetPatternProvider(int patternId)
        {
            return (UiaPatternId)patternId switch
            {
                UiaPatternId.ExpandCollapse => Peer is IExpandCollapseProvider ? this : null,
                UiaPatternId.Invoke => Peer is AAP.IInvokeProvider ? this : null,
                UiaPatternId.RangeValue => Peer is AAP.IRangeValueProvider ? this : null,
                UiaPatternId.Scroll => Peer is AAP.IScrollProvider ? this : null,
                UiaPatternId.ScrollItem => this,
                UiaPatternId.Selection => Peer is AAP.ISelectionProvider ? this : null,
                UiaPatternId.SelectionItem => Peer is AAP.ISelectionItemProvider ? this : null,
                UiaPatternId.Toggle => Peer is AAP.IToggleProvider ? this : null,
                UiaPatternId.Value => Peer is AAP.IValueProvider ? this : null,
                _ => null,
            };
        }

        public virtual object? GetPropertyValue(int propertyId)
        {
            return (UiaPropertyId)propertyId switch
            {
                UiaPropertyId.AcceleratorKey => InvokeSync(() => Peer.GetAcceleratorKey()),
                UiaPropertyId.AccessKey => InvokeSync(() => Peer.GetAccessKey()),
                UiaPropertyId.AutomationId => InvokeSync(() => Peer.GetAutomationId()),
                UiaPropertyId.ClassName => InvokeSync(() => Peer.GetClassName()),
                UiaPropertyId.ClickablePoint => new[] { BoundingRectangle.Center.X, BoundingRectangle.Center.Y },
                UiaPropertyId.ControlType => InvokeSync(() => ToUiaControlType(Peer.GetAutomationControlType())),
                UiaPropertyId.Culture => CultureInfo.CurrentCulture.LCID,
                UiaPropertyId.FrameworkId => "Avalonia",
                UiaPropertyId.HasKeyboardFocus => InvokeSync(() => Peer.HasKeyboardFocus()),
                UiaPropertyId.IsContentElement => InvokeSync(() => Peer.IsContentElement()),
                UiaPropertyId.IsControlElement => InvokeSync(() => Peer.IsControlElement()),
                UiaPropertyId.IsEnabled => InvokeSync(() => Peer.IsEnabled()),
                UiaPropertyId.IsKeyboardFocusable => InvokeSync(() => Peer.IsKeyboardFocusable()),
                UiaPropertyId.LocalizedControlType => InvokeSync(() => Peer.GetLocalizedControlType()),
                UiaPropertyId.Name => InvokeSync(() => Peer.GetName()),
                UiaPropertyId.ProcessId => Process.GetCurrentProcess().Id,
                UiaPropertyId.RuntimeId => _runtimeId,
                _ => null,
            };
        }

        public int[]? GetRuntimeId() => _runtimeId;

        public virtual IRawElementProviderFragment? Navigate(NavigateDirection direction)
        {
            IAutomationNode? GetSibling(int direction)
            {
                var children = Peer.GetParent()?.GetChildren();

                for (var i = 0; i < (children?.Count ?? 0); ++i)
                {
                    if (ReferenceEquals(children![i], Peer))
                    {
                        var j = i + direction;
                        if (j >= 0 && j < children.Count)
                            return children[j].Node;
                    }
                }

                return null;
            }

            if (Peer.GetType().Name == "PopupAutomationPeer")
            {
                System.Diagnostics.Debug.WriteLine("Popup automation node navigate " + direction);
            }

            return InvokeSync(() =>
            {
                return direction switch
                {
                    NavigateDirection.Parent => Peer.GetParent()?.Node,
                    NavigateDirection.NextSibling => GetSibling(1),
                    NavigateDirection.PreviousSibling => GetSibling(-1),
                    NavigateDirection.FirstChild => Peer.GetChildren().FirstOrDefault()?.Node,
                    NavigateDirection.LastChild => Peer.GetChildren().LastOrDefault()?.Node,
                    _ => null,
                };
            }) as IRawElementProviderFragment;
        }

        public void SetFocus() => InvokeSync(() => Peer.SetFocus());

        IRawElementProviderSimple[]? IRawElementProviderFragment.GetEmbeddedFragmentRoots() => null;
        void IRawElementProviderSimple2.ShowContextMenu() => InvokeSync(() => Peer.ShowContextMenu());
        void IInvokeProvider.Invoke() => InvokeSync((AAP.IInvokeProvider x) => x.Invoke());

        void IRawElementProviderAdviseEvents.AdviseEventAdded(int eventId, int[] properties)
        {
            switch ((UiaEventId)eventId)
            {
                case UiaEventId.AutomationPropertyChanged:
                    ++_raisePropertyChanged;
                    break;
                case UiaEventId.AutomationFocusChanged:
                    ++_raiseFocusChanged;
                    break;
            }
        }

        void IRawElementProviderAdviseEvents.AdviseEventRemoved(int eventId, int[] properties)
        {
            switch ((UiaEventId)eventId)
            {
                case UiaEventId.AutomationPropertyChanged:
                    --_raisePropertyChanged;
                    break;
                case UiaEventId.AutomationFocusChanged:
                    --_raiseFocusChanged;
                    break;
            }
        }

        protected void InvokeSync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        [return: MaybeNull]
        protected T InvokeSync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
                return func();
            else
                return Dispatcher.UIThread.InvokeAsync(func).Result;
        }

        protected void InvokeSync<TInterface>(Action<TInterface> action)
        {
            if (Peer is TInterface i)
            {
                try
                {
                    InvokeSync(() => action(i));
                }
                catch (AggregateException e) when (e.InnerException is ElementNotEnabledException)
                {
                    throw new COMException(e.Message, UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED);
                }
            }
        }

        [return: MaybeNull]
        protected TResult InvokeSync<TInterface, TResult>(Func<TInterface, TResult> func)
        {
            if (Peer is TInterface i)
            {
                try
                {
                    return InvokeSync(() => func(i));
                }
                catch (AggregateException e) when (e.InnerException is ElementNotEnabledException)
                {
                    throw new COMException(e.Message, UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED);
                }
            }

            return default;
        }

        protected void RaiseFocusChanged(AutomationNode? focused)
        {
            if (_raiseFocusChanged > 0)
            {
                UiaCoreProviderApi.UiaRaiseAutomationEvent(
                    focused,
                    (int)UiaEventId.AutomationFocusChanged);
            }
        }

        private AutomationPeer GetRoot()
        {
            Dispatcher.UIThread.VerifyAccess();

            var peer = Peer;
            var parent = peer.GetParent();

            while (!(peer is AAP.IRootProvider) && parent is object)
            {
                peer = parent;
                parent = peer.GetParent();
            }

            return peer;
        }

        private static UiaControlTypeId ToUiaControlType(AutomationControlType role)
        {
            return role switch
            {
                AutomationControlType.Button => UiaControlTypeId.Button,
                AutomationControlType.Calendar => UiaControlTypeId.Calendar,
                AutomationControlType.CheckBox => UiaControlTypeId.CheckBox,
                AutomationControlType.ComboBox => UiaControlTypeId.ComboBox,
                AutomationControlType.ComboBoxItem => UiaControlTypeId.ListItem,
                AutomationControlType.Edit => UiaControlTypeId.Edit,
                AutomationControlType.Hyperlink => UiaControlTypeId.Hyperlink,
                AutomationControlType.Image => UiaControlTypeId.Image,
                AutomationControlType.ListItem => UiaControlTypeId.ListItem,
                AutomationControlType.List => UiaControlTypeId.List,
                AutomationControlType.Menu => UiaControlTypeId.Menu,
                AutomationControlType.MenuBar => UiaControlTypeId.MenuBar,
                AutomationControlType.MenuItem => UiaControlTypeId.MenuItem,
                AutomationControlType.ProgressBar => UiaControlTypeId.ProgressBar,
                AutomationControlType.RadioButton => UiaControlTypeId.RadioButton,
                AutomationControlType.ScrollBar => UiaControlTypeId.ScrollBar,
                AutomationControlType.Slider => UiaControlTypeId.Slider,
                AutomationControlType.Spinner => UiaControlTypeId.Spinner,
                AutomationControlType.StatusBar => UiaControlTypeId.StatusBar,
                AutomationControlType.Tab => UiaControlTypeId.Tab,
                AutomationControlType.TabItem => UiaControlTypeId.TabItem,
                AutomationControlType.Text => UiaControlTypeId.Text,
                AutomationControlType.ToolBar => UiaControlTypeId.ToolBar,
                AutomationControlType.ToolTip => UiaControlTypeId.ToolTip,
                AutomationControlType.Tree => UiaControlTypeId.Tree,
                AutomationControlType.TreeItem => UiaControlTypeId.TreeItem,
                AutomationControlType.Custom => UiaControlTypeId.Custom,
                AutomationControlType.Group => UiaControlTypeId.Group,
                AutomationControlType.Thumb => UiaControlTypeId.Thumb,
                AutomationControlType.DataGrid => UiaControlTypeId.DataGrid,
                AutomationControlType.DataItem => UiaControlTypeId.DataItem,
                AutomationControlType.Document => UiaControlTypeId.Document,
                AutomationControlType.SplitButton => UiaControlTypeId.SplitButton,
                AutomationControlType.Window => UiaControlTypeId.Window,
                AutomationControlType.Pane => UiaControlTypeId.Pane,
                AutomationControlType.Header => UiaControlTypeId.Header,
                AutomationControlType.HeaderItem => UiaControlTypeId.HeaderItem,
                AutomationControlType.Table => UiaControlTypeId.Table,
                AutomationControlType.TitleBar => UiaControlTypeId.TitleBar,
                AutomationControlType.Separator => UiaControlTypeId.Separator,
                _ => UiaControlTypeId.Custom,
            };
        }
    }
}
