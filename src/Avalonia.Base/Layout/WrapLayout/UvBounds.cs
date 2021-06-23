// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Avalonia;
using Avalonia.Layout;

namespace Avalonia.Layout
{
    internal struct UvBounds
    {
        public UvBounds(Orientation orientation, Rect rect)
        {
            if (orientation == Orientation.Horizontal)
            {
                UMin = rect.Left;
                UMax = rect.Right;
                VMin = rect.Top;
                VMax = rect.Bottom;
            }
            else
            {
                UMin = rect.Top;
                UMax = rect.Bottom;
                VMin = rect.Left;
                VMax = rect.Right;
            }
        }

        public double UMin { get; }

        public double UMax { get; }

        public double VMin { get; }

        public double VMax { get; }
    }
}
