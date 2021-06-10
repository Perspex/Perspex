using Avalonia.Collections;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Enum which describes how to position the TickBar.
    /// </summary>
    public enum TickBarPlacement
    {
        /// <summary>
        /// Position this tick at the left of target element.
        /// </summary>
        Left,

        /// <summary>
        /// Position this tick at the top of target element.
        /// </summary>
        Top,

        /// <summary>
        /// Position this tick at the right of target element.
        /// </summary>
        Right,

        /// <summary>
        /// Position this tick at the bottom of target element.
        /// </summary>
        Bottom,
    }


    /// <summary>
    /// An element that is used for drawing <see cref="Slider"/>'s Ticks.
    /// </summary>
    public class TickBar : Control
    {
        static TickBar()
        {
            AffectsRender<TickBar>(FillProperty,
                                   IsDirectionReversedProperty,
                                   ReservedSpaceProperty,
                                   MaximumProperty,
                                   MinimumProperty,
                                   OrientationProperty,
                                   PlacementProperty,
                                   TickFrequencyProperty,
                                   TicksProperty);
        }

        public TickBar() : base()
        {
        }

        /// <summary>
        /// Defines the <see cref="Fill"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> FillProperty =
            AvaloniaProperty.Register<TickBar, IBrush>(nameof(Fill));

        /// <summary>
        /// Brush used to fill the TickBar's Ticks.
        /// </summary>
        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<TickBar, double>(nameof(Minimum), 0d);

        /// <summary>
        /// Logical position where the Minimum Tick will be drawn
        /// </summary>
        public double Minimum
        {
            get { return GetValue(MinimumProperty); }
            set
            {
                SetValue(MinimumProperty, value);
            }
        }

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<TickBar, double>(nameof(Maximum), 0d);

        /// <summary>
        /// Logical position where the Maximum Tick will be drawn
        /// </summary>
        public double Maximum
        {
            get { return GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="TickFrequency"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TickFrequencyProperty =
            AvaloniaProperty.Register<TickBar, double>(nameof(TickFrequency), 0d);

        /// <summary>
        /// TickFrequency property defines how the tick will be drawn.
        /// </summary>
        public double TickFrequency
        {
            get { return GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<TickBar, Orientation>(nameof(Orientation));

        /// <summary>
        /// TickBar parent's orientation.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Ticks"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>> TicksProperty =
            AvaloniaProperty.Register<TickBar, AvaloniaList<double>>(nameof(Ticks));

        /// <summary>
        /// The Ticks property contains collection of value of type Double which
        /// are the logical positions use to draw the ticks.
        /// The property value is a <see cref="AvaloniaList{T}" />.
        /// </summary>
        public AvaloniaList<double> Ticks
        {
            get { return GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="IsDirectionReversed"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDirectionReversedProperty =
            AvaloniaProperty.Register<TickBar, bool>(nameof(IsDirectionReversed), false);

        /// <summary>
        /// The IsDirectionReversed property defines the direction of value incrementation.
        /// By default, if Tick's orientation is Horizontal, ticks will be drawn from left to right.
        /// (And, bottom to top for Vertical orientation).
        /// If IsDirectionReversed is 'true' the direction of the drawing will be in opposite direction.
        /// </summary>
        public bool IsDirectionReversed
        {
            get { return GetValue(IsDirectionReversedProperty); }
            set { SetValue(IsDirectionReversedProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Placement"/> property.
        /// </summary>
        public static readonly StyledProperty<TickBarPlacement> PlacementProperty =
            AvaloniaProperty.Register<TickBar, TickBarPlacement>(nameof(Placement), 0d);

        /// <summary>
        /// Placement property specified how the Tick will be placed.
        /// This property affects the way ticks are drawn.
        /// This property has type of <see cref="TickBarPlacement" />.
        /// </summary>
        public TickBarPlacement Placement
        {
            get { return GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="ReservedSpace"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> ReservedSpaceProperty =
            AvaloniaProperty.Register<TickBar, Rect>(nameof(ReservedSpace));

        /// <summary>
        /// TickBar will use ReservedSpaceProperty for left and right spacing (for horizontal orientation) or
        /// top and bottom spacing (for vertical orienation).
        /// The space on both sides of TickBar is half of specified ReservedSpace.
        /// This property has type of <see cref="Rect" />.
        /// </summary>
        public Rect ReservedSpace
        {
            get { return GetValue(ReservedSpaceProperty); }
            set { SetValue(ReservedSpaceProperty, value); }
        }

        /// <summary>
        /// Draw ticks.
        /// Ticks can be draw in 8 different ways depends on Placement property and IsDirectionReversed property.
        ///
        /// This function also draw selection-tick(s) if IsSelectionRangeEnabled is 'true' and
        /// SelectionStart and SelectionEnd are valid.
        ///
        /// The primary ticks (for Mininum and Maximum value) height will be 100% of TickBar's render size (use Width or Height
        /// depends on Placement property).
        ///
        /// The secondary ticks (all other ticks, including selection-tics) height will be 75% of TickBar's render size.
        ///
        /// Brush that use to fill ticks is specified by Fill property.
        /// </summary>
        public override void Render(DrawingContext dc)
        {
            var size = new Size(Bounds.Width, Bounds.Height);
            var range = Maximum - Minimum;
            var tickLen = 0.0d;   // Height for Primary Tick (for Mininum and Maximum value)
            var tickLen2 = 0.0d;  // Height for Secondary Tick
            var logicalToPhysical = 1.0;
            var startPoint = new Point();
            var endPoint = new Point();
            var rSpace = Orientation == Orientation.Horizontal ? ReservedSpace.Width : ReservedSpace.Height;

            // Take Thumb size in to account
            double halfReservedSpace = rSpace * 0.5;

            switch (Placement)
            {
                case TickBarPlacement.Top:
                    if (MathUtilities.GreaterThanOrClose(rSpace, size.Width))
                    {
                        return;
                    }
                    size = new Size(size.Width - rSpace, size.Height);
                    tickLen = -size.Height;
                    startPoint = new Point(halfReservedSpace, size.Height);
                    endPoint = new Point(halfReservedSpace + size.Width, size.Height);
                    logicalToPhysical = size.Width / range;
                    break;

                case TickBarPlacement.Bottom:
                    if (MathUtilities.GreaterThanOrClose(rSpace, size.Width))
                    {
                        return;
                    }
                    size = new Size(size.Width - rSpace, size.Height);
                    tickLen = size.Height;
                    startPoint = new Point(halfReservedSpace, 0d);
                    endPoint = new Point(halfReservedSpace + size.Width, 0d);
                    logicalToPhysical = size.Width / range;
                    break;

                case TickBarPlacement.Left:
                    if (MathUtilities.GreaterThanOrClose(rSpace, size.Height))
                    {
                        return;
                    }
                    size = new Size(size.Width, size.Height - rSpace);

                    tickLen = -size.Width;
                    startPoint = new Point(size.Width, size.Height + halfReservedSpace);
                    endPoint = new Point(size.Width, halfReservedSpace);
                    logicalToPhysical = size.Height / range * -1;
                    break;

                case TickBarPlacement.Right:
                    if (MathUtilities.GreaterThanOrClose(rSpace, size.Height))
                    {
                        return;
                    }
                    size = new Size(size.Width, size.Height - rSpace);
                    tickLen = size.Width;
                    startPoint = new Point(0d, size.Height + halfReservedSpace);
                    endPoint = new Point(0d, halfReservedSpace);
                    logicalToPhysical = size.Height / range * -1;
                    break;
            };

            tickLen2 = tickLen * 0.75;

            // Invert direciton of the ticks
            if (IsDirectionReversed)
            {
                logicalToPhysical *= -1;

                // swap startPoint & endPoint
                var pt = startPoint;
                startPoint = endPoint;
                endPoint = pt;
            }

            var pen = new ImmutablePen(Fill?.ToImmutable(), 1.0d);

            // Is it Vertical?
            if (Placement == TickBarPlacement.Left || Placement == TickBarPlacement.Right)
            {
                // Reduce tick interval if it is more than would be visible on the screen
                double interval = TickFrequency;
                if (interval > 0.0)
                {
                    double minInterval = (Maximum - Minimum) / size.Height;
                    if (interval < minInterval)
                    {
                        interval = minInterval;
                    }
                }

                // Draw Min & Max tick
                dc.DrawLine(pen, startPoint, new Point(startPoint.X + tickLen, startPoint.Y));
                dc.DrawLine(pen, new Point(startPoint.X, endPoint.Y),
                                 new Point(startPoint.X + tickLen, endPoint.Y));

                // This property is rarely set so let's try to avoid the GetValue
                // caching of the mutable default value
                var ticks = Ticks ?? null;

                // Draw ticks using specified Ticks collection
                if (ticks?.Count > 0)
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        if (MathUtilities.LessThanOrClose(ticks[i], Minimum) || MathUtilities.GreaterThanOrClose(ticks[i], Maximum))
                        {
                            continue;
                        }

                        double adjustedTick = ticks[i] - Minimum;

                        double y = adjustedTick * logicalToPhysical + startPoint.Y;
                        dc.DrawLine(pen,
                            new Point(startPoint.X, y),
                            new Point(startPoint.X + tickLen2, y));
                    }
                }
                // Draw ticks using specified TickFrequency
                else if (interval > 0.0)
                {
                    for (double i = interval; i < range; i += interval)
                    {
                        double y = i * logicalToPhysical + startPoint.Y;

                        dc.DrawLine(pen,
                            new Point(startPoint.X, y),
                            new Point(startPoint.X + tickLen2, y));
                    }
                }
            }
            else  // Placement == Top || Placement == Bottom
            {
                // Reduce tick interval if it is more than would be visible on the screen
                double interval = TickFrequency;
                if (interval > 0.0)
                {
                    double minInterval = (Maximum - Minimum) / size.Width;
                    if (interval < minInterval)
                    {
                        interval = minInterval;
                    }
                }

                // Draw Min & Max tick
                dc.DrawLine(pen, startPoint, new Point(startPoint.X, startPoint.Y + tickLen));
                dc.DrawLine(pen, new Point(endPoint.X, startPoint.Y),
                                 new Point(endPoint.X, startPoint.Y + tickLen));

                // This property is rarely set so let's try to avoid the GetValue
                // caching of the mutable default value
                var ticks = Ticks ?? null;

                // Draw ticks using specified Ticks collection
                if (ticks?.Count > 0)
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        if (MathUtilities.LessThanOrClose(ticks[i], Minimum) || MathUtilities.GreaterThanOrClose(ticks[i], Maximum))
                        {
                            continue;
                        }
                        double adjustedTick = ticks[i] - Minimum;

                        double x = adjustedTick * logicalToPhysical + startPoint.X;
                        dc.DrawLine(pen,
                            new Point(x, startPoint.Y),
                            new Point(x, startPoint.Y + tickLen2));
                    }
                }
                // Draw ticks using specified TickFrequency
                else if (interval > 0.0)
                {
                    for (double i = interval; i < range; i += interval)
                    {
                        double x = i * logicalToPhysical + startPoint.X;
                        dc.DrawLine(pen,
                            new Point(x, startPoint.Y),
                            new Point(x, startPoint.Y + tickLen2));
                    }
                }
            }
        }
    }
}
