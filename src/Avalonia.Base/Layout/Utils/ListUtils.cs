using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Layout.Utils
{
    internal static class ListUtils
    {
        public static void Resize<T>(this List<T> list, int size, T value)
        {
            int cur = list.Count;

            if (size < cur)
            {
                list.RemoveRange(size, cur - size);
            }
            else if (size > cur)
            {
                if (size > list.Capacity)
                {
                    list.Capacity = size;
                }

                list.AddRange(Enumerable.Repeat(value, size - cur));
            }
        }

        public static void Resize<T>(this List<T> list, int count)
        {
            Resize(list, count, default);
        }
    }
}
