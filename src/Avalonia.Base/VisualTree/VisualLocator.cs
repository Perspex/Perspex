using System;
using System.Linq;
using Avalonia.Reactive;

namespace Avalonia.VisualTree
{
    public class VisualLocator
    {
        public static IObservable<IVisual> Track(IVisual relativeTo, int ancestorLevel, Type ancestorType = null)
        {
            return new VisualTracker(relativeTo, ancestorLevel, ancestorType);
        }

        private class VisualTracker : LightweightObservableBase<IVisual>
        {
            private readonly IVisual _relativeTo;
            private readonly int _ancestorLevel;
            private readonly Type _ancestorType;

            public VisualTracker(IVisual relativeTo, int ancestorLevel, Type ancestorType)
            {
                _relativeTo = relativeTo;
                _ancestorLevel = ancestorLevel;
                _ancestorType = ancestorType;
            }

            protected override void Initialize()
            {
                _relativeTo.AttachedToVisualTree += AttachedDetached;
                _relativeTo.DetachedFromVisualTree += AttachedDetached;
            }

            protected override void Deinitialize()
            {
                _relativeTo.AttachedToVisualTree -= AttachedDetached;
                _relativeTo.DetachedFromVisualTree -= AttachedDetached;
            }

            protected override void Subscribed(IObserver<IVisual> observer, bool first)
            {
                observer.OnNext(GetResult());
            }

            private void AttachedDetached(object sender, VisualTreeAttachmentEventArgs e) => PublishNext(GetResult());

            private IVisual GetResult()
            {
                if (_relativeTo.IsAttachedToVisualTree)
                {
                    return _relativeTo.GetVisualAncestors()
                        .Where(x => _ancestorType?.IsAssignableFrom(x.GetType()) ?? true)
                        .ElementAtOrDefault(_ancestorLevel);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
