using System;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph
{
    internal static class LineBoundsHelper
    {
        private static double CalculateAngle(Point p1, Point p2)
        {
            var xDiff = p2.X - p1.X;
            var yDiff = p2.Y - p1.Y;

            return Math.Atan2(yDiff, xDiff);
        }

        internal static double CalculateOppSide(double angle, double hyp)
        {
            return Math.Sin(angle) * hyp;
        }

        internal static double CalculateAdjSide(double angle, double hyp)
        {
            return Math.Cos(angle) * hyp;
        }

        private static (Point p1, Point p2) TranslatePointsAlongTangent(Point p1, Point p2, double angle, double distance)
        {
            var xDiff = CalculateOppSide(angle, distance);
            var yDiff = CalculateAdjSide(angle, distance);

            var c1 = new Point(p1.X + xDiff, p1.Y - yDiff);
            var c2 = new Point(p1.X - xDiff, p1.Y + yDiff);

            var c3 = new Point(p2.X + xDiff, p2.Y - yDiff);
            var c4 = new Point(p2.X - xDiff, p2.Y + yDiff);

            var minX = Math.Min(c1.X, Math.Min(c2.X, Math.Min(c3.X, c4.X)));
            var minY = Math.Min(c1.Y, Math.Min(c2.Y, Math.Min(c3.Y, c4.Y)));
            var maxX = Math.Max(c1.X, Math.Max(c2.X, Math.Max(c3.X, c4.X)));
            var maxY = Math.Max(c1.Y, Math.Max(c2.Y, Math.Max(c3.Y, c4.Y)));

            return (new Point(minX, minY), new Point(maxX, maxY));
        }

        private static Rect CalculateBounds(Point p1, Point p2, double thickness, double angleToCorner)
        {
            var pts = TranslatePointsAlongTangent(p1, p2, angleToCorner, thickness / 2);

            return new Rect(pts.p1, pts.p2);
        }

        public static Rect CalculateBounds(Point p1, Point p2, IPen p)
        {
            var radians = CalculateAngle(p1, p2);

            if (p.LineCap != PenLineCap.Flat)
            {
                var pts = TranslatePointsAlongTangent(p1, p2, radians - Math.PI / 2, p.Thickness / 2);

                return CalculateBounds(pts.p1, pts.p2, p.Thickness, radians);
            }
            else
            {
                return CalculateBounds(p1, p2, p.Thickness, radians);
            }
        }
    }
}
