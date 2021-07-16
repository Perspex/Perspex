using System;
using System.IO;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    partial interface IAvnMenuItem
    {
        
    }
}
namespace Avalonia.Native.Interop.Impl
{
    partial class __MicroComIAvnMenuItemProxy
    {
        private __MicroComIAvnMenuProxy _subMenu;
        private CompositeDisposable _propertyDisposables = new CompositeDisposable();
        private IDisposable _currentActionDisposable;

        public NativeMenuItemBase ManagedMenuItem { get; set; }

        private void UpdateTitle(string title) => SetTitle(title ?? "");

        private void UpdateIsChecked(bool isChecked) => SetIsChecked(isChecked.AsComBool());

        private void UpdateToggleType(NativeMenuItemToggleType toggleType)
        {
            SetToggleType((AvnMenuItemToggleType)toggleType);
        }

        private unsafe void UpdateIcon (IBitmap icon)
        {
            if(icon is null)
            {
                SetIcon(null, IntPtr.Zero);
            }
            else
            {
                using(var ms = new MemoryStream())
                {
                    icon.Save(ms);

                    var imageData = ms.ToArray();

                    fixed(void* ptr = imageData)
                    {
                        SetIcon(ptr, new IntPtr(imageData.Length));
                    }
                }
            }
        }

        private void UpdateGesture(Input.KeyGesture gesture)
        {
            var modifiers = gesture == null ? AvnInputModifiers.AvnInputModifiersNone : (AvnInputModifiers)gesture.KeyModifiers;
            var key = gesture == null ? AvnKey.AvnKeyNone : (AvnKey)gesture.Key;
            SetGesture(key, modifiers);
        }

        private void UpdateAction(NativeMenuItem item)
        {
            _currentActionDisposable?.Dispose();

            var action = new PredicateCallback(() =>
            {
                if (item.Command != null || item.HasClickHandlers)
                {
                    return item.IsEnabled;
                }

                return false;
            });

            var callback = new MenuActionCallback(() => { (item as INativeMenuItemExporterEventsImplBridge)?.RaiseClicked(); });

            _currentActionDisposable = Disposable.Create(() =>
            {
                action.Dispose();
                callback.Dispose();
            });

            SetAction(action, callback);
        }

        internal void Initialize(NativeMenuItemBase nativeMenuItem)
        {
            ManagedMenuItem = nativeMenuItem;

            if (ManagedMenuItem is NativeMenuItem item)
            {
                UpdateTitle(item.Header);

                UpdateGesture(item.Gesture);

                UpdateAction(ManagedMenuItem as NativeMenuItem);

                UpdateToggleType(item.ToggleType);

                UpdateIcon(item.Icon);

                UpdateIsChecked(item.IsChecked);

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.HeaderProperty)
                    .Subscribe(x => UpdateTitle(x)));

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.GestureProperty)
                    .Subscribe(x => UpdateGesture(x)));

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.CommandProperty)
                    .Subscribe(x => UpdateAction(ManagedMenuItem as NativeMenuItem)));

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.ToggleTypeProperty)
                    .Subscribe(x => UpdateToggleType(x)));

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.IsCheckedProperty)
                    .Subscribe(x => UpdateIsChecked(x)));

                _propertyDisposables.Add(ManagedMenuItem.GetObservable(NativeMenuItem.IconProperty)
                    .Subscribe(x => UpdateIcon(x)));
            }
        }

        internal void Deinitialize()
        {
            if (_subMenu != null)
            {
                SetSubMenu(null);
                _subMenu.Deinitialise();
                _subMenu.Dispose();
                _subMenu = null;
            }

            _propertyDisposables?.Dispose();
            _currentActionDisposable?.Dispose();
        }

        internal void Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenuItem item)
        {
            if (item != ManagedMenuItem)
            {
                throw new ArgumentException("The item does not match the menuitem being updated.", nameof(item));
            }

            if (item.Menu != null)
            {
                if (_subMenu == null)
                {
                    _subMenu = __MicroComIAvnMenuProxy.Create(factory);

                    _subMenu.Initialize(exporter, item.Menu, item.Header);

                    SetSubMenu(_subMenu);
                }

                _subMenu.Update(factory, item.Menu);
            }

            if (item.Menu == null && _subMenu != null)
            {
                _subMenu.Deinitialise();
                _subMenu.Dispose();

                SetSubMenu(null);
            }
        }
    }
}
