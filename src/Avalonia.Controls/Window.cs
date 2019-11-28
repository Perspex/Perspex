// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using System.ComponentModel;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    [Flags]
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width = 1,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height = 2,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight = 3,
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : WindowBase, IStyleable, IFocusScope, ILayoutRoot
    {
        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        public static readonly StyledProperty<bool> HasSystemDecorationsProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(HasSystemDecorations), true);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        public static readonly StyledProperty<bool> ShowInTaskbarProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowInTaskbar), true);

        /// <summary>
        /// Represents the current window state (normal, minimized, maximized)
        /// </summary>
        public static readonly StyledProperty<WindowState> WindowStateProperty =
            AvaloniaProperty.Register<Window, WindowState>(nameof(WindowState));

        /// <summary>
        /// Defines the <see cref="Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<Window, string>(nameof(Title), "Window");

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon> IconProperty =
            AvaloniaProperty.Register<Window, WindowIcon>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="WindowStartupLocation"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, WindowStartupLocation> WindowStartupLocationProperty =
            AvaloniaProperty.RegisterDirect<Window, WindowStartupLocation>(
                nameof(WindowStartupLocation),
                o => o.WindowStartupLocation,
                (o, v) => o.WindowStartupLocation = v);

        public static readonly StyledProperty<bool> CanResizeProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(CanResize), true);

        /// <summary>
        /// Routed event that can be used for global tracking of window destruction
        /// </summary>
        public static readonly RoutedEvent WindowClosedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);
        
        /// <summary>
        /// Routed event that can be used for global tracking of opening windows
        /// </summary>
        public static readonly RoutedEvent WindowOpenedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);



        private readonly NameScope _nameScope = new NameScope();
        private object _dialogResult;
        private readonly Size _maxPlatformClientSize;
        private WindowStartupLocation _windowStartupLocation;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetTitle((string)e.NewValue));
            HasSystemDecorationsProperty.Changed.AddClassHandler<Window>(
                (s, e) => s.PlatformImpl?.SetSystemDecorations((bool)e.NewValue));

            ShowInTaskbarProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.ShowTaskbarIcon((bool)e.NewValue));

            IconProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetIcon(((WindowIcon)e.NewValue).PlatformImpl));

            CanResizeProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.CanResize((bool)e.NewValue));

            WindowStateProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.WindowState = (WindowState)e.NewValue; });
            
            MinWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size((double)e.NewValue, w.MinHeight), new Size(w.MaxWidth, w.MaxHeight)));
            MinHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, (double)e.NewValue), new Size(w.MaxWidth, w.MaxHeight)));
            MaxWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size((double)e.NewValue, w.MaxHeight)));
            MaxHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size(w.MaxWidth, (double)e.NewValue)));

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
            : this(PlatformManager.CreateWindow())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="impl">The window implementation.</param>
        public Window(IWindowImpl impl)
            : base(impl)
        {
            impl.Closing = HandleClosing;
            impl.WindowStateChanged = HandleWindowStateChanged;
            _maxPlatformClientSize = PlatformImpl?.MaxClientSize ?? default(Size);
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl?.Resize(x));
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
        public new IWindowImpl PlatformImpl => (IWindowImpl)base.PlatformImpl;

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        public SizeToContent SizeToContent
        {
            get { return GetValue(SizeToContentProperty); }
            set { SetValue(SizeToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        /// 
        public bool HasSystemDecorations
        {
            get { return GetValue(HasSystemDecorationsProperty); }
            set { SetValue(HasSystemDecorationsProperty, value); }
        }

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        /// 
        public bool ShowInTaskbar
        {
            get { return GetValue(ShowInTaskbarProperty); }
            set { SetValue(ShowInTaskbarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get { return GetValue(WindowStateProperty); }
            set { SetValue(WindowStateProperty, value); }
        }

        /// <summary>
        /// Enables or disables resizing of the window.
        /// Note that if <see cref="HasSystemDecorations"/> is set to False then this property
        /// has no effect and should be treated as a recommendation for the user setting HasSystemDecorations.
        /// </summary>
        public bool CanResize
        {
            get { return GetValue(CanResizeProperty); }
            set { SetValue(CanResizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get { return _windowStartupLocation; }
            set { SetAndRaise(WindowStartupLocationProperty, ref _windowStartupLocation, value); }
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get { return PlatformImpl?.Position ?? PixelPoint.Origin; }
            set
            {
                PlatformImpl?.Move(value);
            }
        }
        
        /// <summary>
        /// Carries out the arrange pass of the window.
        /// </summary>
        /// <param name="finalSize">The final window size.</param>
        /// <returns>The <paramref name="finalSize"/> parameter unchanged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            using (BeginAutoSizing())
            {
                PlatformImpl?.Resize(finalSize);
            }

            return base.ArrangeOverride(PlatformImpl?.ClientSize ?? default(Size));
        }
        
        /// <inheritdoc/>
        Size ILayoutRoot.MaxClientSize => _maxPlatformClientSize;

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(Window);

        /// <summary>
        /// Fired before a window is closed.
        /// </summary>
        public event EventHandler<CancelEventArgs> Closing;      

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}(IWindowImpl)"/>
        /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
        /// resulting task will produce the <see cref="_dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object dialogResult)
        {
            _dialogResult = dialogResult;
            Close(false);
        }

        internal void Close(bool ignoreCancel)
        {
            bool close = true;

            try
            {
                if (!ignoreCancel && HandleClosing())
                {
                    close = false;
                    return;
                }
            }
            finally
            {
                if (close)
                {
                    PlatformImpl?.Dispose();
                    HandleClosed();
                }
            }
        }

        /// <summary>
        /// Handles a closing notification from <see cref="IWindowImpl.Closing"/>.
        /// </summary>
        protected virtual bool HandleClosing()
        {
            var args = new CancelEventArgs();
            OnClosing(args);
            return args.Cancel;
        }

        protected virtual void HandleWindowStateChanged(WindowState state)
        {
            WindowState = state;

            if (state == WindowState.Minimized)
            {
                Renderer.Stop();
            }
            else
            {
                Renderer.Start();
            }
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public override void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            using (BeginAutoSizing())
            {
                Renderer?.Stop();
                PlatformImpl?.Hide();
            }

            IsVisible = false;
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public override void Show()
        {
            if (PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot re-show a closed window.");
            }

            if (IsVisible)
            {
                return;
            }

            this.RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

            EnsureInitialized();
            IsVisible = true;
            LayoutManager.ExecuteInitialLayoutPass(this);

            using (BeginAutoSizing())
            {
                PlatformImpl?.Show();
                Renderer?.Start();
            }
            SetWindowStartupLocation(Owner?.PlatformImpl);
            OnOpened(EventArgs.Empty);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <param name="owner">The dialog's owner window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog(Window owner)
        {
            return ShowDialog<object>(owner);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <param name="owner">The dialog's owner window.</param>
        /// <returns>.
        /// A task that can be used to retrieve the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>(Window owner) => ShowDialog<TResult>(owner.PlatformImpl);

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <param name="owner">The dialog's owner window.</param>
        /// <returns>.
        /// A task that can be used to retrieve the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>(IWindowImpl owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (IsVisible)
            {
                throw new InvalidOperationException("The window is already being shown.");
            }

            RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

            EnsureInitialized();
            IsVisible = true;
            LayoutManager.ExecuteInitialLayoutPass(this);

            var result = new TaskCompletionSource<TResult>();

            using (BeginAutoSizing())
            {

                PlatformImpl?.ShowDialog(owner);

                Renderer?.Start();
                Observable.FromEventPattern<EventHandler, EventArgs>(
                    x => this.Closed += x,
                    x => this.Closed -= x)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        owner.Activate();
                        result.SetResult((TResult)(_dialogResult ?? default(TResult)));
                    });
                OnOpened(EventArgs.Empty);
            }

            SetWindowStartupLocation(owner);
            return result.Task;
        }

        private void SetWindowStartupLocation(IWindowBaseImpl owner = null)
        {
            var scaling = owner?.Scaling ?? PlatformImpl?.Scaling ?? 1;

            // TODO: We really need non-client size here.
            var rect = new PixelRect(
                PixelPoint.Origin,
                PixelSize.FromSize(ClientSize, scaling));

            if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                var screen = Screens.ScreenFromPoint(owner?.Position ?? Position);

                if (screen != null)
                {
                    Position = screen.WorkingArea.CenterRect(rect).Position;
                }
            }
            else if (WindowStartupLocation == WindowStartupLocation.CenterOwner)
            {
                if (owner != null)
                {
                    // TODO: We really need non-client size here.
                    var ownerRect = new PixelRect(
                        owner.Position,
                        PixelSize.FromSize(owner.ClientSize, scaling));
                    Position = ownerRect.CenterRect(rect).Position;
                }
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var clientSize = ClientSize;
            Size constraint = clientSize;

            if ((sizeToContent & SizeToContent.Width) != 0)
            {
                constraint = constraint.WithWidth(double.PositiveInfinity);
            }

            if ((sizeToContent & SizeToContent.Height) != 0)
            {
                constraint = constraint.WithHeight(double.PositiveInfinity);
            }

            var result = base.MeasureOverride(constraint);

            if ((sizeToContent & SizeToContent.Width) == 0)
            {
                result = result.WithWidth(clientSize.Width);
            }

            if ((sizeToContent & SizeToContent.Height) == 0)
            {
                result = result.WithHeight(clientSize.Height);
            }

            return result;
        }

        protected sealed override void HandleClosed()
        {
            RaiseEvent(new RoutedEventArgs(WindowClosedEvent));

            base.HandleClosed();
        }

        /// <inheritdoc/>
        protected sealed override void HandleResized(Size clientSize)
        {
            if (!AutoSizing)
            {
                SizeToContent = SizeToContent.Manual;
            }

            base.HandleResized(clientSize);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
        /// overridden method must call <see cref="OnClosing"/> on the base class if the
        /// <see cref="Closing"/> event needs to be raised.
        /// </remarks>
        protected virtual void OnClosing(CancelEventArgs e) => Closing?.Invoke(this, e);
    }
}
