using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ListBoxPageViewModel : ViewModelBase
    {
        private IList _items;
        private bool _multiple;
        private bool _toggle;
        private bool _alwaysSelected;
        private bool _autoScrollToSelectedItem = true;
        private ItemTypes _itemType;
        private IObservable<SelectionMode> _selectionMode;

        public ListBoxPageViewModel()
        {
            this.WhenAnyValue(x => x.ItemType)
                .Subscribe(x =>
                {
                    Items = x switch
                    {
                        ItemTypes.FromDataTemplate => new ObservableCollection<string>(
                            Enumerable.Range(0, 10000).Select(i => GenerateContent(i))),
                        ItemTypes.ListBoxItems => new ObservableCollection<ListBoxItem>(
                            Enumerable.Range(0, 200).Select(i => GenerateItem(i))),
                        _ => throw new NotSupportedException(),
                    };
                });
            
            Selection = new SelectionModel<object>();
            Selection.Select(1);

            _selectionMode = this.WhenAnyValue(
                x => x.Multiple,
                x => x.Toggle,
                x => x.AlwaysSelected,
                (m, t, a) =>
                    (m ? Avalonia.Controls.SelectionMode.Multiple : 0) |
                    (t ? Avalonia.Controls.SelectionMode.Toggle : 0) |
                    (a ? Avalonia.Controls.SelectionMode.AlwaysSelected : 0));

            AddItemCommand = MiniCommand.Create(() =>
            {
                if (ItemType == ItemTypes.FromDataTemplate)
                {
                    Items.Add(GenerateContent(Items.Count));
                }
                else
                {
                    Items.Add(GenerateItem(Items.Count));
                }
            });

            RemoveItemCommand = MiniCommand.Create(() =>
            {
                var items = Selection.SelectedItems.ToList();

                foreach (var item in items)
                {
                    Items.Remove(item);
                }
            });

            SelectRandomItemCommand = MiniCommand.Create(() =>
            {
                var random = new Random();

                if (Items.Count > 0)
                {
                    using (Selection.BatchUpdate())
                    {
                        Selection.Clear();
                        Selection.Select(random.Next(Items.Count - 1));
                    }
                }
            });
        }

        public IList Items 
        {
            get => _items;
            private set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        public SelectionModel<object> Selection { get; }
        public IObservable<SelectionMode> SelectionMode => _selectionMode;

        public bool Multiple
        {
            get => _multiple;
            set => this.RaiseAndSetIfChanged(ref _multiple, value);
        }

        public bool Toggle
        {
            get => _toggle;
            set => this.RaiseAndSetIfChanged(ref _toggle, value);
        }

        public bool AlwaysSelected
        {
            get => _alwaysSelected;
            set => this.RaiseAndSetIfChanged(ref _alwaysSelected, value);
        }

        public bool AutoScrollToSelectedItem
        {
            get => _autoScrollToSelectedItem;
            set => this.RaiseAndSetIfChanged(ref _autoScrollToSelectedItem, value);
        }

        public ItemTypes ItemType
        {
            get => _itemType;
            set => this.RaiseAndSetIfChanged(ref _itemType, value);
        }

        public MiniCommand AddItemCommand { get; }
        public MiniCommand RemoveItemCommand { get; }
        public MiniCommand SelectRandomItemCommand { get; }

        private string GenerateContent(int index) => $"Item {index}";
        private ListBoxItem GenerateItem(int index) => new ListBoxItem 
        { 
            Content = "ListBoxItem " + GenerateContent(index) 
        };

        public enum ItemTypes
        {
            FromDataTemplate,
            ListBoxItems,
        }
    }
}
