using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        private ListBox _listBox;
        private IItemsPresenter _presenter;
        private TextBlock _realizedCount;

        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();

            _listBox = this.FindControl<ListBox>("listBox");
            _realizedCount = this.FindControl<TextBlock>("realizedCount");
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            LayoutUpdated -= OnLayoutUpdated;
            _presenter = _listBox.Presenter;
            _presenter.VisualChildren.CollectionChanged += (s, e) => UpdateRealizedCount();
            UpdateRealizedCount();
        }

        private void UpdateRealizedCount()
        {
            _realizedCount.Text = $"{_presenter.VisualChildren.Count} containers realized";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
