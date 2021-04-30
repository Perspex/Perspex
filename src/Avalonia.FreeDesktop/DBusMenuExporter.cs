using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.DBusMenu;
using Avalonia.Input;
using Avalonia.Threading;
using Tmds.DBus;
#pragma warning disable 1998

namespace Avalonia.FreeDesktop
{
    public class DBusMenuExporter
    {
        public static ITopLevelNativeMenuExporter TryCreate(IntPtr xid)
        {
            if (DBusHelper.Connection == null)
                return null;

            return new DBusMenuExporterImpl(DBusHelper.Connection, xid);
        }

        class DBusMenuExporterImpl : ITopLevelNativeMenuExporter, IDBusMenu, IDisposable
        {
            private readonly Connection _dbus;
            private readonly uint _xid;
            private IRegistrar _registar;
            private bool _disposed;
            private uint _revision = 1;
            private NativeMenu _menu;
            private Dictionary<int, NativeMenuItemBase> _idsToItems = new Dictionary<int, NativeMenuItemBase>();
            private Dictionary<NativeMenuItemBase, int> _itemsToIds = new Dictionary<NativeMenuItemBase, int>();
            private readonly HashSet<NativeMenu> _menus = new HashSet<NativeMenu>();
            private bool _resetQueued;
            private int _nextId = 1;
            public DBusMenuExporterImpl(Connection dbus, IntPtr xid)
            {
                _dbus = dbus;
                _xid = (uint)xid.ToInt32();
                ObjectPath = new ObjectPath("/net/avaloniaui/dbusmenu/"
                                            + Guid.NewGuid().ToString().Replace("-", ""));
                SetNativeMenu(new NativeMenu());
                Init();
            }

            async void Init()
            {
                try
                {
                    await _dbus.RegisterObjectAsync(this);
                    _registar = DBusHelper.Connection.CreateProxy<IRegistrar>(
                        "com.canonical.AppMenu.Registrar",
                        "/com/canonical/AppMenu/Registrar");
                    if (!_disposed)
                        await _registar.RegisterWindowAsync(_xid, ObjectPath);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    // It's not really important if this code succeeds,
                    // and it's not important to know if it succeeds
                    // since even if we register the window it's not guaranteed that
                    // menu will be actually exported
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _dbus.UnregisterObject(this);
                // Fire and forget
                _registar?.UnregisterWindowAsync(_xid);
            }



            public bool IsNativeMenuExported { get; private set; }
            public event EventHandler OnIsNativeMenuExportedChanged;

            public void SetNativeMenu(NativeMenu menu)
            {
                if (menu == null)
                    menu = new NativeMenu();

                if (_menu != null)
                    ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= OnMenuItemsChanged;
                _menu = menu;
                ((INotifyCollectionChanged)_menu.Items).CollectionChanged += OnMenuItemsChanged;
                
                DoLayoutReset();
            }
            
            /*
                 This is basic initial implementation, so we don't actually track anything and
                 just reset the whole layout on *ANY* change
                 
                 This is not how it should work and will prevent us from implementing various features,
                 but that's the fastest way to get things working, so...
             */
            void DoLayoutReset()
            {
                _resetQueued = false;
                foreach (var i in _idsToItems.Values) 
                    i.PropertyChanged -= OnItemPropertyChanged;
                foreach(var menu in _menus)
                    ((INotifyCollectionChanged)menu.Items).CollectionChanged -= OnMenuItemsChanged;
                _menus.Clear();
                _idsToItems.Clear();
                _itemsToIds.Clear();
                _revision++;
                LayoutUpdated?.Invoke((_revision, 0));
            }

            void QueueReset()
            {
                if(_resetQueued)
                    return;
                _resetQueued = true;
                Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
            }

            private (NativeMenuItemBase item, NativeMenu menu) GetMenu(int id)
            {
                if (id == 0)
                    return (null, _menu);
                _idsToItems.TryGetValue(id, out var item);
                return (item, (item as NativeMenuItem)?.Menu);
            }

            private void EnsureSubscribed(NativeMenu menu)
            {
                if(menu!=null && _menus.Add(menu))
                    ((INotifyCollectionChanged)menu.Items).CollectionChanged += OnMenuItemsChanged;
            }
            
            private int GetId(NativeMenuItemBase item)
            {
                if (_itemsToIds.TryGetValue(item, out var id))
                    return id;
                id = _nextId++;
                _idsToItems[id] = item;
                _itemsToIds[item] = id;
                item.PropertyChanged += OnItemPropertyChanged;
                if (item is NativeMenuItem nmi)
                    EnsureSubscribed(nmi.Menu);
                return id;
            }

            private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                QueueReset();
            }

            private void OnItemPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
            {
                QueueReset();
            }

            public ObjectPath ObjectPath { get; }


            async Task<object> IFreeDesktopDBusProperties.GetAsync(string prop)
            {
                if (prop == "Version")
                    return 2;
                if (prop == "Status")
                    return "normal";
                return 0;
            }

            async Task<DBusMenuProperties> IFreeDesktopDBusProperties.GetAllAsync()
            {
                return new DBusMenuProperties
                {
                    Version = 2,
                    Status = "normal",
                };
            }

            private static string[] AllProperties = new[]
            {
                "type", "label", "enabled", "visible", "shortcut", "toggle-type", "children-display", "toggle-state", "icon-data"
            };
            
            object GetProperty((NativeMenuItemBase item, NativeMenu menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return "separator";
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                    {
                        return null;
                    }
                    if (name == "label")
                        return item?.Header ?? "<null>";
                    if (name == "enabled")
                    {
                        if (item == null)
                            return null;
                        if (item.Menu != null && item.Menu.Items.Count == 0)
                            return false;
                        if (item.IsEnabled == false)
                            return false;
                        return null;
                    }
                    if (name == "shortcut")
                    {
                        if (item?.Gesture == null)
                            return null;
                        if (item.Gesture.KeyModifiers == 0)
                            return null;
                        var lst = new List<string>();
                        var mod = item.Gesture;
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Control))
                            lst.Add("Control");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Alt))
                            lst.Add("Alt");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Shift))
                            lst.Add("Shift");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Meta))
                            lst.Add("Super");
                        lst.Add(item.Gesture.Key.ToString());
                        return new[] { lst.ToArray() };
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == NativeMenuItemToggleType.CheckBox)
                            return "checkmark";
                        if (item.ToggleType == NativeMenuItemToggleType.Radio)
                            return "radio";
                    }

                    if (name == "toggle-state")
                    {
                        if (item.ToggleType != NativeMenuItemToggleType.None)
                            return item.IsChecked ? 1 : 0;
                    }

                    if (name == "icon-data")
                    {
                        if (item.Icon != null)
                        {
                            var ms = new MemoryStream();
                            item.Icon.Save(ms);
                            return ms.ToArray();
                        }
                    }

                    if (name == "children-display")
                        return menu != null ? "submenu" : null;
                }

                return null;
            }

            private List<KeyValuePair<string, object>> _reusablePropertyList = new List<KeyValuePair<string, object>>();
            KeyValuePair<string, object>[] GetProperties((NativeMenuItemBase item, NativeMenu menu) i, string[] names)
            {
                if (names?.Length > 0 != true)
                    names = AllProperties;
                _reusablePropertyList.Clear();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v != null)
                        _reusablePropertyList.Add(new KeyValuePair<string, object>(n, v));
                }

                return _reusablePropertyList.ToArray();
            }

            
            public Task SetAsync(string prop, object val) => Task.CompletedTask;

            public Task<(uint revision, (int, KeyValuePair<string, object>[], object[]) layout)> GetLayoutAsync(
                int ParentId, int RecursionDepth, string[] PropertyNames)
            {
                var menu = GetMenu(ParentId);
                var rv = (_revision, GetLayout(menu.item, menu.menu, RecursionDepth, PropertyNames));
                if (!IsNativeMenuExported)
                {
                    IsNativeMenuExported = true;
                    Dispatcher.UIThread.Post(() =>
                    {
                        OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty);
                    });
                }
                return Task.FromResult(rv);
            }

            (int, KeyValuePair<string, object>[], object[]) GetLayout(NativeMenuItemBase item, NativeMenu menu, int depth, string[] propertyNames)
            {
                var id = item == null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = (depth == 0 || menu == null) ? new object[0] : new object[menu.Items.Count];
                if(menu != null)
                    for (var c = 0; c < children.Length; c++)
                    {
                        var ch = menu.Items[c];

                        children[c] = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                    }

                return (id, props, children);
            }

            public Task<(int, KeyValuePair<string, object>[])[]> GetGroupPropertiesAsync(int[] Ids, string[] PropertyNames)
            {
                var arr = new (int, KeyValuePair<string, object>[])[Ids.Length];
                for (var c = 0; c < Ids.Length; c++)
                {
                    var id = Ids[c];
                    var item = GetMenu(id);
                    var props = GetProperties(item, PropertyNames);
                    arr[c] = (id, props);
                }

                return Task.FromResult(arr);
            }

            public async Task<object> GetPropertyAsync(int Id, string Name)
            {
                return GetProperty(GetMenu(Id), Name) ?? 0;
            }



            public void HandleEvent(int id, string eventId, object data, uint timestamp)
            {
                if (eventId == "clicked")
                {
                    var item = GetMenu(id).item;

                    if (item is NativeMenuItem menuItem && item is INativeMenuItemExporterEventsImplBridge bridge)
                    {
                        if (menuItem?.IsEnabled == true)
                            bridge?.RaiseClicked();
                    }
                }
            }
            
            public Task EventAsync(int Id, string EventId, object Data, uint Timestamp)
            {
                HandleEvent(Id, EventId, Data, Timestamp);
                return Task.CompletedTask;
            }

            public Task<int[]> EventGroupAsync((int id, string eventId, object data, uint timestamp)[] Events)
            {
                foreach (var e in Events)
                    HandleEvent(e.id, e.eventId, e.data, e.timestamp);
                return Task.FromResult(new int[0]);
            }

            public async Task<bool> AboutToShowAsync(int Id)
            {
                return false;
            }

            public async Task<(int[] updatesNeeded, int[] idErrors)> AboutToShowGroupAsync(int[] Ids)
            {
                return (new int[0], new int[0]);
            }

            #region Events

#pragma warning disable CS0067
            private event Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)>
                ItemsPropertiesUpdated;
            private event Action<(uint revision, int parent)> LayoutUpdated;
            private event Action<(int id, uint timestamp)> ItemActivationRequested;
            private event Action<PropertyChanges> PropertiesChanged;
#pragma warning restore CS0067

            async Task<IDisposable> IDBusMenu.WatchItemsPropertiesUpdatedAsync(Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)> handler, Action<Exception> onError)
            {
                ItemsPropertiesUpdated += handler;
                return Disposable.Create(() => ItemsPropertiesUpdated -= handler);
            }
            async Task<IDisposable> IDBusMenu.WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler, Action<Exception> onError)
            {
                LayoutUpdated += handler;
                return Disposable.Create(() => LayoutUpdated -= handler);
            }

            async Task<IDisposable> IDBusMenu.WatchItemActivationRequestedAsync(Action<(int id, uint timestamp)> handler, Action<Exception> onError)
            {
                ItemActivationRequested+= handler;
                return Disposable.Create(() => ItemActivationRequested -= handler);
            }

            async Task<IDisposable> IFreeDesktopDBusProperties.WatchPropertiesAsync(Action<PropertyChanges> handler)
            {
                PropertiesChanged += handler;
                return Disposable.Create(() => PropertiesChanged -= handler);
            }

            #endregion
        }
    }
}
