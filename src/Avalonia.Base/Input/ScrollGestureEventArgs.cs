using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class ScrollGestureEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public Vector Delta { get; }
        private static int _nextId = 1;

        public static int GetNextFreeId() => _nextId++;
        
        public ScrollGestureEventArgs(int id, Vector delta) : base(Gestures.ScrollGestureEvent)
        {
            Id = id;
            Delta = delta;
        }
    }

    public class ScrollGestureEndedEventArgs : RoutedEventArgs
    {
        public int Id { get; }

        public ScrollGestureEndedEventArgs(int id) : base(Gestures.ScrollGestureEndedEvent)
        {
            Id = id;
        }
    }
}
