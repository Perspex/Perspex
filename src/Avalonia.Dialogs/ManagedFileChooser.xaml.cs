using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;

namespace Avalonia.Dialogs
{
    internal class ManagedFileChooser : UserControl
    {
        private Control _quickLinksRoot;
        private ListBox _filesView;

        public ManagedFileChooser()
        {
            AvaloniaXamlLoader.Load(this);
            AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            _quickLinksRoot = this.FindControl<Control>("QuickLinks");
            _filesView = this.FindControl<ListBox>("Files");
        }

        ManagedFileChooserViewModel Model => DataContext as ManagedFileChooserViewModel;

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var model = (e.Source as StyledElement)?.DataContext as ManagedFileChooserItemViewModel;

            if (model == null)
            {
                return;
            }

            var isQuickLink = _quickLinksRoot.IsLogicalAncestorOf(e.Source as Control);
#pragma warning disable CS0618 // Type or member is obsolete
            if (e.ClickCount == 2 || isQuickLink)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (model.ItemType == ManagedFileChooserItemType.File)
                {
                    Model?.SelectSingleFile(model);
                }
                else
                {
                    Model?.Navigate(model.Path);
                }

                e.Handled = true;
            }
        }

        protected override async void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            var model = (DataContext as ManagedFileChooserViewModel);

            if (model == null)
            {
                return;
            }

            var preselected = model.SelectedItems.FirstOrDefault();

            if (preselected == null)
            {
                return;
            }

            //Let everything to settle down and scroll to selected item
            await Task.Delay(100);

            if (preselected != model.SelectedItems.FirstOrDefault())
            {
                return;
            }

            // Workaround for ListBox bug, scroll to the previous file
            var indexOfPreselected = model.Items.IndexOf(preselected);

            if (indexOfPreselected > 1)
            {
                _filesView.ScrollIntoView(indexOfPreselected - 1);
            }
        }
    }
}
