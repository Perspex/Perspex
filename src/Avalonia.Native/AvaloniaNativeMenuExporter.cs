using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Native.Interop;
using Avalonia.Native.Interop.Impl;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class AvaloniaNativeMenuExporter : ITopLevelNativeMenuExporter
    {
        private IAvaloniaNativeFactory _factory;
        private bool _resetQueued = true;
        private bool _exported = false;
        private IAvnWindow _nativeWindow;
        private NativeMenu _menu;
        private __MicroComIAvnMenuProxy _nativeMenu;

        public AvaloniaNativeMenuExporter(IAvnWindow nativeWindow, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _nativeWindow = nativeWindow;

            DoLayoutReset();
        }

        public AvaloniaNativeMenuExporter(IAvaloniaNativeFactory factory)
        {
            _factory = factory;

            DoLayoutReset();
        }

        public bool IsNativeMenuExported => _exported;

        public event EventHandler OnIsNativeMenuExportedChanged;

        public void SetNativeMenu(NativeMenu menu)
        {
            _menu = menu == null ? new NativeMenu() : menu;
            DoLayoutReset(true);
        }

        internal void UpdateIfNeeded()
        {
            if (_resetQueued)
            {
                DoLayoutReset();
            }
        }

        private static NativeMenu CreateDefaultAppMenu()
        {
            var result = new NativeMenu();

            var aboutItem = new NativeMenuItem
            {
                Header = "About Avalonia",
            };

            aboutItem.Click += async (sender, e) =>
            {
                var dialog = new AboutAvaloniaDialog();

                var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                await dialog.ShowDialog(mainWindow);
            };

            result.Add(aboutItem);

            return result;
        }

        private void DoLayoutReset(bool forceUpdate = false)
        {
            if (_resetQueued || forceUpdate)
            {
                _resetQueued = false;

                if (_nativeWindow is null)
                {
                    var appMenu = NativeMenu.GetMenu(Application.Current);

                    if (appMenu == null)
                    {
                        appMenu = CreateDefaultAppMenu();
                        NativeMenu.SetMenu(Application.Current, appMenu);
                    }

                    SetMenu(appMenu);
                }
                else
                {
                    if (_menu != null)
                    {
                        SetMenu(_nativeWindow, _menu);
                    }
                }

                _exported = true;
            }
        }

        internal void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(() => DoLayoutReset(), DispatcherPriority.Background);
        }

        private void SetMenu(NativeMenu menu)
        {
            var menuItem = menu.Parent;

            var appMenuHolder = menuItem?.Parent;

            if (menu.Parent is null)
            {
                menuItem = new NativeMenuItem();
            }

            if (appMenuHolder is null)
            {
                appMenuHolder = new NativeMenu();

                appMenuHolder.Add(menuItem);
            }

            menuItem.Menu = menu;

            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = (__MicroComIAvnMenuProxy)__MicroComIAvnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, appMenuHolder, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, appMenuHolder);

            if (setMenu)
            {
                _factory.SetAppMenu(_nativeMenu);
            }
        }

        private void SetMenu(IAvnWindow avnWindow, NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComIAvnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");     

                setMenu = true;           
            }

            _nativeMenu.Update(_factory, menu);

            if(setMenu)
            {
                avnWindow.SetMainMenu(_nativeMenu);
            }
        }
    }
}
