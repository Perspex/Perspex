// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of Underline element.
//

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
//using System.IO.Packaging;
//using System.Security;
//using System.Text;
//using System.Windows.Automation.Peers;
//using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using MS.Internal;
//using MS.Internal.AppModel;
//using System.Windows.Threading;

//using CommonAvaloniaProperty=MS.Internal.PresentationFramework.CommonAvaloniaPropertyAttribute;
//using SecurityHelper=MS.Internal.PresentationFramework.SecurityHelper;

namespace System.Windows.Documents
{
    /// <summary>
    /// Implements a Hyperlink element
    /// </summary>
    [TextElementEditingBehaviorAttribute(IsMergeable = false, IsTypographicOnly = false)]
    public class Hyperlink : Span/*, ICommandSource, IUriContext*/
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        //
        // Static Ctor to create default style sheet
        //
        static Hyperlink()
        {
            // TODO:DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
            // TODO:_dType = IAvaloniaObjectType.FromSystemTypeInternal(typeof(Hyperlink));
            // TODO:FocusableProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(true));
            // TODO:EventManager.RegisterClassHandler(typeof(Hyperlink), Mouse.QueryCursorEvent, new QueryCursorEventHandler(OnQueryCursor));
        }

        /// <summary>
        /// Initializes a new instance of Hyperlink element.
        /// </summary>
        /// <remarks>
        /// To become fully functional this element requires at least one other Inline element
        /// as its child, typically Run with some text.
        /// </remarks>
        public Hyperlink() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of Hyperlink element and adds a given Inline element as its first child.
        /// </summary>
        /// <param name="childInline">
        /// Inline element added as an initial child to this Hyperlink element
        /// </param>
        public Hyperlink(Inline childInline) : base(childInline)
        {
        }

        /// <summary>
        /// Creates a new Span instance.
        /// </summary>
        /// <param name="childInline">
        /// Optional child Inline for the new Span.  May be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Span.  May be null.
        /// </param>
        public Hyperlink(Inline childInline, TextPointer insertionPosition) : base(childInline, insertionPosition)
        {
        }

        /// <summary>
        /// Creates a new Hyperlink instance covering existing content.
        /// </summary>
        /// <param name="start">
        /// Start position of the new Hyperlink.
        /// </param>
        /// <param name="end">
        /// End position of the new Hyperlink.
        /// </param>
        /// <remarks>
        /// start and end must both be parented by the same Paragraph, otherwise
        /// the method will raise an ArgumentException.
        /// </remarks>
        public Hyperlink(TextPointer start, TextPointer end) : base(start, end)
        {
            // After inserting this Hyperlink, we need to extract any child Hyperlinks.

            TextPointer navigator = this.ContentStart.CreatePointer();
            TextPointer stop = this.ContentEnd;

            while (navigator.CompareTo(stop) < 0)
            {
                Hyperlink hyperlink = navigator.GetAdjacentElement(LogicalDirection.Forward) as Hyperlink;

                if (hyperlink != null)
                {
                    hyperlink.Reposition(null, null);
                }
                else
                {
                    navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }
        }

        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// This method does exactly the same operation as clicking the Hyperlink with the mouse, except the navigation is not treated as user-initiated.
        /// </summary>
        //TODO: public void DoClick()
        //{
        //    DoNonUserInitiatedNavigation(this);
        //}

        #region ICommandSource

        /// <summary>
        ///     The AvaloniaProperty for RoutedCommand
        /// </summary>
        public static readonly StyledProperty<ICommand> CommandProperty =
                AvaloniaProperty.Register<Hyperlink, ICommand>(
                        "Command");

        /// <summary>
        /// Get or set the Command property
        /// </summary>
        [Bindable(true), Category("Action")]
        //[Localizability(LocalizationCategory.NeverLocalize)]
        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        // Returns true when this Hyperlink is hosted by an enabled
        // TextEditor (eg, within a RichTextBox).
        //TODO: private bool IsEditable
        //{
        //    get
        //    {
        //        return (this.TextContainer.TextSelection != null &&
        //                !this.TextContainer.TextSelection.TextEditor.IsReadOnly);
        //    }
        //}

        /// <summary>
        ///     Fetches the value of the IsEnabled property
        /// </summary>
        /// <remarks>
        ///     The reason this property is overridden is so that Hyperlink
        ///     can infuse the value for CanExecute into it.
        /// </remarks>
        //TODO: protected override bool IsEnabledCore
        //{
        //    get
        //    {
        //        return base.IsEnabledCore && CanExecute;
        //    }
        //}

        /// <summary>
        /// The AvaloniaProperty for the CommandParameter
        /// </summary>
        public static readonly AvaloniaProperty CommandParameterProperty =
                AvaloniaProperty.Register<Hyperlink, object>(
                        "CommandParameter");

        /// <summary>
        /// Reflects the parameter to pass to the CommandProperty upon execution.
        /// </summary>
        [Bindable(true), Category("Action")]
        //[Localizability(LocalizationCategory.NeverLocalize)]
        public object CommandParameter
        {
            get
            {
                return GetValue(CommandParameterProperty);
            }
            set
            {
                SetValue(CommandParameterProperty, value);
            }
        }

        ///// <summary>
        /////     The AvaloniaProperty for Target property
        /////     Flags:              None
        /////     Default Value:      null
        ///// </summary>
        //TODO: public static readonly AvaloniaProperty CommandTargetProperty =
        //        AvaloniaProperty.Register(
        //                "CommandTarget",
        //                typeof(IInputElement),
        //                typeof(Hyperlink),
        //                new FrameworkPropertyMetadata((IInputElement)null));

        ///// <summary>
        /////     The target element on which to fire the command.
        ///// </summary>
        //[Bindable(true), Category("Action")]
        //TODO: public IInputElement CommandTarget
        //{
        //    get
        //    {
        //        return (IInputElement)GetValue(CommandTargetProperty);
        //    }
        //    set
        //    {
        //        SetValue(CommandTargetProperty, value);
        //    }
        //}

        //#endregion

        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Contains the target URI to navigate when hyperlink is clicked
        /// </summary>
        //[CommonAvaloniaProperty]
        public static readonly StyledProperty<Uri> NavigateUriProperty =
            AvaloniaProperty.Register<Hyperlink, Uri>(
                      "NavigateUri");

        /// <summary>
        /// Coerce value callback for NavigateUri.
        /// </summary>
        /// <param name="d">Element to coerce NavigateUri for.</param>
        /// <param name="value">New value for NavigateUri.</param>
        /// <returns>Coerced value.</returns>
        //TODO: internal static Uri CoerceNavigateUri(IAvaloniaObject d, Uri value)
        //{
        //    //
        //    // If the element for which NavigateUri is being changed is the protected element,
        //    // we don't let the update go through. This cancels NavigateUri modifications in
        //    // the critical period when the URI is shown on the status bar.
        //    // An example attack:
        //    //      void hl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //    //      {
        //    //          hl.NavigateUri = null;
        //    //          hl.DoClick();
        //    //          hl.NavigateUri = new Uri("http://www.evil.com");
        //    //      }
        //    // (Or, instead of setting NavigateUri=null, add a handler for Hyperlink.RequestNavigateEvent and
        //    //  set e.Handled=true.)
        //    //
        //    if (s_criticalNavigateUriProtectee.Value == d.GetHashCode() && ShouldPreventUriSpoofing)
        //    {
        //        value = null;
        //    }

        //    return value;
        //}

        /// <summary>
        /// Provide public access to NavigateUriProperty property. Content the URI to navigate.
        /// </summary>
        [Bindable(true)/*, CustomCategory("Navigation")*/]
        //[Localizability(LocalizationCategory.Hyperlink)]
        public Uri NavigateUri
        {
            get
            {
                return (Uri)GetValue(NavigateUriProperty);
            }
            set
            {
                SetValue(NavigateUriProperty, value);
            }
        }

        ///// <summary>
        ///// Contains the target window to navigate when hyperlink is clicked
        ///// </summary>
        //TODO: public static readonly AvaloniaProperty TargetNameProperty
        //    = AvaloniaProperty.Register("TargetName", typeof(String), typeof(Hyperlink),
        //                                  new FrameworkPropertyMetadata(string.Empty));

        ///// <summary>
        ///// Provide public access to TargetNameProperty property.  The target window to navigate.
        ///// </summary>
        //[Bindable(true), CustomCategory("Navigation")]
        //[Localizability(
        //    LocalizationCategory.None,
        //    Modifiability = Modifiability.Unmodifiable)
        //]
        //TODO: public string TargetName
        //{
        //    get
        //    {
        //        return (string)GetValue(TargetNameProperty);
        //    }
        //    set
        //    {
        //        SetValue(TargetNameProperty, value);
        //    }
        //}

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        #region Public Events

        // **  The right solution is to have NavigationService define the event DP.
        // Once the event is defined on
        // NavigationService and RequestNavigateEventArgs is modified to set its ID to the new event,
        // the event ID below must be modified to reflect the change.

        /// <summary>
        /// Navigate Event
        /// </summary>
        //TODO: public static readonly RoutedEvent RequestNavigateEvent = EventManager.RegisterRoutedEvent(
        //                                            "RequestNavigate",
        //                                            RoutingStrategy.Bubble,
        //                                            typeof(RequestNavigateEventHandler),
        //                                            typeof(Hyperlink));

        /// <summary>
        /// Add / Remove RequestNavigateEvent handler
        /// </summary>
        //TODO: public event RequestNavigateEventHandler RequestNavigate
        //{
        //    add
        //    {
        //        AddHandler(RequestNavigateEvent, value);
        //    }
        //    remove
        //    {
        //        RemoveHandler(RequestNavigateEvent, value);
        //    }
        //}

        /// <summary>
        /// Event correspond to left mouse button click
        /// </summary>
        //TODO: public static readonly RoutedEvent ClickEvent = System.Windows.Controls.Primitives.ButtonBase.ClickEvent.AddOwner(typeof(Hyperlink));

        /// <summary>
        /// Add / Remove ClickEvent handler
        /// </summary>
        //[Category("Behavior")]
        //TODO: public event RoutedEventHandler Click { add { AddHandler(ClickEvent, value); } remove { RemoveHandler(ClickEvent, value); } }

        /// <summary>
        /// StatusBar event
        /// </summary>
        //TODO: internal static readonly RoutedEvent RequestSetStatusBarEvent = EventManager.RegisterRoutedEvent(
        //                                            "RequestSetStatusBar",
        //                                            RoutingStrategy.Bubble,
        //                                            typeof(RoutedEventHandler),
        //                                            typeof(Hyperlink));

        #endregion Public Events

        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        /// <remarks>Kept around for backward compatibility in derived classes.</remarks>
        //TODO: protected internal override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonDown(e);

        //    if (IsEnabled && (!IsEditable || ((Keyboard.Modifiers & ModifierKeys.Control) != 0)))
        //    {
        //        OnMouseLeftButtonDown(this, e);
        //    }
        //}

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        /// <remarks>
        /// Added for the NavigateUri = null case, which won't have event handlers hooked
        /// up since OnNavigateUriChanged isn't ever called. However, we want to have the
        /// sequence of commands and Click event triggered even in this case for Hyperlink.
        /// </remarks>
        //TODO: protected internal override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonUp(e);

        //    OnMouseLeftButtonUp(this, e);
        //}

        #region Spoofing prevention and status bar access

        /// <summary>
        /// Cached URI for spoofing countermeasures.
        /// </summary>
        /// <remarks>
        /// We keep one per thread in case multiple threads would be involved in the spoofing attack.
        /// </remarks>
        //[ThreadStatic]
        //private static SecurityCriticalDataForSet<Uri> s_cachedNavigateUri;

        /// <summary>
        /// Identification code of the hyperlink element currently protected against spoofing attacks.
        /// This code is checked during the NavigateUri coerce value callback in order to protect the
        /// NavigateUri from changing during the critical period between showing the URI on the status
        /// bar and clearing it, which is the timeframe where spoofing attacks can occur.
        /// </summary>
        /// <remarks>
        /// We keep one per thread in case multiple threads would be involved in the spoofing attack.
        ///// </remarks>
        //[ThreadStatic]
        //private static SecurityCriticalDataForSet<int?> s_criticalNavigateUriProtectee;

        /// <summary>
        /// Caches a target URI for spoofing prevention.
        /// </summary>
        /// <param name="d">Hyperlink object for which the target URI is to be cached.</param>
        /// <param name="targetUri">Target URI the user expects to be navigate to.</param>
        //private static void CacheNavigateUri(IAvaloniaObject d, Uri targetUri)
        //{
        //    //
        //    // This prevents against multi-threaded spoofing attacks.
        //    //
        //    d.VerifyAccess();

        //    s_cachedNavigateUri.Value = targetUri;
        //}

        /// <summary>
        /// Navigates to the specified URI if it matches the pre-registered cached target URI (spoofing prevention).
        /// </summary>
        /// <param name="sourceElement">Source for the RequestNavigateEventArgs.</param>
        /// <param name="targetUri">URI to navigate to.</param>
        /// <param name="targetWindow">Target window for the RequestNavigateEventArgs.</param>
        //private static void NavigateToUri(IInputElement sourceElement, Uri targetUri, string targetWindow)
        //{
        //    Debug.Assert(targetUri != null);

        //    //
        //    // This prevents against multi-threaded spoofing attacks.
        //    //
        //    IAvaloniaObject dObj = (IAvaloniaObject)sourceElement;
        //    dObj.VerifyAccess();

        //    //
        //    // Spoofing countermeasure makes sure the URI hasn't changed since display in the status bar.
        //    //
        //    Uri cachedUri = Hyperlink.s_cachedNavigateUri.Value;
        //    // ShouldPreventUriSpoofing is checked last in order to avoid incurring a first-chance SecurityException
        //    // in common scenarios.
        //    if (cachedUri == null || cachedUri.Equals(targetUri) || !ShouldPreventUriSpoofing)
        //    {
        //        // Need to mark as visited

        //        // We treat FixedPage seperately to maintain backward compatibility
        //        // with the original separate FixedPage implementation of this, which
        //        // calls the GetLinkUri method.
        //        if (!(sourceElement is Hyperlink))
        //        {
        //            targetUri = FixedPage.GetLinkUri(sourceElement, targetUri);
        //        }

        //        RequestNavigateEventArgs navigateArgs = new RequestNavigateEventArgs(targetUri, targetWindow);
        //        navigateArgs.Source = sourceElement;
        //        sourceElement.RaiseEvent(navigateArgs);

        //        if (navigateArgs.Handled)
        //        {
        //            //
        //            // The browser's status bar should be cleared. Otherwise it will still show the
        //            // hyperlink address after navigation has completed.
        //            // !! We have to do this after the current callstack is unwound in order to keep
        //            // the anti-spoofing state valid. A particular attach is to do a bogus call to
        //            // DoClick() in a mouse click preview event and then change the NavigateUri.
        //            //
        //            dObj.Dispatcher.BeginInvoke(DispatcherPriority.Send,
        //                new System.Threading.SendOrPostCallback(ClearStatusBarAndCachedUri), sourceElement);
        //        }
        //    }
        //}

        /// <summary>
        /// Updates the status bar to reflect the current NavigateUri.
        /// </summary>
        //private static void UpdateStatusBar(object sender)
        //{
        //    IInputElement element = (IInputElement)sender;
        //    IAvaloniaObject dObject = (IAvaloniaObject)sender;

        //    Uri targetUri = (Uri)dObject.GetValue(GetNavigateUriProperty(element));

        //    //
        //    // Keep the identification code for the element that's to be protected against spoofing
        //    // attacks because its URI is shown on the status bar.
        //    //
        //    s_criticalNavigateUriProtectee.Value = dObject.GetHashCode();

        //    //
        //    // Cache URI for spoofing countermeasures.
        //    //
        //    CacheNavigateUri(dObject, targetUri);

        //    RequestSetStatusBarEventArgs args = new RequestSetStatusBarEventArgs(targetUri);
        //    element.RaiseEvent(args);
        //}

        // The implementation of Hyperlink.NavigateUri and FixedPage.NavigateUri are unified,
        // but the DPs themselves are not. FixedPage.NavigateUri is attached; Hyperlink.Navigate
        // is a regular DP. Use this method to get the property DP based on the element.
        //private static AvaloniaProperty GetNavigateUriProperty(object element)
        //{
        //    Hyperlink hl = element as Hyperlink;
        //    return (hl == null) ? FixedPage.NavigateUriProperty : NavigateUriProperty;
        //}

        /// <summary>
        /// Clears the status bar.
        /// </summary>
        //    private static void ClearStatusBarAndCachedUri(object sender)
        //    {
        //        IInputElement element = (IInputElement)sender;


        //        Clear the status bar first, from this point on we're not protecting against spoofing
        //         anymore.

        //        element.RaiseEvent(RequestSetStatusBarEventArgs.Clear);


        //        Invalidate cache URI for spoofing countermeasures.


        //       CacheNavigateUri((IAvaloniaObject)sender, null);


        //        Clear the identification code for the element that was protected against spoofing.

        //       s_criticalNavigateUriProtectee.Value = null;
        //}

        #endregion

        /// <summary>
        /// Navigate to URI specified in NavigateUri property and mark the hyperlink as visited
        /// </summary>
        /// <remarks>
        /// Some forms of navigation are not allowed in the internet zone.
        /// As such there are cases where this API will demand for fulltrust.
        ///
        /// This method is kept of backward compatibility and isn't a real event handler anymore.
        /// It should remain in here however for subclasses that want to override it either to
        /// redefine behavior or to get notified about the click event.
        /// </remarks>
        //protected virtual void OnClick()
        //    {
        //        if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
        //        {
        //            AutomationPeer peer = ContentElementAutomationPeer.CreatePeerForElement(this);
        //            if (peer != null)
        //                peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
        //        }

        //        DoNavigation(this);
        //        RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent, this));

        //        MS.Internal.Commands.CommandHelpers.ExecuteCommandSource(this);
        //    }

        //    /// <summary>
        //    /// This is the method that responds to the KeyDown event.
        //    /// </summary>
        //    /// <remarks>
        //    /// This method is kept for backward compatibility.
        //    /// </remarks>
        //    protected internal override void OnKeyDown(KeyEventArgs e)
        //    {
        //        if (!e.Handled && e.Key == Key.Enter)
        //        {
        //            OnKeyDown(this, e);
        //        }
        //        else
        //        {
        //            base.OnKeyDown(e);
        //        }
        //    }

        //    //
        //    //  This property
        //    //  1. Finds the correct initial size for the _effectiveValues store on the current IAvaloniaObject
        //    //  2. This is a performance optimization
        //    //
        //    internal override int EffectiveValuesInitialSize
        //    {
        //        get { return 19; }
        //    }

        //    /// <summary>
        //    /// Creates AutomationPeer (<see cref="ContentElement.OnCreateAutomationPeer"/>)
        //    /// </summary>
        //    protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        //    {
        //        return new System.Windows.Automation.Peers.HyperlinkAutomationPeer(this);
        //    }

        #endregion Protected Methods

        //#region IUriContext implementation

        /// <summary>
        /// IUriContext interface is implemented by Hyperlink element so that it
        /// can hold on to the base URI used by parser.
        /// The base URI is needed to resolve NavigateUri property
        /// </summary>
        ///// <value>Base Uri</value>
        //Uri IUriContext.BaseUri
        //{
        //    get
        //    {
        //        return  BaseUri;
        //    }
        //    set
        //    {
        //        BaseUri = value;
        //    }
        //}

        ///// <summary>
        /////    Implementation for BaseUri
        ///// </summary>
        //protected virtual Uri BaseUri
        //{
        //    get
        //    {
        //        return (Uri)GetValue(BaseUriHelper.BaseUriProperty);
        //    }
        //    set
        //    {
        //        SetValue(BaseUriHelper.BaseUriProperty, value);
        //    }
        //}

        #endregion IUriContext implementation


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// The content spanned by this Hyperlink represented as plain text.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string Text
        {
            get
            {
                return TextRangeBase.GetTextInternal(this.ContentStart, this.ContentEnd);
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        //#region Private Methods

        // QueryCursorEvent callback.
        // If this Hyperlink is editable, use the editor cursor unless
        // the control key is down.
        //TODO: private static void OnQueryCursor(object sender, QueryCursorEventArgs e)
        //{
        //    Hyperlink link = (Hyperlink)sender;

        //    if (link.IsEnabled && link.IsEditable)
        //    {
        //        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        //        {
        //            e.Cursor = link.TextContainer.TextSelection.TextEditor._cursor;
        //            e.Handled = true;
        //        }
        //    }
        //}

        //#endregion Private Methods

        //#region Private Properties
        //--------------------------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------------------------

        //static bool ShouldPreventUriSpoofing
        //{
        //    get
        //    {
        //        if (!s_shouldPreventUriSpoofing.Value.HasValue)
        //        {
        //            s_shouldPreventUriSpoofing.Value = false;
        //        }
        //        return (bool)s_shouldPreventUriSpoofing.Value;
        //    }
        //}
        //static SecurityCriticalDataForSet<bool?> s_shouldPreventUriSpoofing;

        //#endregion Private Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        //#region Private Fields

        //private bool _canExecute = true;

        //#endregion Private Fields

        //--------------------------------------------------------------------
        //
        // Navigation control
        //
        //---------------------------------------------------------------------

        //#region Navigation control

        /// <summary>
        /// Records the IsPressed property attached to elements with hyperlink functionality.
        /// </summary>
        //TODO: private static readonly AvaloniaProperty IsHyperlinkPressedProperty =
        //        AvaloniaProperty.Register(
        //                "IsHyperlinkPressed",
        //                typeof(bool),
        //                typeof(Hyperlink),
        //                new FrameworkPropertyMetadata(false));

        //internal static void OnNavigateUriChanged(IAvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        //{
        //    IInputElement element = d as IInputElement;

        //    //
        //    // We only set up spoofing prevention for known objects that are IInputElements.
        //    // However, for backward compatibility we shouldn't make this callback fail since
        //    // other places such as FixedTextBuilder use NavigateUri e.g. for serialization.
        //    //
        //    if (element != null)
        //    {
        //        Uri navigateUri = (Uri)e.NewValue;

        //        //
        //        // We use a different code path for Path, Canvas, Glyphs and FixedPage to maintain backward compatibility
        //        // with the original separate Hyperlink implementation of this (which didn't execute CanNavigateToUri).
        //        //
        //        if (navigateUri != null)
        //        {
        //            FrameworkElement fe = d as FrameworkElement;

        //            if (fe != null && ((fe is Path) || (fe is Canvas) || (fe is Glyphs) || (fe is FixedPage)))
        //            {
        //                SetUpNavigationEventHandlers(element);
        //                fe.Cursor = Cursors.Hand;
        //            }
        //            else
        //            {
        //                FrameworkContentElement fce = d as FrameworkContentElement;

        //                if (fce != null && (fce is Hyperlink))
        //                {
        //                    SetUpNavigationEventHandlers(element);
        //                }
        //            }
        //        }
        //    }
        //}

        //private static void SetUpNavigationEventHandlers(IInputElement element)
        //{
        //    //
        //    // We only support FixedPage.NavigateUri to be attached to those four elements (aka pseudo-hyperlinks):
        //    // Path, Canvas, Glyph, FixedPage.
        //    //
        //    // We can get away with the UIElement events event for the Hyperlink which is a ContentElement
        //    // because of the aliasing present on those.
        //    //

        //    //
        //    // Hyperlink already has instance handlers for the following events. To avoid handling the event twice,
        //    // we only hook up the static handlers for pseudo-hyperlinks.
        //    //
        //    if (!(element is Hyperlink))
        //    {
        //        SetUpEventHandler(element, UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown)); //initiates navigation
        //        SetUpEventHandler(element, UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnMouseLeftButtonDown)); //capture hyperlink pressed state
        //        SetUpEventHandler(element, UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnMouseLeftButtonUp)); //can initiate navigation
        //    }

        //    SetUpEventHandler(element, UIElement.MouseEnterEvent, new MouseEventHandler(OnMouseEnter)); //set status bar
        //    SetUpEventHandler(element, UIElement.MouseLeaveEvent, new MouseEventHandler(OnMouseLeave)); //clear status bar
        //}

        //private static void SetUpEventHandler(IInputElement element, RoutedEvent routedEvent, Delegate handler)
        //{
        //    //
        //    // Setting NavigateUri causes navigation event handlers to be set up.
        //    // Doing this repeatedly would keep adding handlers; therefore remove any handler first.
        //    //
        //    element.RemoveHandler(routedEvent, handler);
        //    element.AddHandler(routedEvent, handler);
        //}

        ///// <summary>
        ///// This is the method that responds to the KeyDown event.
        ///// </summary>
        //private static void OnKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (!e.Handled && e.Key == Key.Enter)
        //    {
        //        //
        //        // Keyboard navigation doesn't reveal the URL on the status bar, so there's no spoofing
        //        // attack possible. We clear the cache here and allow navigation to go through.
        //        //
        //        CacheNavigateUri((IAvaloniaObject)sender, null);

        //        if (e.UserInitiated)
        //        {
        //            DoUserInitiatedNavigation(sender);
        //        }
        //        else
        //        {
        //            DoNonUserInitiatedNavigation(sender);
        //        }

        //        e.Handled = true;
        //    }
        //}

        /// <summary>
        /// This is the method that responds to the MouseLeftButtonEvent event.
        /// </summary>
        //private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    IInputElement element = (IInputElement)sender;
        //    IAvaloniaObject dp = (IAvaloniaObject)sender;

        //    // Hyperlink should take focus when left mouse button is clicked on it
        //    // This is consistent with all ButtonBase controls and current Win32 behavior
        //    element.Focus();

        //    // It is possible that the mouse state could have changed during all of
        //    // the call-outs that have happened so far.
        //    if (e.ButtonState == MouseButtonState.Pressed)
        //    {
        //        // Capture the mouse, and make sure we got it.
        //        Mouse.Capture(element);
        //        if (element.IsMouseCaptured)
        //        {
        //            // Though we have already checked this state, our call to CaptureMouse
        //            // could also end up changing the state, so we check it again.

        //            //
        //            // ISSUE - Leave this here because of 1111993.
        //            //
        //            if (e.ButtonState == MouseButtonState.Pressed)
        //            {
        //                dp.SetValue(IsHyperlinkPressedProperty, true);
        //            }
        //            else
        //            {
        //                // Release capture since we decided not to press the button.
        //                element.ReleaseMouseCapture();
        //            }
        //        }
        //    }

        //    e.Handled = true;
        //}

        ///// <summary>
        ///// This is the method that responds to the MouseLeftButtonUpEvent event.
        ///// </summary>
        //private static void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    IInputElement element = (IInputElement)sender;
        //    IAvaloniaObject dp = (IAvaloniaObject)sender;

        //    if (element.IsMouseCaptured)
        //    {
        //        element.ReleaseMouseCapture();
        //    }

        //    //
        //    // ISSUE - Leave this here because of 1111993.
        //    //
        //    if ((bool)dp.GetValue(IsHyperlinkPressedProperty))
        //    {
        //        dp.SetValue(IsHyperlinkPressedProperty, false);

        //        // Make sure we're mousing up over the hyperlink
        //        if (element.IsMouseOver)
        //        {
        //            if (e.UserInitiated)
        //            {
        //                DoUserInitiatedNavigation(sender);
        //            }
        //            else
        //            {
        //                DoNonUserInitiatedNavigation(sender);
        //            }
        //        }
        //    }

        //    e.Handled = true;
        //}

        ///// <summary>
        ///// Fire the event to change the status bar.
        ///// </summary>
        //private static void OnMouseEnter(object sender, MouseEventArgs e)
        //{
        //    UpdateStatusBar(sender);
        //}

        ///// <summary>
        ///// Set the status bar text back to empty
        ///// </summary>
        //private static void OnMouseLeave(object sender, MouseEventArgs e)
        //{
        //    IInputElement ee = (IInputElement)sender;

        //    //
        //    // Prevent against replay attacks. We expect the mouse not to be over the
        //    // element, otherwise someone tries to circumvent the spoofing countermeasures
        //    // while we're in the critical period between OnMouseEnter and OnMouseLeave.
        //    //
        //    if (!ee.IsMouseOver)
        //    {
        //        ClearStatusBarAndCachedUri(sender);
        //    }
        //}

        //private static void DoUserInitiatedNavigation(object sender)
        //{
        //        DispatchNavigation(sender);
        //}

        //private static void DoNonUserInitiatedNavigation(object sender)
        //{
        //    CacheNavigateUri((IAvaloniaObject)sender, null);
        //    DispatchNavigation(sender);
        //}

        ///// <summary>
        ///// Dispatches navigation; if the object is a Hyperlink we go through OnClick
        ///// to preserve the original event chain, otherwise we call our DoNavigation
        ///// method.
        ///// </summary>
        //private static void DispatchNavigation(object sender)
        //{
        //    Hyperlink hl = sender as Hyperlink;
        //    if (hl != null)
        //    {
        //        //
        //        // Call the virtual OnClick on Hyperlink to keep old behavior.
        //        //
        //        hl.OnClick();
        //    }
        //    else
        //    {
        //        DoNavigation(sender);
        //    }
        //}

        ///// <summary>
        ///// Navigate to URI specified in the object's NavigateUri property.
        ///// </summary>
        //private static void DoNavigation(object sender)
        //{
        //    IInputElement element = (IInputElement)sender;
        //    IAvaloniaObject dObject = (IAvaloniaObject)sender;

        //    Uri inputUri = (Uri)dObject.GetValue(GetNavigateUriProperty(element));
        //    string targetWindow = (string)dObject.GetValue(TargetNameProperty);
        //    RaiseNavigate(element, inputUri, targetWindow);
        //}

        ///// <summary>
        ///// Navigate to URI. Used by OnClick and by automation.
        ///// </summary>
        ///// <param name="sourceElement">Source for the RequestNavigateEventArgs.</param>
        ///// <param name="targetUri">URI to navigate to.</param>
        ///// <param name="targetWindow">Target window for the RequestNavigateEventArgs.</param>
        //internal static void RaiseNavigate(IInputElement element, Uri targetUri, string targetWindow)
        //{
        //    //
        //    // Do secure (spoofing countermeasures) navigation.
        //    //
        //    if (targetUri != null)
        //    {
        //        NavigateToUri(element, targetUri, targetWindow);
        //    }
        //}

        //#endregion

        //#region DTypeThemeStyleKey

        // Returns the IAvaloniaObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        //internal override IAvaloniaObjectType DTypeThemeStyleKey
        //{
        //    get { return _dType; }
        //}

        //private static IAvaloniaObjectType _dType;

        //#endregion DTypeThemeStyleKey
    }
}
