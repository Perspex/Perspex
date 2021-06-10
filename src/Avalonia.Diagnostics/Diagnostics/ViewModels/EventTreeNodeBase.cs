using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class EventTreeNodeBase : ViewModelBase
    {
        internal bool _updateChildren = true;
        internal bool _updateParent = true;
        private bool _isExpanded;
        private bool? _isEnabled = false;
        private bool _isVisible;

        protected EventTreeNodeBase(EventTreeNodeBase? parent, string text)
        {
            Parent = parent;
            Text = text;
            IsVisible = true;
        }

        public IAvaloniaReadOnlyList<EventTreeNodeBase>? Children
        {
            get;
            protected set;
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public virtual bool? IsEnabled
        {
            get => _isEnabled;
            set => RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public EventTreeNodeBase? Parent
        {
            get;
        }

        public string Text
        {
            get;
        }

        internal void UpdateChecked()
        {
            IsEnabled = GetValue();

            bool? GetValue()
            {
                if (Children == null)
                    return false;

                bool? value = false;

                for (int i = 0; i < Children.Count; i++)
                {
                    if (i == 0)
                    {
                        value = Children[i].IsEnabled;
                        continue;
                    }

                    if (value != Children[i].IsEnabled)
                    {
                        value = null;
                        break;
                    }
                }

                return value;
            }
        }
    }
}
