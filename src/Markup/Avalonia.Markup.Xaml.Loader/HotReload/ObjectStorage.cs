using System;
using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.HotReload
{
    // TODO: This class can be injected while compiling the xaml.
    public class ObjectStorage
    {
        private static readonly Dictionary<string, List<WeakReference<object>>> _liveObjects = new Dictionary<string, List<WeakReference<object>>>();

        public static void RegisterLiveObject(object obj, string file)
        {
            if (!_liveObjects.TryGetValue(file, out var list))
            {
                list = new List<WeakReference<object>>();
                _liveObjects[file] = list;
            }

            list.Add(new WeakReference<object>(obj));
        }
        
        public static IList<object> GetLiveObjects(string file)
        {
            if (!_liveObjects.TryGetValue(file, out var list))
            {
                return Array.Empty<object>();
            }

            var result = new List<object>();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var reference = list[i];
                if (!reference.TryGetTarget(out var obj))
                {
                    list.RemoveAt(i);
                    continue;
                }

                result.Add(obj);
            }

            return result;
        }
    }
}
