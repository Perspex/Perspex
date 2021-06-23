using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling
{
    internal class ChildSelector : Selector
    {
        private readonly Selector _parent;
        private string _selectorString;

        public ChildSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Child selector must be preceeded by a selector.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        public override bool InTemplate => _parent.InTemplate;

        /// <inheritdoc/>
        public override bool IsCombinator => true;

        /// <inheritdoc/>
        public override Type TargetType => null;

        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString() + " > ";
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            var controlParent = ((ILogical)control).LogicalParent;

            if (controlParent != null)
            {
                var parentMatch = _parent.Match((IStyleable)controlParent, subscribe);

                if (parentMatch.Result == SelectorMatchResult.Sometimes)
                {
                    return parentMatch;
                }
                else if (parentMatch.IsMatch)
                {
                    return SelectorMatch.AlwaysThisInstance;
                }
                else
                {
                    return SelectorMatch.NeverThisInstance;
                }
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        protected override Selector MovePrevious() => null;
    }
}
