using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines constants for how the SplitView Pane should display
    /// </summary>
    public enum SplitViewDisplayMode
    {
        /// <summary>
        /// Pane is displayed next to content, and does not auto collapse
        /// when tapped outside
        /// </summary>
        Inline,
        /// <summary>
        /// Pane is displayed next to content. When collapsed, pane is still
        /// visible according to CompactPaneLength. Pane does not auto collapse
        /// when tapped outside
        /// </summary>
        CompactInline,
        /// <summary>
        /// Pane is displayed above content. Pane collapses when tapped outside
        /// </summary>
        Overlay,
        /// <summary>
        /// Pane is displayed above content. When collapsed, pane is still
        /// visible according to CompactPaneLength. Pane collapses when tapped outside
        /// </summary>
        CompactOverlay
    }

    /// <summary>
    /// Defines constants for where the Pane should appear
    /// </summary>
    public enum SplitViewPanePlacement
    {
        Left,
        Right
    }

    public class SplitViewTemplateSettings : AvaloniaObject
    {
        internal SplitViewTemplateSettings() { }

        public static readonly StyledProperty<double> ClosedPaneWidthProperty =
            AvaloniaProperty.Register<SplitViewTemplateSettings, double>(nameof(ClosedPaneWidth), 0d);

        public static readonly StyledProperty<GridLength> PaneColumnGridLengthProperty =
            AvaloniaProperty.Register<SplitViewTemplateSettings, GridLength>(nameof(PaneColumnGridLength));

        public double ClosedPaneWidth
        {
            get => GetValue(ClosedPaneWidthProperty);
            internal set => SetValue(ClosedPaneWidthProperty, value);
        }

        public GridLength PaneColumnGridLength
        {
            get => GetValue(PaneColumnGridLengthProperty);
            internal set => SetValue(PaneColumnGridLengthProperty, value);
        }
    }

    /// <summary>
    /// A control with two views: A collapsible pane and an area for content
    /// </summary>
    [PseudoClasses(":open", ":closed")]
    [PseudoClasses(":compactoverlay", ":compactinline", ":overlay", ":inline")]
    [PseudoClasses(":left", ":right")]
    [PseudoClasses(":lightdismiss")]
    public class SplitView : ContentControl
    {
        /*
            Pseudo classes & combos
            :open / :closed
            :compactoverlay :compactinline :overlay :inline
            :left :right
        */

        /// <summary>
        /// Defines the <see cref="CompactPaneLength"/> property
        /// </summary>
        public static readonly StyledProperty<double> CompactPaneLengthProperty =
            AvaloniaProperty.Register<SplitView, double>(nameof(CompactPaneLength), defaultValue: 48);

        /// <summary>
        /// Defines the <see cref="DisplayMode"/> property
        /// </summary>
        public static readonly StyledProperty<SplitViewDisplayMode> DisplayModeProperty =
            AvaloniaProperty.Register<SplitView, SplitViewDisplayMode>(nameof(DisplayMode), defaultValue: SplitViewDisplayMode.Overlay);

        /// <summary>
        /// Defines the <see cref="IsPaneOpen"/> property
        /// </summary>
        public static readonly DirectProperty<SplitView, bool> IsPaneOpenProperty =
            AvaloniaProperty.RegisterDirect<SplitView, bool>(nameof(IsPaneOpen),
                x => x.IsPaneOpen, (x, v) => x.IsPaneOpen = v);

        /// <summary>
        /// Defines the <see cref="OpenPaneLength"/> property
        /// </summary>
        public static readonly StyledProperty<double> OpenPaneLengthProperty =
            AvaloniaProperty.Register<SplitView, double>(nameof(OpenPaneLength), defaultValue: 320);

        /// <summary>
        /// Defines the <see cref="PaneBackground"/> property
        /// </summary>
        public static readonly StyledProperty<IBrush> PaneBackgroundProperty =
            AvaloniaProperty.Register<SplitView, IBrush>(nameof(PaneBackground));

        /// <summary>
        /// Defines the <see cref="PanePlacement"/> property
        /// </summary>
        public static readonly StyledProperty<SplitViewPanePlacement> PanePlacementProperty =
            AvaloniaProperty.Register<SplitView, SplitViewPanePlacement>(nameof(PanePlacement));

        /// <summary>
        /// Defines the <see cref="Pane"/> property
        /// </summary>
        public static readonly StyledProperty<object> PaneProperty =
            AvaloniaProperty.Register<SplitView, object>(nameof(Pane));

        /// <summary>
        /// Defines the <see cref="PaneTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> PaneTemplateProperty =
            AvaloniaProperty.Register<HeaderedContentControl, IDataTemplate>(nameof(PaneTemplate));

        /// <summary>
        /// Defines the <see cref="UseLightDismissOverlayMode"/> property
        /// </summary>
        public static readonly StyledProperty<bool> UseLightDismissOverlayModeProperty =
            AvaloniaProperty.Register<SplitView, bool>(nameof(UseLightDismissOverlayMode));

        /// <summary>
        /// Defines the <see cref="TemplateSettings"/> property
        /// </summary>
        public static readonly StyledProperty<SplitViewTemplateSettings> TemplateSettingsProperty =
            AvaloniaProperty.Register<SplitView, SplitViewTemplateSettings>(nameof(TemplateSettings));

        private bool _isPaneOpen;
        private Panel _pane;
        private IDisposable _pointerDisposable;

        public SplitView()
        {
            PseudoClasses.Add(":overlay");
            PseudoClasses.Add(":left");

            TemplateSettings = new SplitViewTemplateSettings();
        }

        static SplitView()
        {
            UseLightDismissOverlayModeProperty.Changed.AddClassHandler<SplitView>((x, v) => x.OnUseLightDismissChanged(v));
            CompactPaneLengthProperty.Changed.AddClassHandler<SplitView>((x, v) => x.OnCompactPaneLengthChanged(v));
            PanePlacementProperty.Changed.AddClassHandler<SplitView>((x, v) => x.OnPanePlacementChanged(v));
            DisplayModeProperty.Changed.AddClassHandler<SplitView>((x, v) => x.OnDisplayModeChanged(v));
            
        }

        /// <summary>
        /// Gets or sets the length of the pane when in <see cref="SplitViewDisplayMode.CompactOverlay"/> 
        /// or <see cref="SplitViewDisplayMode.CompactInline"/> mode
        /// </summary>
        public double CompactPaneLength
        {
            get => GetValue(CompactPaneLengthProperty);
            set => SetValue(CompactPaneLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="SplitViewDisplayMode"/> for the SplitView
        /// </summary>
        public SplitViewDisplayMode DisplayMode
        {
            get => GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the pane is open or closed
        /// </summary>
        public bool IsPaneOpen
        {
            get => _isPaneOpen;
            set
            {
                if (value == _isPaneOpen)
                {
                    return;
                }

                if (value)
                {
                    OnPaneOpening(this, null);
                    SetAndRaise(IsPaneOpenProperty, ref _isPaneOpen, value);

                    PseudoClasses.Add(":open");
                    PseudoClasses.Remove(":closed");
                    OnPaneOpened(this, null);
                }
                else
                {
                    SplitViewPaneClosingEventArgs args = new SplitViewPaneClosingEventArgs(false);
                    OnPaneClosing(this, args);
                    if (!args.Cancel)
                    {
                        SetAndRaise(IsPaneOpenProperty, ref _isPaneOpen, value);

                        PseudoClasses.Add(":closed");
                        PseudoClasses.Remove(":open");
                        OnPaneClosed(this, null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the length of the pane when open
        /// </summary>
        public double OpenPaneLength
        {
            get => GetValue(OpenPaneLengthProperty);
            set => SetValue(OpenPaneLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the background of the pane
        /// </summary>
        public IBrush PaneBackground
        {
            get => GetValue(PaneBackgroundProperty);
            set => SetValue(PaneBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="SplitViewPanePlacement"/> for the SplitView
        /// </summary>
        public SplitViewPanePlacement PanePlacement
        {
            get => GetValue(PanePlacementProperty);
            set => SetValue(PanePlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the Pane for the SplitView
        /// </summary>
        public object Pane
        {
            get => GetValue(PaneProperty);
            set => SetValue(PaneProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate PaneTemplate
        {
            get => GetValue(PaneTemplateProperty);
            set => SetValue(PaneTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether WinUI equivalent LightDismissOverlayMode is enabled
        /// <para>When enabled, and the pane is open in Overlay or CompactOverlay mode,
        /// the contents of the splitview are darkened to visually separate the open pane
        /// and the rest of the SplitView</para>
        /// </summary>
        public bool UseLightDismissOverlayMode
        {
            get => GetValue(UseLightDismissOverlayModeProperty);
            set => SetValue(UseLightDismissOverlayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the TemplateSettings for the SplitView
        /// </summary>
        public SplitViewTemplateSettings TemplateSettings
        {
            get => GetValue(TemplateSettingsProperty);
            set => SetValue(TemplateSettingsProperty, value);
        }

        /// <summary>
        /// Fired when the pane is closed
        /// </summary>
        public event EventHandler<EventArgs> PaneClosed;

        /// <summary>
        /// Fired when the pane is closing
        /// </summary>
        public event EventHandler<SplitViewPaneClosingEventArgs> PaneClosing;

        /// <summary>
        /// Fired when the pane is opened
        /// </summary>
        public event EventHandler<EventArgs> PaneOpened;

        /// <summary>
        /// Fired when the pane is opening
        /// </summary>
        public event EventHandler<EventArgs> PaneOpening;

        protected override bool RegisterContentPresenter(IContentPresenter presenter)
        {
            var result = base.RegisterContentPresenter(presenter);

            if (presenter.Name == "PART_PanePresenter")
            {
                return true;
            }
            
            return result;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _pane = e.NameScope.Find<Panel>("PART_PaneRoot");
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = this.VisualRoot;
            if (topLevel is Window window)
            {
                _pointerDisposable = window.AddDisposableHandler(PointerPressedEvent, PointerPressedOutside, RoutingStrategies.Tunnel);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _pointerDisposable?.Dispose();
        }

        private void PointerPressedOutside(object sender, PointerPressedEventArgs e)
        {
            if (!IsPaneOpen)
            {
                return;
            }

            //If we click within the Pane, don't do anything
            //Otherwise, ClosePane if open & using an overlay display mode
            bool closePane = ShouldClosePane();
            if (!closePane)
            {
                return;
            }

            var src = e.Source as IVisual;
            while (src != null)
            {
                // Make assumption that if Popup is in visual tree,
                // owning control is within pane
                // This works because if pane is triggered to close
                // when clicked anywhere else in Window, the pane 
                // would close before the popup is opened
                if (src == _pane || src is PopupRoot)
                {
                    closePane = false;
                    break;
                }

                src = src.VisualParent;
            }
            if (closePane)
            {
                IsPaneOpen = false;
                e.Handled = true;
            }
        }
                
        private bool ShouldClosePane()
        {
            return (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay);
        }

        protected virtual void OnPaneOpening(SplitView sender, EventArgs args)
        {
            PaneOpening?.Invoke(sender, args);
        }

        protected virtual void OnPaneOpened(SplitView sender, EventArgs args)
        {
            PaneOpened?.Invoke(sender, args);
        }

        protected virtual void OnPaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        {
            PaneClosing?.Invoke(sender, args);
        }

        protected virtual void OnPaneClosed(SplitView sender, EventArgs args)
        {
            PaneClosed?.Invoke(sender, args);
        }

        private void OnCompactPaneLengthChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newLen = (double)e.NewValue;
            var displayMode = DisplayMode;
            if (displayMode == SplitViewDisplayMode.CompactInline)
            {
                TemplateSettings.ClosedPaneWidth = newLen;
            }
            else if (displayMode == SplitViewDisplayMode.CompactOverlay)
            {
                TemplateSettings.ClosedPaneWidth = newLen;
                TemplateSettings.PaneColumnGridLength = new GridLength(newLen, GridUnitType.Pixel);
            }
        }

        private void OnPanePlacementChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldState = e.OldValue.ToString().ToLower();
            var newState = e.NewValue.ToString().ToLower();
            PseudoClasses.Remove($":{oldState}");
            PseudoClasses.Add($":{newState}");
        }

        private void OnDisplayModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldState = e.OldValue.ToString().ToLower();
            var newState = e.NewValue.ToString().ToLower();

            PseudoClasses.Remove($":{oldState}");
            PseudoClasses.Add($":{newState}");

            var (closedPaneWidth, paneColumnGridLength) = (SplitViewDisplayMode)e.NewValue switch
            {
                SplitViewDisplayMode.Overlay => (0, new GridLength(0, GridUnitType.Pixel)),
                SplitViewDisplayMode.CompactOverlay => (CompactPaneLength, new GridLength(CompactPaneLength, GridUnitType.Pixel)),
                SplitViewDisplayMode.Inline => (0, new GridLength(0, GridUnitType.Auto)),
                SplitViewDisplayMode.CompactInline => (CompactPaneLength, new GridLength(0, GridUnitType.Auto)),
                _ => throw new NotImplementedException(),
            };
            TemplateSettings.ClosedPaneWidth = closedPaneWidth;
            TemplateSettings.PaneColumnGridLength = paneColumnGridLength;
        }

        private void OnUseLightDismissChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var mode = (bool)e.NewValue;
            PseudoClasses.Set(":lightdismiss", mode);
        }
    }
}
