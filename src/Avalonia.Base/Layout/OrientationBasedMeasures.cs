// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

namespace Avalonia.Layout
{
    internal enum ScrollOrientation
    {
        Vertical,
        Horizontal,
    }

    internal class OrientationBasedMeasures
    {
        public ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;

        public double Major(in Size size) => ScrollOrientation == ScrollOrientation.Vertical ? size.Height : size.Width;
        public double Minor(in Size size) => ScrollOrientation == ScrollOrientation.Vertical ? size.Width : size.Height;
        public double MajorSize(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.Height : rect.Width;
        public double MinorSize(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.Width : rect.Height;
        public double MajorStart(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.Y : rect.X;
        public double MinorStart(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.X : rect.Y;
        public double MajorEnd(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.Bottom : rect.Right;
        public double MinorEnd(in Rect rect) => ScrollOrientation == ScrollOrientation.Vertical ? rect.Right : rect.Bottom;

        public void SetMajorSize(ref Rect rect, double value)
        {
            if (ScrollOrientation == ScrollOrientation.Vertical)
            {
                rect = rect.WithHeight(value);
            }
            else
            {
                rect = rect.WithWidth(value);
            }
        }

        public void SetMinorSize(ref Rect rect, double value)
        {
            if (ScrollOrientation == ScrollOrientation.Vertical)
            {
                rect = rect.WithWidth(value);
            }
            else
            {
                rect = rect.WithHeight(value);
            }
        }

        public void SetMajorStart(ref Rect rect, double value)
        {
            if (ScrollOrientation == ScrollOrientation.Vertical)
            {
                rect = rect.WithY(value);
            }
            else
            {
                rect = rect.WithX(value);
            }
        }

        public void SetMinorStart(ref Rect rect, double value)
        {
            if (ScrollOrientation == ScrollOrientation.Vertical)
            {
                rect = rect.WithX(value);
            }
            else
            {
                rect = rect.WithY(value);
            }
        }

        public Rect MinorMajorRect(double minor, double major, double minorSize, double majorSize)
        {
            return ScrollOrientation == ScrollOrientation.Vertical ?
                new Rect(minor, major, minorSize, majorSize) :
                new Rect(major, minor, majorSize, minorSize);
        }

        public Point MinorMajorPoint(double minor, double major)
        {
            return ScrollOrientation == ScrollOrientation.Vertical ?
                new Point(minor, major) :
                new Point(major, minor);
        }

        public Size MinorMajorSize(double minor, double major)
        {
            return ScrollOrientation == ScrollOrientation.Vertical ?
                new Size(minor, major) :
                new Size(major, minor);
        }
    }
}
