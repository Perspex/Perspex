using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class MenuPageViewModel : ViewModelBase
    {
        private bool _shouldConstrainToRootBounds = false;

        public Control View { get; set; }
        public MenuPageViewModel()
        {
            OpenCommand = MiniCommand.CreateFromTask(Open);
            SaveCommand = MiniCommand.Create(Save);
            OpenRecentCommand = MiniCommand.Create<string>(OpenRecent);

            var recentItems = new[]
            {
                new MenuItemViewModel
                {
                    Header = "File1.txt",
                    Command = OpenRecentCommand,
                    CommandParameter = @"c:\foo\File1.txt"
                },
                new MenuItemViewModel
                {
                    Header = "File2.txt",
                    Command = OpenRecentCommand,
                    CommandParameter = @"c:\foo\File2.txt"
                },
            };

            RecentItems = recentItems;
            MenuItems = new[]
            {
                new MenuItemViewModel
                {
                    Header = "_File",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "O_pen...", Command = OpenCommand },
                        new MenuItemViewModel { Header = "Save", Command = SaveCommand },
                        new MenuItemViewModel { Header = "-" },
                        new MenuItemViewModel
                        {
                            Header = "Recent",
                            Items = recentItems
                        },
                    }
                },
                new MenuItemViewModel
                {
                    Header = "_Edit",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "_Copy" },
                        new MenuItemViewModel { Header = "_Paste" },
                    }
                }
            };
        }

        public bool ShouldConstrainToRootBounds
        {
            get => _shouldConstrainToRootBounds;
            set => RaiseAndSetIfChanged(ref _shouldConstrainToRootBounds, value);
        }

        public IReadOnlyList<MenuItemViewModel> MenuItems { get; set; }
        public IReadOnlyList<MenuItemViewModel> RecentItems { get; set; }
        public MiniCommand OpenCommand { get; }
        public MiniCommand SaveCommand { get; }
        public MiniCommand OpenRecentCommand { get; }

        public async Task Open()
        {
            var window = View?.GetVisualRoot() as Window;
            if (window == null)
                return;
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(window);

            if (result != null)
            {
                foreach (var path in result)
                {
                    System.Diagnostics.Debug.WriteLine($"Opened: {path}");
                }
            }
        }

        public void Save()
        {
            System.Diagnostics.Debug.WriteLine("Save");
        }

        public void OpenRecent(string path)
        {
            System.Diagnostics.Debug.WriteLine($"Open recent: {path}");
        }
    }
}
