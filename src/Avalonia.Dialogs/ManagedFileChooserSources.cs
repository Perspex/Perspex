using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace Avalonia.Dialogs
{
    internal class ManagedFileChooserSources
    {
        public Func<ManagedFileChooserNavigationItem[]> GetUserDirectories { get; set; }
            = DefaultGetUserDirectories;

        public Func<ManagedFileChooserNavigationItem[]> GetFileSystemRoots { get; set; }
            = DefaultGetFileSystemRoots;

        public Func<ManagedFileChooserSources, ManagedFileChooserNavigationItem[]> GetAllItemsDelegate { get; set; }
            = DefaultGetAllItems;

        public ManagedFileChooserNavigationItem[] GetAllItems() => GetAllItemsDelegate(this);
        public static readonly ObservableCollection<MountedVolumeInfo> MountedVolumes = new ObservableCollection<MountedVolumeInfo>();

        public static ManagedFileChooserNavigationItem[] DefaultGetAllItems(ManagedFileChooserSources sources)
        {
            return sources.GetUserDirectories().Concat(sources.GetFileSystemRoots()).ToArray();
        }

        private static Environment.SpecialFolder[] s_folders = new[]
        {
            Environment.SpecialFolder.Desktop,
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolder.MyMusic,
            Environment.SpecialFolder.MyPictures,
            Environment.SpecialFolder.MyVideos
        };

        public static ManagedFileChooserNavigationItem[] DefaultGetUserDirectories()
        {
            return s_folders.Select(Environment.GetFolderPath).Distinct()
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Where(Directory.Exists)
                .Select(d => new ManagedFileChooserNavigationItem
                {
                    ItemType = ManagedFileChooserItemType.Folder,
                    Path = d,
                    DisplayName = Path.GetFileName(d)
                }).ToArray();
        }

        public static ManagedFileChooserNavigationItem[] DefaultGetFileSystemRoots()
        {
            return MountedVolumes
                   .Select(x =>
                   {
                       var displayName = x.VolumeLabel;

                       if (displayName == null & x.VolumeSizeBytes > 0)
                       {
                           displayName = $"{ByteSizeHelper.ToString(x.VolumeSizeBytes)} Volume";
                       };

                       try
                       {
                           Directory.GetFiles(x.VolumePath);
                       }
                       catch (Exception)
                       {
                           return null;
                       }

                       return new ManagedFileChooserNavigationItem
                       {
                           ItemType = ManagedFileChooserItemType.Volume,
                           DisplayName = displayName,
                           Path = x.VolumePath
                       };
                   })
                   .Where(x => x != null)
                   .ToArray();
        }
    }
}
