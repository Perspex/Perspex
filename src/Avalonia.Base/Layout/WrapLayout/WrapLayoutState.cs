// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Layout
{
    internal class WrapLayoutState
    {
        private List<WrapItem> _items = new List<WrapItem>();
        private VirtualizingLayoutContext _context;

        public WrapLayoutState(VirtualizingLayoutContext context)
        {
            this._context = context;
        }

        public Orientation Orientation { get; private set; }

        public UvMeasure Spacing { get; internal set; }

        public double AvailableU { get; internal set; }

        internal WrapItem GetItemAt(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (index <= (_items.Count - 1))
            {
                return _items[index];
            }
            else
            {
                WrapItem item = new WrapItem(index);
                _items.Add(item);
                return item;
            }
        }

        internal void Clear()
        {
            _items.Clear();
        }

        internal void RemoveFromIndex(int index)
        {
            if (index >= _items.Count)
            {
                // Item was added/removed but we haven't realized that far yet
                return;
            }

            int numToRemove = _items.Count - index;
            _items.RemoveRange(index, numToRemove);
        }

        internal void SetOrientation(Orientation orientation)
        {
            foreach (var item in _items.Where(i => i.Measure.HasValue))
            {
                UvMeasure measure = item.Measure.Value;
                double v = measure.V;
                measure.V = measure.U;
                measure.U = v;
                item.Measure = measure;
                item.Position = null;
            }

            Orientation = orientation;
            AvailableU = 0;
        }

        internal void ClearPositions()
        {
            foreach (var item in _items)
            {
                item.Position = null;
            }
        }

        internal double GetHeight()
        {
            if (_items.Count == 0)
            {
                return 0;
            }

            bool calculateAverage = true;
            if ((_items.Count == _context.ItemCount) && _items[_items.Count - 1].Position.HasValue)
            {
                calculateAverage = false;
            }

            UvMeasure? lastPosition = null;
            double maxV = 0;

            int itemCount = _items.Count;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Position == null)
                {
                    itemCount--;
                    continue;
                }

                if (lastPosition != null)
                {
                    if (lastPosition.Value.V > item.Position.Value.V)
                    {
                        // This is a row above the last item. Exit and calculate the average
                        break;
                    }
                }

                lastPosition = item.Position;
                maxV = Math.Max(maxV, item.Measure.Value.V);
            }

            double totalHeight = lastPosition.Value.V + maxV;
            if (calculateAverage)
            {
                return (totalHeight / itemCount) * _context.ItemCount;
            }
            else
            {
                return totalHeight;
            }
        }

        internal void RecycleElementAt(int index)
        {
            var element = _context.GetOrCreateElementAt(index);
            _context.RecycleElement(element);
        }
    }
}
