// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public readonly struct IndexPath : IComparable<IndexPath>, IEquatable<IndexPath>
    {
        public static readonly IndexPath Unselected = default;

        private readonly int _index;
        private readonly int[]? _path;

        public IndexPath(int index)
        {
            _index = index + 1;
            _path = null;
        }

        public IndexPath(int groupIndex, int itemIndex)
        {
            _index = 0;
            _path = new[] { groupIndex, itemIndex };
        }

        public IndexPath(params int[] indexes)
        {
            _index = 0;
            _path = indexes;
        }

        public IndexPath(IEnumerable<int>? indexes)
        {
            if (indexes != null)
            {
                _index = 0;
                _path = indexes.ToArray();
            }
            else
            {
                _index = 0;
                _path = null;
            }
        }

        private IndexPath(int[] basePath, int index)
        {
            basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            
            _index = 0;
            _path = new int[basePath.Length + 1];
            Array.Copy(basePath, _path, basePath.Length);
            _path[basePath.Length] = index;
        }

        public int GetSize() => _path?.Length ?? (_index == 0 ? 0 : 1);

        public int GetAt(int index)
        {
            if (index >= GetSize())
            {
                throw new IndexOutOfRangeException();
            }

            return _path?[index] ?? (_index - 1);
        }

        public int CompareTo(IndexPath other)
        {
            var rhsPath = other;
            int compareResult = 0;
            int lhsCount = GetSize();
            int rhsCount = rhsPath.GetSize();

            if (lhsCount == 0 || rhsCount == 0)
            {
                // one of the paths are empty, compare based on size
                compareResult = (lhsCount - rhsCount);
            }
            else
            {
                // both paths are non-empty, but can be of different size
                for (int i = 0; i < Math.Min(lhsCount, rhsCount); i++)
                {
                    if (GetAt(i) < rhsPath.GetAt(i))
                    {
                        compareResult = -1;
                        break;
                    }
                    else if (GetAt(i) > rhsPath.GetAt(i))
                    {
                        compareResult = 1;
                        break;
                    }
                }

                // if both match upto min(lhsCount, rhsCount), compare based on size
                compareResult = compareResult == 0 ? (lhsCount - rhsCount) : compareResult;
            }

            if (compareResult != 0)
            {
                compareResult = compareResult > 0 ? 1 : -1;
            }

            return compareResult;
        }

        public IndexPath CloneWithChildIndex(int childIndex)
        {
            if (_path != null)
            {
                return new IndexPath(_path, childIndex);
            }
            else if (_index != 0)
            {
                return new IndexPath(_index - 1, childIndex);
            }
            else
            {
                return new IndexPath(childIndex);
            }
        }

        public override string ToString()
        {
            if (_path != null)
            {
                return "R" + string.Join(".", _path);
            }
            else if (_index != 0)
            {
                return "R" + (_index - 1);
            }
            else
            {
                return "R";
            }
        }

        public override bool Equals(object? obj) => obj is IndexPath other && Equals(other);

        public bool Equals(IndexPath other) => CompareTo(other) == 0;

        public override int GetHashCode()
        {
            var hashCode = -504981047;

            if (_path != null)
            {
                foreach (var i in _path)
                {
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                }
            }
            else
            {
                hashCode = hashCode * -1521134295 + _index.GetHashCode();
            }

            return hashCode;
        }
        
        internal bool IsAncestorOf(in IndexPath other)
        {
            if (other.GetSize() <= GetSize())
            {
                return false;
            }

            var size = GetSize();

            for (int i = 0; i < size; i++)
            {
                if (GetAt(i) != other.GetAt(i))
                {
                    return false;
                }
            }

            return true;
        }

        internal int? GetLeaf()
        {
            if (GetSize() > 0)
            {
                return GetAt(GetSize() - 1);
            }

            return null;
        }

        internal IndexPath GetParent()
        {
            if (GetSize() == 0)
            {
                throw new InvalidOperationException("Cannot get parent of root index.");
            }

            if (_path is null)
            {
                return default;
            }
            else if (_path.Length == 2)
            {
                return new IndexPath(_path[0]);
            }
            else
            {
                var path = new int[_path.Length - 1];
                Array.Copy(_path, path, _path.Length - 1);
                return new IndexPath(path);
            }
        }

        internal int[] ToArray()
        {
            var result = new int[GetSize()];

            if (_path is object)
            {
                _path.CopyTo(result, 0);
            }
            else if (result.Length > 0)
            {
                result[0] = _index - 1;
            }

            return result;
        }

        public static bool operator <(IndexPath x, IndexPath y) { return x.CompareTo(y) < 0; }
        public static bool operator >(IndexPath x, IndexPath y) { return x.CompareTo(y) > 0; }
        public static bool operator <=(IndexPath x, IndexPath y) { return x.CompareTo(y) <= 0; }
        public static bool operator >=(IndexPath x, IndexPath y) { return x.CompareTo(y) >= 0; }
        public static bool operator ==(IndexPath x, IndexPath y) { return x.CompareTo(y) == 0; }
        public static bool operator !=(IndexPath x, IndexPath y) { return x.CompareTo(y) != 0; }
        public static bool operator ==(IndexPath? x, IndexPath? y) { return (x ?? default).CompareTo(y ?? default) == 0; }
        public static bool operator !=(IndexPath? x, IndexPath? y) { return (x ?? default).CompareTo(y ?? default) != 0; }
    }
}
