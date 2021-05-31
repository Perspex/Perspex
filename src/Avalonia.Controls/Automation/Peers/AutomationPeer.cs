using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Automation.Platform;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public enum AutomationControlType
    {
        Button,
        Calendar,
        CheckBox,
        ComboBox,
        ComboBoxItem,
        Edit,
        Hyperlink,
        Image,
        ListItem,
        List,
        Menu,
        MenuBar,
        MenuItem,
        ProgressBar,
        RadioButton,
        ScrollBar,
        Slider,
        Spinner,
        StatusBar,
        Tab,
        TabItem,
        Text,
        ToolBar,
        ToolTip,
        Tree,
        TreeItem,
        Custom,
        Group,
        Thumb,
        DataGrid,
        DataItem,
        Document,
        SplitButton,
        Window,
        Pane,
        Header,
        HeaderItem,
        Table,
        TitleBar,
        Separator,
    }

    /// <summary>
    /// Provides a base class that exposes an element to UI Automation.
    /// </summary>
    public abstract class AutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationPeer"/> class.
        /// </summary>
        /// <param name="factory">
        /// The factory to use to create the platform automation node.
        /// </param>
        protected AutomationPeer(IAutomationNodeFactory factory)
        {
            Node = factory.CreateNode(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationPeer"/> class.
        /// </summary>
        /// <param name="node">
        /// The platform automation node.
        /// </param>
        protected AutomationPeer(IAutomationNode node)
        {
            Node = node;
        }

        /// <summary>
        /// Gets the related node in the platform UI Automation tree.
        /// </summary>
        public IAutomationNode Node { get; }

        /// <summary>
        /// Attempts to bring the element associated with the automation peer into view.
        /// </summary>
        public void BringIntoView() => BringIntoViewCore();

        /// <summary>
        /// Gets the accelerator key combinations for the element that is associated with the UI
        /// Automation peer.
        /// </summary>
        public string? GetAcceleratorKey() => GetAcceleratorKeyCore();

        /// <summary>
        /// Gets the access key for the element that is associated with the automation peer.
        /// </summary>
        public string? GetAccessKey() => GetAccessKeyCore();

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        public AutomationControlType GetAutomationControlType() => GetAutomationControlTypeCore();

        /// <summary>
        /// Gets the automation ID of the element that is associated with the UI Automation peer.
        /// </summary>
        public string? GetAutomationId() => GetAutomationIdCore();

        /// <summary>
        /// Gets the bounding rectangle of the element that is associated with the automation peer
        /// in top-level coordinates.
        /// </summary>
        public Rect GetBoundingRectangle() => GetBoundingRectangleCore();

        /// <summary>
        /// Gets the child automation peers.
        /// </summary>
        public IReadOnlyList<AutomationPeer> GetChildren() => GetOrCreateChildrenCore();

        /// <summary>
        /// Gets a string that describes the class of the element.
        /// </summary>
        public string GetClassName() => GetClassNameCore() ?? string.Empty;

        /// <summary>
        /// Gets the automation peer for the label that is targeted to the element.
        /// </summary>
        /// <returns></returns>
        public AutomationPeer? GetLabeledBy() => GetLabeledByCore();

        /// <summary>
        /// Gets a human-readable localized string that represents the type of the control that is
        /// associated with this automation peer.
        /// </summary>
        public string GetLocalizedControlType() => GetLocalizedControlTypeCore();

        /// <summary>
        /// Gets text that describes the element that is associated with this automation peer.
        /// </summary>
        public string GetName() => GetNameCore() ?? string.Empty;

        /// <summary>
        /// Gets the <see cref="AutomationPeer"/> that is the parent of this <see cref="AutomationPeer"/>.
        /// </summary>
        /// <returns></returns>
        public AutomationPeer? GetParent() => GetParentCore();

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation
        /// peer currently has keyboard focus.
        /// </summary>
        public bool HasKeyboardFocus() => HasKeyboardFocusCore();

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation
        /// peer contains data that is presented to the user.
        /// </summary>
        public bool IsContentElement() => IsControlElement() && IsContentElementCore();

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as
        /// interactive or as contributing to the logical structure of the control in the GUI.
        /// </summary>
        public bool IsControlElement() => IsControlElementCore();

        /// <summary>
        /// Gets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        public bool IsEnabled() => IsEnabledCore();

        /// <summary>
        /// Gets a value that indicates whether the element can accept keyboard focus.
        /// </summary>
        /// <returns></returns>
        public bool IsKeyboardFocusable() => IsKeyboardFocusableCore();

        /// <summary>
        /// Sets the keyboard focus on the element that is associated with this automation peer.
        /// </summary>
        public void SetFocus() => SetFocusCore();

        /// <summary>
        /// Shows the context menu for the element that is associated with this automation peer.
        /// </summary>
        /// <returns>true if a context menu is present for the element; otherwise false.</returns>
        public bool ShowContextMenu() => ShowContextMenuCore();

        /// <summary>
        /// Raises an event to notify the automation client of a changed property value.
        /// </summary>
        /// <param name="automationProperty">The property that changed.</param>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public void RaisePropertyChangedEvent(
            AutomationProperty automationProperty,
            object? oldValue,
            object? newValue)
        {
            Node.PropertyChanged(automationProperty, oldValue, newValue);
        }

        protected virtual string GetLocalizedControlTypeCore()
        {
            var controlType = GetAutomationControlType();

            return controlType switch
            {
                AutomationControlType.CheckBox => "check box",
                AutomationControlType.ComboBox => "combo box",
                AutomationControlType.ListItem => "list item",
                AutomationControlType.MenuBar => "menu bar",
                AutomationControlType.MenuItem => "menu item",
                AutomationControlType.ProgressBar => "progress bar",
                AutomationControlType.RadioButton => "radio button",
                AutomationControlType.ScrollBar => "scroll bar",
                AutomationControlType.StatusBar => "status bar",
                AutomationControlType.TabItem => "tab item",
                AutomationControlType.ToolBar => "toolbar",
                AutomationControlType.ToolTip => "tooltip",
                AutomationControlType.TreeItem => "tree item",
                AutomationControlType.Custom => "custom",
                AutomationControlType.DataGrid => "data grid",
                AutomationControlType.DataItem => "data item",
                AutomationControlType.SplitButton => "split button",
                AutomationControlType.HeaderItem => "header item",
                AutomationControlType.TitleBar => "title bar",
                _ => controlType.ToString().ToLowerInvariant(),
            };
        }

        protected abstract void BringIntoViewCore();
        protected abstract string? GetAcceleratorKeyCore();
        protected abstract string? GetAccessKeyCore();
        protected abstract AutomationControlType GetAutomationControlTypeCore();
        protected abstract string? GetAutomationIdCore();
        protected abstract Rect GetBoundingRectangleCore();
        protected abstract IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore();
        protected abstract string GetClassNameCore();
        protected abstract AutomationPeer? GetLabeledByCore();
        protected abstract string? GetNameCore();
        protected abstract AutomationPeer? GetParentCore();
        protected abstract bool HasKeyboardFocusCore();
        protected abstract bool IsContentElementCore();
        protected abstract bool IsControlElementCore();
        protected abstract bool IsEnabledCore();
        protected abstract bool IsKeyboardFocusableCore();
        protected abstract void SetFocusCore();
        protected abstract bool ShowContextMenuCore();
        protected internal abstract bool TrySetParent(AutomationPeer? parent);

        protected void EnsureEnabled()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
        }
    }
}
