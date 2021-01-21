using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Controls.Selection;
using MiniMvvm;

namespace VirtualizationDemo.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private int _itemCount = 200;
        private string _newItemString = "New Item";
        private int _newItemIndex;
        private AvaloniaList<ItemViewModel> _items;
        private string _prefix = "Item";
        private ScrollBarVisibility _horizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        private ScrollBarVisibility _verticalScrollBarVisibility = ScrollBarVisibility.Auto;
        private Orientation _orientation = Orientation.Vertical;

        public MainWindowViewModel()
        {
            this.WhenAnyValue(x => x.ItemCount).Subscribe(ResizeItems);
            RecreateCommand = MiniCommand.Create(() => Recreate());

            AddItemCommand = MiniCommand.Create(() => AddItem());

            RemoveItemCommand = MiniCommand.Create(() => Remove());

            SelectFirstCommand = MiniCommand.Create(() => SelectItem(0));

            SelectLastCommand = MiniCommand.Create(() => SelectItem(Items.Count - 1));
        }

        public string NewItemString
        {
            get { return _newItemString; }
            set { this.RaiseAndSetIfChanged(ref _newItemString, value); }
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set { this.RaiseAndSetIfChanged(ref _itemCount, value); }
        }

        public SelectionModel<ItemViewModel> Selection { get; } = new SelectionModel<ItemViewModel>();

        public AvaloniaList<ItemViewModel> Items
        {
            get { return _items; }
            private set { this.RaiseAndSetIfChanged(ref _items, value); }
        }

        public Orientation Orientation
        {
            get { return _orientation; }
            set { this.RaiseAndSetIfChanged(ref _orientation, value); }
        }

        public IEnumerable<Orientation> Orientations =>
            Enum.GetValues(typeof(Orientation)).Cast<Orientation>();

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return _horizontalScrollBarVisibility; }
            set { this.RaiseAndSetIfChanged(ref _horizontalScrollBarVisibility, value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return _verticalScrollBarVisibility; }
            set { this.RaiseAndSetIfChanged(ref _verticalScrollBarVisibility, value); }
        }

        public IEnumerable<ScrollBarVisibility> ScrollBarVisibilities =>
            Enum.GetValues(typeof(ScrollBarVisibility)).Cast<ScrollBarVisibility>();

        public MiniCommand AddItemCommand { get; private set; }
        public MiniCommand RecreateCommand { get; private set; }
        public MiniCommand RemoveItemCommand { get; private set; }
        public MiniCommand SelectFirstCommand { get; private set; }
        public MiniCommand SelectLastCommand { get; private set; }

        public void RandomizeSize()
        {
            var random = new Random();

            foreach (var i in Items)
            {
                i.Height = random.Next(240) + 10;
            }
        }

        public void ResetSize()
        {
            foreach (var i in Items)
            {
                i.Height = double.NaN;
            }
        }

        private void ResizeItems(int count)
        {
            if (Items == null)
            {
                var items = Enumerable.Range(0, count)
                    .Select(x => new ItemViewModel(x));
                Items = new AvaloniaList<ItemViewModel>(items);
            }
            else if (count > Items.Count)
            {
                var items = Enumerable.Range(Items.Count, count - Items.Count)
                    .Select(x => new ItemViewModel(x));
                Items.AddRange(items);
            }
            else if (count < Items.Count)
            {
                Items.RemoveRange(count, Items.Count - count);
            }
        }

        private void AddItem()
        {
            var index = Items.Count;

            if (Selection.SelectedItems.Count > 0)
            {
                index = Selection.SelectedIndex;
            }

            Items.Insert(index, new ItemViewModel(_newItemIndex++, NewItemString));
        }

        private void Remove()
        {
            if (Selection.SelectedItems.Count > 0)
            {
                Items.RemoveAll(Selection.SelectedItems.ToList());
            }
        }

        private void Recreate()
        {
            _prefix = _prefix == "Item" ? "Recreated" : "Item";
            var items = Enumerable.Range(0, _itemCount)
                .Select(x => new ItemViewModel(x, _prefix));
            Items = new AvaloniaList<ItemViewModel>(items);
        }

        private void SelectItem(int index)
        {
            Selection.SelectedIndex = index;
        }
    }
}
