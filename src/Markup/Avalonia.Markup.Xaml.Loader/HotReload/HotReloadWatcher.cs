using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Logging;
using Avalonia.Threading;
using XamlX.IL;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class Watcher
    {
        private static readonly ConcurrentDictionary<string, FileSystemWatcher> s_watchers;

        static Watcher()
        {
            s_watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        }

        public static FileSystemWatcher GetOrCreate(string directory)
        {
            if (s_watchers.TryGetValue(directory, out var watcher))
            {
                return watcher;
            }

            watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.FileName
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.Attributes
                               | NotifyFilters.Size
                               | NotifyFilters.LastWrite
                               | NotifyFilters.LastAccess
                               | NotifyFilters.CreationTime
                               | NotifyFilters.Security,
                EnableRaisingEvents = true
            };

            s_watchers[directory] = watcher;

            return watcher;
        }
    }

    public static class HotReloadWatcher
    {
        public static void Register<T>([CallerFilePath] string filePath = null)
        {
            string directoryName = Path.GetDirectoryName(filePath);
            string xamlFilePath = filePath.Replace("xaml.cs", "xaml");
            
            var instructions = LoadFile<T>(xamlFilePath, false);

            var watcher = Watcher.GetOrCreate(directoryName);

            watcher.Changed += (_, args) =>
            {
                if (args.FullPath != xamlFilePath)
                {
                    return;
                }

                try
                {
                    var newInstructions = LoadFile<T>(xamlFilePath, true);
                    var actions = IlDiffer.Diff(instructions, newInstructions);
                    
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var action in actions)
                        {
                            foreach (var obj in ObjectStorage.GetLiveObjects(xamlFilePath))
                            {
                                action.Apply(obj);
                            }
                        }

                        instructions = newInstructions;
                    });
                }
                catch (System.Exception exception)
                {
                    Logger
                        .TryGet(LogEventLevel.Error, "HotReload")
                        ?.Log(null, "Exception occured during HotReload. {Exception}", exception);
                }
            };
        }

        private static List<RecordingIlEmitter.RecordedInstruction> LoadFile<T>(
            string filePath,
            bool patchIl)
        {
            string xaml;

            do
            {
                try
                {
                    xaml = File.ReadAllText(filePath);
                    break;
                }
                catch (IOException)
                {
                }
            } while (true);

            return AvaloniaXamlAstLoader.Load(
                xaml,
                filePath,
                typeof(T),
                typeof(T).Assembly,
                patchIl: patchIl);
        }
    }
}
