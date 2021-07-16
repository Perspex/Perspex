using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Avalonia.Controls.Primitives.PopupPositioning
{
    public interface IManagedPopupPositionerPopup
    {
        IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens { get; }
        Rect ParentClientAreaScreenGeometry { get; }
        double Scaling { get; }
        void MoveAndResize(Point devicePoint, Size virtualSize);
    }

    public class ManagedPopupPositionerScreenInfo
    {
        public Rect Bounds { get; }
        public Rect WorkingArea { get; }

        public ManagedPopupPositionerScreenInfo(Rect bounds, Rect workingArea)
        {
            Bounds = bounds;
            WorkingArea = workingArea;
        }
    }

    /// <summary>
    /// An <see cref="IPopupPositioner"/> implementation for platforms on which a popup can be
    /// arbitrarily positioned.
    /// </summary>
    public class ManagedPopupPositioner : IPopupPositioner
    {
        private readonly IManagedPopupPositionerPopup _popup;

        public ManagedPopupPositioner(IManagedPopupPositionerPopup popup)
        {
            _popup = popup;
        }


        private static Point GetAnchorPoint(Rect anchorRect, PopupAnchor edge)
        {
            double x, y;
            if (edge.HasAllFlags(PopupAnchor.Left))
                x = anchorRect.X;
            else if (edge.HasAllFlags(PopupAnchor.Right))
                x = anchorRect.Right;
            else
                x = anchorRect.X + anchorRect.Width / 2;
            
            if (edge.HasAllFlags(PopupAnchor.Top))
                y = anchorRect.Y;
            else if (edge.HasAllFlags(PopupAnchor.Bottom))
                y = anchorRect.Bottom;
            else
                y = anchorRect.Y + anchorRect.Height / 2;
            return new Point(x, y);
        }

        private static Point Gravitate(Point anchorPoint, Size size, PopupGravity gravity)
        {
            double x, y;
            if (gravity.HasAllFlags(PopupGravity.Left))
                x = -size.Width;
            else if (gravity.HasAllFlags(PopupGravity.Right))
                x = 0;
            else
                x = -size.Width / 2;
            
            if (gravity.HasAllFlags(PopupGravity.Top))
                y = -size.Height;
            else if (gravity.HasAllFlags(PopupGravity.Bottom))
                y = 0;
            else
                y = -size.Height / 2;
            return anchorPoint + new Point(x, y);
        }

        public void Update(PopupPositionerParameters parameters)
        {
            var rect = Calculate(
                parameters.Size * _popup.Scaling,
                new Rect(
                    parameters.AnchorRectangle.TopLeft * _popup.Scaling,
                    parameters.AnchorRectangle.Size * _popup.Scaling),
                parameters.Anchor,
                parameters.Gravity,
                parameters.ConstraintAdjustment,
                parameters.Offset * _popup.Scaling);
           
            _popup.MoveAndResize(
                rect.Position,
                rect.Size / _popup.Scaling);
        }

        
        private Rect Calculate(Size translatedSize, 
            Rect anchorRect, PopupAnchor anchor, PopupGravity gravity,
            PopupPositionerConstraintAdjustment constraintAdjustment, Point offset)
        {
            var parentGeometry = _popup.ParentClientAreaScreenGeometry;
            anchorRect = anchorRect.Translate(parentGeometry.TopLeft);
            
            Rect GetBounds()
            {
                var screens = _popup.Screens;
                
                var targetScreen = screens.FirstOrDefault(s => s.Bounds.Contains(anchorRect.TopLeft))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Intersects(anchorRect))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Contains(parentGeometry.TopLeft))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Intersects(parentGeometry))
                                   ?? screens.FirstOrDefault();

                if (targetScreen != null && targetScreen.WorkingArea.IsEmpty)
                {
                    return targetScreen.Bounds;
                }
                
                return targetScreen?.WorkingArea
                       ?? new Rect(0, 0, double.MaxValue, double.MaxValue);
            }

            var bounds = GetBounds();

            bool FitsInBounds(Rect rc, PopupAnchor edge = PopupAnchor.AllMask)
            {
                if (edge.HasAllFlags(PopupAnchor.Left) && rc.X < bounds.X ||
                    edge.HasAllFlags(PopupAnchor.Top) && rc.Y < bounds.Y ||
                    edge.HasAllFlags(PopupAnchor.Right) && rc.Right > bounds.Right ||
                    edge.HasAllFlags(PopupAnchor.Bottom) && rc.Bottom > bounds.Bottom)
                {
                    return false;
                }

                return true;
            }

            static bool IsValid(in Rect rc) => rc.Width > 0 && rc.Height > 0;

            Rect GetUnconstrained(PopupAnchor a, PopupGravity g) =>
                new Rect(Gravitate(GetAnchorPoint(anchorRect, a), translatedSize, g) + offset, translatedSize);


            var geo = GetUnconstrained(anchor, gravity);

            // If flipping geometry and anchor is allowed and helps, use the flipped one,
            // otherwise leave it as is
            if (!FitsInBounds(geo, PopupAnchor.HorizontalMask)
                && constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.FlipX))
            {
                var flipped = GetUnconstrained(anchor.FlipX(), gravity.FlipX());
                if (FitsInBounds(flipped, PopupAnchor.HorizontalMask))
                    geo = geo.WithX(flipped.X);
            }

            // If sliding is allowed, try moving the rect into the bounds
            if (constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.SlideX))
            {
                geo = geo.WithX(Math.Max(geo.X, bounds.X));
                if (geo.Right > bounds.Right)
                    geo = geo.WithX(bounds.Right - geo.Width);
            }
            
            // Resize the rect horizontally if allowed.
            if (constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.ResizeX))
            {
                var unconstrainedRect = geo;

                if (!FitsInBounds(unconstrainedRect, PopupAnchor.Left))
                {
                    unconstrainedRect = unconstrainedRect.WithX(bounds.X);
                }

                if (!FitsInBounds(unconstrainedRect, PopupAnchor.Right))
                {
                    unconstrainedRect = unconstrainedRect.WithWidth(bounds.Width - unconstrainedRect.X);
                }

                if (IsValid(unconstrainedRect))
                {
                    geo = unconstrainedRect;
                }
            }

            // If flipping geometry and anchor is allowed and helps, use the flipped one,
            // otherwise leave it as is
            if (!FitsInBounds(geo, PopupAnchor.VerticalMask)
                && constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.FlipY))
            {
                var flipped = GetUnconstrained(anchor.FlipY(), gravity.FlipY());
                if (FitsInBounds(flipped, PopupAnchor.VerticalMask))
                    geo = geo.WithY(flipped.Y);
            }

            // If sliding is allowed, try moving the rect into the bounds
            if (constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.SlideY))
            {
                geo = geo.WithY(Math.Max(geo.Y, bounds.Y));
                if (geo.Bottom > bounds.Bottom)
                    geo = geo.WithY(bounds.Bottom - geo.Height);
            }

            // Resize the rect vertically if allowed.
            if (constraintAdjustment.HasAllFlags(PopupPositionerConstraintAdjustment.ResizeY))
            {
                var unconstrainedRect = geo;

                if (!FitsInBounds(unconstrainedRect, PopupAnchor.Top))
                {
                    unconstrainedRect = unconstrainedRect.WithY(bounds.Y);
                }

                if (!FitsInBounds(unconstrainedRect, PopupAnchor.Bottom))
                {
                    unconstrainedRect = unconstrainedRect.WithHeight(bounds.Bottom - unconstrainedRect.Y);
                }

                if (IsValid(unconstrainedRect))
                {
                    geo = unconstrainedRect;
                }
            }

            return geo;
        }
    }
}
