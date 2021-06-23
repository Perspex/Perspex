using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An <see cref="IStyleActivator"/> which is active when a set of classes match those on a
    /// control.
    /// </summary>
    internal sealed class StyleClassActivator : StyleActivatorBase
    {
        private readonly IList<string> _match;
        private readonly IAvaloniaReadOnlyList<string> _classes;
        private NotifyCollectionChangedEventHandler? _classesChangedHandler;

        public StyleClassActivator(IAvaloniaReadOnlyList<string> classes, IList<string> match)
        {
            _classes = classes;
            _match = match;
        }

        private NotifyCollectionChangedEventHandler ClassesChangedHandler =>
            _classesChangedHandler ??= ClassesChanged;

        public static bool AreClassesMatching(IReadOnlyList<string> classes, IList<string> toMatch)
        {
            int remainingMatches = toMatch.Count;
            int classesCount = classes.Count;

            // Early bail out - we can't match if control does not have enough classes.
            if (classesCount < remainingMatches)
            {
                return false;
            }

            for (var i = 0; i < classesCount; i++)
            {
                var c = classes[i];

                if (toMatch.Contains(c))
                {
                    --remainingMatches;

                    // Already matched so we can skip checking other classes.
                    if (remainingMatches == 0)
                    {
                        break;
                    }
                }
            }

            return remainingMatches == 0;
        }

        protected override void Initialize()
        {
            PublishNext(IsMatching());
            _classes.CollectionChanged += ClassesChangedHandler;
        }

        protected override void Deinitialize()
        {
            _classes.CollectionChanged -= ClassesChangedHandler;
        }

        private void ClassesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Move)
            {
                PublishNext(IsMatching());
            }
        }

        private bool IsMatching() => AreClassesMatching(_classes, _match);
    }
}
