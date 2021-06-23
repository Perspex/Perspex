using System;
using System.Linq;
using Avalonia.Reactive;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Locates controls relative to other controls.
    /// </summary>
    public static class ControlLocator
    {
        public static IObservable<ILogical> Track(ILogical relativeTo, int ancestorLevel, Type ancestorType = null)
        {
            return new ControlTracker(relativeTo, ancestorLevel, ancestorType);
        }

        private class ControlTracker : LightweightObservableBase<ILogical>
        {
            private readonly ILogical _relativeTo;
            private readonly int _ancestorLevel;
            private readonly Type _ancestorType;
            private ILogical _value;

            public ControlTracker(ILogical relativeTo, int ancestorLevel, Type ancestorType)
            {
                _relativeTo = relativeTo;
                _ancestorLevel = ancestorLevel;
                _ancestorType = ancestorType;
            }

            protected override void Initialize()
            {
                Update();
                _relativeTo.AttachedToLogicalTree += Attached;
                _relativeTo.DetachedFromLogicalTree += Detached;
            }

            protected override void Deinitialize()
            {
                _relativeTo.AttachedToLogicalTree -= Attached;
                _relativeTo.DetachedFromLogicalTree -= Detached;

                _value = null;
            }

            protected override void Subscribed(IObserver<ILogical> observer, bool first)
            {
                observer.OnNext(_value);
            }

            private void Attached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                Update();
                PublishNext(_value);
            }

            private void Detached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                _value = null;
                PublishNext(null);
            }

            private void Update()
            {
                _value = _relativeTo.GetLogicalAncestors()
                    .Where(x => _ancestorType?.IsAssignableFrom(x.GetType()) ?? true)
                    .ElementAtOrDefault(_ancestorLevel);
            }
        }
    }
}
