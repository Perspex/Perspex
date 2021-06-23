using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    internal class DescendantSelector : Selector
    {
        private readonly Selector _parent;
        private string? _selectorString;

        public DescendantSelector(Selector? parent)
        {
            _parent = parent ?? throw new InvalidOperationException("Descendant selector must be preceeded by a selector.");
        }

        /// <inheritdoc/>
        public override bool IsCombinator => true;

        /// <inheritdoc/>
        public override bool InTemplate => _parent.InTemplate;

        /// <inheritdoc/>
        public override Type? TargetType => null;

        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString() + ' ';
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            var c = (ILogical)control;
            var descendantMatches = new OrActivatorBuilder();

            while (c != null)
            {
                c = c.LogicalParent;

                if (c is IStyleable)
                {
                    var match = _parent.Match((IStyleable)c, subscribe);

                    if (match.Result == SelectorMatchResult.Sometimes)
                    {
                        descendantMatches.Add(match.Activator);
                    }
                    else if (match.IsMatch)
                    {
                        return SelectorMatch.AlwaysThisInstance;
                    }
                }
            }

            if (descendantMatches.Count > 0)
            {
                return new SelectorMatch(descendantMatches.Get());
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        protected override Selector? MovePrevious() => null;
    }
}
