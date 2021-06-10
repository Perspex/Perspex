using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class ZIndexComparer : IComparer<IVisual>
    {
        public static readonly ZIndexComparer Instance = new ZIndexComparer();
        public static readonly Comparison<IVisual> ComparisonInstance = Instance.Compare;

        public int Compare(IVisual x, IVisual y) => (x?.ZIndex ?? 0).CompareTo(y?.ZIndex ?? 0);
    }
}
