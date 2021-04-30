using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia
{
    /// <summary>
    /// Encapsulates a Avalonia application.
    /// </summary>
    /// <remarks>
    /// The <see cref="Application"/> class encapsulates Avalonia application-specific
    /// functionality, including:
    /// - A global set of <see cref="DataTemplates"/>.
    /// - A global set of <see cref="Styles"/>.
    /// - A <see cref="FocusManager"/>.
    /// - An <see cref="InputManager"/>.
    /// - Registers services needed by the rest of Avalonia in the <see cref="RegisterServices"/>
    /// method.
    /// - Tracks the lifetime of the application.
    /// </remarks>
    public class Application : AvaloniaObject, IDataContextProvider, IGlobalDataTemplates, IGlobalStyles, IResourceHost, IApplicationPlatformEvents
    {
        /// <summary>
        /// The application-global data templates.
        /// </summary>
        private DataTemplates _dataTemplates;

        private readonly Lazy<IClipboard> _clipboard =
            new Lazy<IClipboard>(() => (IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)));
        private readonly Styler _styler = new Styler();
        private Styles _styles;
        private IResourceDictionary _resources;
        private bool _notifyingResourcesChanged;
        private Action<IReadOnlyList<IStyle>> _stylesAdded;
        private Action<IReadOnlyList<IStyle>> _stylesRemoved;

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object> DataContextProperty =
            StyledElement.DataContextProperty.AddOwner<Application>();

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        public event EventHandler<UrlOpenedEventArgs> UrlsOpened; 

        /// <summary>
        /// Creates an instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
        {
            Name = "Avalonia Application";
        }

        /// <summary>
        /// Gets or sets the Applications's data context.
        /// </summary>
        /// <remarks>
        /// The data context property specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object DataContext
        {
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        /// <summary>
        /// Gets the current instance of the <see cref="Application"/> class.
        /// </summary>
        /// <value>
        /// The current instance of the <see cref="Application"/> class.
        /// </value>
        public static Application Current
        {
            get { return AvaloniaLocator.Current.GetService<Application>(); }
        }

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public DataTemplates DataTemplates => _dataTemplates ?? (_dataTemplates = new DataTemplates());

        /// <summary>
        /// Gets the application's focus manager.
        /// </summary>
        /// <value>
        /// The application's focus manager.
        /// </value>
        public IFocusManager FocusManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public InputManager InputManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application clipboard.
        /// </summary>
        public IClipboard Clipboard => _clipboard.Value;

        /// <summary>
        /// Gets the application's global resource dictionary.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ??= new ResourceDictionary(this);
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));
                _resources?.RemoveOwner(this);
                _resources = value;
                _resources.AddOwner(this);
            }
        }

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styles Styles => _styles ??= new Styles(this);

        /// <inheritdoc/>
        bool IDataTemplateHost.IsDataTemplatesInitialized => _dataTemplates != null;

        /// <inheritdoc/>
        bool IResourceNode.HasResources => (_resources?.HasResources ?? false) ||
            (((IResourceNode)_styles)?.HasResources ?? false);

        /// <summary>
        /// Gets the styling parent of the application, which is null.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => null;

        /// <inheritdoc/>
        bool IStyleHost.IsStylesInitialized => _styles != null;
       
        /// <summary>
        /// Application lifetime, use it for things like setting the main window and exiting the app from code
        /// Currently supported lifetimes are:
        /// - <see cref="IClassicDesktopStyleApplicationLifetime"/>
        /// - <see cref="ISingleViewApplicationLifetime"/>
        /// - <see cref="IControlledApplicationLifetime"/> 
        /// </summary>
        public IApplicationLifetime ApplicationLifetime { get; set; }

        event Action<IReadOnlyList<IStyle>> IGlobalStyles.GlobalStylesAdded
        {
            add => _stylesAdded += value;
            remove => _stylesAdded -= value;
        }

        event Action<IReadOnlyList<IStyle>> IGlobalStyles.GlobalStylesRemoved
        {
            add => _stylesRemoved += value;
            remove => _stylesRemoved -= value;
        }

        /// <summary>
        /// Initializes the application by loading XAML etc.
        /// </summary>
        public virtual void Initialize() { }

        /// <inheritdoc/>
        bool IResourceNode.TryGetResource(object key, out object value)
        {
            value = null;
            return (_resources?.TryGetResource(key, out value) ?? false) ||
                   Styles.TryGetResource(key, out value);
        }

        void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }

        void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
        {
            _stylesAdded?.Invoke(styles);
        }

        void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
        {
            _stylesRemoved?.Invoke(styles);
        }

        /// <summary>
        /// Register's the services needed by Avalonia.
        /// </summary>
        public virtual void RegisterServices()
        {
            AvaloniaSynchronizationContext.InstallIfNeeded();
            FocusManager = new FocusManager();
            InputManager = new InputManager();

            AvaloniaLocator.CurrentMutable
                .Bind<IAccessKeyHandler>().ToTransient<AccessKeyHandler>()
                .Bind<IGlobalDataTemplates>().ToConstant(this)
                .Bind<IGlobalStyles>().ToConstant(this)
                .Bind<IFocusManager>().ToConstant(FocusManager)
                .Bind<IInputManager>().ToConstant(InputManager)
                .Bind<IKeyboardNavigationHandler>().ToTransient<KeyboardNavigationHandler>()
                .Bind<IStyler>().ToConstant(_styler)
                .Bind<IScheduler>().ToConstant(AvaloniaScheduler.Instance)
                .Bind<IDragDropDevice>().ToConstant(DragDropDevice.Instance);
            
            // TODO: Fix this, for now we keep this behavior since someone might be relying on it in 0.9.x
            if (AvaloniaLocator.Current.GetService<IPlatformDragSource>() == null)
                AvaloniaLocator.CurrentMutable
                    .Bind<IPlatformDragSource>().ToTransient<InProcessDragSource>();

            var clock = new RenderLoopClock();
            AvaloniaLocator.CurrentMutable
                .Bind<IGlobalClock>().ToConstant(clock)
                .GetService<IRenderLoop>()?.Add(clock);
        }

        public virtual void OnFrameworkInitializationCompleted()
        {
        }
        
        void  IApplicationPlatformEvents.RaiseUrlsOpened(string[] urls)
        {
            UrlsOpened?.Invoke(this, new UrlOpenedEventArgs (urls));
        }

        private void NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            if (_notifyingResourcesChanged)
            {
                return;
            }

            try
            {
                _notifyingResourcesChanged = true;
                ResourcesChanged?.Invoke(this, ResourcesChangedEventArgs.Empty);
            }
            finally
            {
                _notifyingResourcesChanged = false;
            }
        }

        private void ThisResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            NotifyResourcesChanged(e);
        }

        private string _name;
        /// <summary>
        /// Defines Name property
        /// </summary>
        public static readonly DirectProperty<Application, string> NameProperty =
            AvaloniaProperty.RegisterDirect<Application, string>("Name", o => o.Name, (o, v) => o.Name = v);

        /// <summary>
        /// Application name to be used for various platform-specific purposes
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetAndRaise(NameProperty, ref _name, value);
        }
        
    }
}
