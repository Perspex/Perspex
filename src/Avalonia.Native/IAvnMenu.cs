using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    class MenuEvents : CallbackBase, IAvnMenuEvents
    {
        private IAvnMenu _parent;

        public void Initialise(IAvnMenu parent)
        {
            _parent = parent;
        }

        public void NeedsUpdate()
        {
            _parent?.RaiseNeedsUpdate();
        }

        public void Opening()
        {
            _parent?.RaiseOpening();
        }

        public void Closed()
        {
            _parent?.RaiseClosed();
        }
    }

    partial interface IAvnMenu
    {
        void RaiseNeedsUpdate();
        void RaiseOpening();
        void RaiseClosed();
        void Deinitialise();
    }
}
namespace Avalonia.Native.Interop.Impl
{
    partial class __MicroComIAvnMenuProxy
    {
        private MenuEvents _events;
        private AvaloniaNativeMenuExporter _exporter;
        private List<__MicroComIAvnMenuItemProxy> _menuItems = new List<__MicroComIAvnMenuItemProxy>();
        private Dictionary<NativeMenuItemBase, __MicroComIAvnMenuItemProxy> _menuItemLookup = new Dictionary<NativeMenuItemBase, __MicroComIAvnMenuItemProxy>();
        private CompositeDisposable _propertyDisposables = new CompositeDisposable();

        public void RaiseNeedsUpdate()
        {
            (ManagedMenu as INativeMenuExporterEventsImplBridge).RaiseNeedsUpdate();

            _exporter.UpdateIfNeeded();
        }

        public void RaiseOpening()
        {
            (ManagedMenu as INativeMenuExporterEventsImplBridge).RaiseOpening();
        }

        public void RaiseClosed()
        {
            (ManagedMenu as INativeMenuExporterEventsImplBridge).RaiseClosed();
        }

        internal NativeMenu ManagedMenu { get; private set; }

        public static __MicroComIAvnMenuProxy Create(IAvaloniaNativeFactory factory)
        {
            var events = new MenuEvents();

            var menu = (__MicroComIAvnMenuProxy)factory.CreateMenu(events);

            events.Initialise(menu);

            menu._events = events;

            return menu;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _events.Dispose();
            }
        }

        private void RemoveAndDispose(__MicroComIAvnMenuItemProxy item)
        {
            _menuItemLookup.Remove(item.ManagedMenuItem);
            _menuItems.Remove(item);
            RemoveItem(item);

            item.Deinitialize();
            item.Dispose();
        }

        private void MoveExistingTo(int index, __MicroComIAvnMenuItemProxy item)
        {
            _menuItems.Remove(item);
            _menuItems.Insert(index, item);

            RemoveItem(item);
            InsertItem(index, item);
        }

        private __MicroComIAvnMenuItemProxy CreateNewAt(IAvaloniaNativeFactory factory, int index, NativeMenuItemBase item)
        {
            var result = CreateNew(factory, item);

            result.Initialize(item);

            _menuItemLookup.Add(result.ManagedMenuItem, result);
            _menuItems.Insert(index, result);

            InsertItem(index, result);

            return result;
        }

        private __MicroComIAvnMenuItemProxy CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = (__MicroComIAvnMenuItemProxy)(item is NativeMenuItemSeparator ?
                factory.CreateMenuItemSeparator() :
                factory.CreateMenuItem());
            nativeItem.ManagedMenuItem = item;

            return nativeItem;
        }

        internal void Initialize(AvaloniaNativeMenuExporter exporter, NativeMenu managedMenu, string title)
        {
            _exporter = exporter;
            ManagedMenu = managedMenu;

            ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged += OnMenuItemsChanged;

            if (!string.IsNullOrWhiteSpace(title)) 
                SetTitle(title);
        }

        public void Deinitialise()
        {
            ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged -= OnMenuItemsChanged;

            foreach (var item in _menuItems)
            {
                item.Deinitialize();
                item.Dispose();
            }
        }

        internal void Update(IAvaloniaNativeFactory factory, NativeMenu menu)
        {
            if (menu != ManagedMenu)
            {
                throw new ArgumentException("The menu being updated does not match.", nameof(menu));
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                __MicroComIAvnMenuItemProxy nativeItem;

                if (i >= _menuItems.Count)
                {
                    nativeItem = CreateNewAt(factory, i, menu.Items[i]);
                }
                else if (menu.Items[i] == _menuItems[i].ManagedMenuItem)
                {
                    nativeItem = _menuItems[i];
                }
                else if (_menuItemLookup.TryGetValue(menu.Items[i], out nativeItem))
                {
                    MoveExistingTo(i, nativeItem);
                }
                else
                {
                    nativeItem = CreateNewAt(factory, i, menu.Items[i]);
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    nativeItem.Update(_exporter, factory, nmi);
                }
            }

            while (_menuItems.Count > menu.Items.Count)
            {
                RemoveAndDispose(_menuItems[_menuItems.Count - 1]);
            }
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
