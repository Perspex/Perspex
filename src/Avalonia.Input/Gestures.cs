using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public static class Gestures
    {
        public static readonly RoutedEvent<TappedEventArgs> TappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "Tapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "DoubleTapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<TappedEventArgs> RightTappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "RightTapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEvent =
            RoutedEvent.Register<ScrollGestureEventArgs>(
                "ScrollGesture", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEndedEvent =
            RoutedEvent.Register<ScrollGestureEventArgs>(
                "ScrollGestureEnded", RoutingStrategies.Bubble, typeof(Gestures));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        private static WeakReference<IInteractive> s_lastPress = new WeakReference<IInteractive>(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        static Gestures()
        {
            InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
            InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
        }

        public static void AddTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TappedEvent, handler);
        }

        public static void AddDoubleTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(DoubleTappedEvent, handler);
        }

        public static void AddRightTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(RightTappedEvent, handler);
        }

        public static void RemoveTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(TappedEvent, handler);
        }

        public static void RemoveDoubleTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(DoubleTappedEvent, handler);
        }

        public static void RemoveRightTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(RightTappedEvent, handler);
        }

        private static void PointerPressed(RoutedEventArgs ev)
        {
            if (ev.Source is null)
            {
                return;
            }

            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerPressedEventArgs)ev;
                var visual = (IVisual)ev.Source;

#pragma warning disable CS0618 // Type or member is obsolete
                if (e.ClickCount <= 1)
                {
                    s_lastPress = new WeakReference<IInteractive>(ev.Source);
                }
                else if (s_lastPress != null && e.ClickCount == 2 && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                {
                    if (s_lastPress.TryGetTarget(out var target) && target == e.Source)
                    {
                        e.Source.RaiseEvent(new TappedEventArgs(DoubleTappedEvent, e));
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        private static void PointerReleased(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerReleasedEventArgs)ev;

                if (s_lastPress.TryGetTarget(out var target) && target == e.Source)
                {
                    if (e.InitialPressMouseButton == MouseButton.Left || e.InitialPressMouseButton == MouseButton.Right)
                    {
                        if (e.InitialPressMouseButton == MouseButton.Right)
                        {
                            e.Source.RaiseEvent(new TappedEventArgs(RightTappedEvent, e));
                        }
                        else
                        {
                            e.Source.RaiseEvent(new TappedEventArgs(TappedEvent, e));
                        }
                    }
                }
            }
        }
    }
}
