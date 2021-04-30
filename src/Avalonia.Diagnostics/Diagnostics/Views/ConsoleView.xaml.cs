using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views
{
    internal class ConsoleView : UserControl
    {
        private readonly ListBox _historyList;
        private readonly TextBox _input;

        public ConsoleView()
        {
            this.InitializeComponent();
            _historyList = this.FindControl<ListBox>("historyList");
            ((ILogical)_historyList).LogicalChildren.CollectionChanged += HistoryChanged;
            _input = this.FindControl<TextBox>("input");
            _input.KeyDown += InputKeyDown;
        }

        public void FocusInput() => _input.Focus();

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems[0] is IControl control)
            {
                DispatcherTimer.RunOnce(control.BringIntoView, TimeSpan.Zero);
            }
        }

        private void InputKeyDown(object sender, KeyEventArgs e)
        {
            var vm = (ConsoleViewModel)DataContext;

            switch (e.Key)
            {
                case Key.Enter:
                    _ = vm.Execute();
                    e.Handled = true;
                    break;
                case Key.Up:
                    vm.HistoryUp();
                    _input.CaretIndex = _input.Text.Length;
                    e.Handled = true;
                    break;
                case Key.Down:
                    vm.HistoryDown();
                    _input.CaretIndex = _input.Text.Length;
                    e.Handled = true;
                    break;
            }
        }
    }
}
