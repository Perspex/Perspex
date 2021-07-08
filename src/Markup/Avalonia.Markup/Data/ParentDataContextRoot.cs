using System;
using Avalonia.Reactive;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Data
{
    internal class ParentDataContextRoot<T> : SingleSubscriberObservableBase<T>
        where T : class
    {
        private readonly IVisual _source;

        public ParentDataContextRoot(IVisual source)
        {
            _source = source;
        }

        protected override void Subscribed()
        {
            ((IAvaloniaObject)_source).PropertyChanged += SourcePropertyChanged;
            StartListeningToDataContext(_source.VisualParent);
            PublishValue();
        }

        protected override void Unsubscribed()
        {
            ((IAvaloniaObject)_source).PropertyChanged -= SourcePropertyChanged;
        }

        private void PublishValue()
        {
            var parent = _source.VisualParent as IStyledElement;

            if (parent?.DataContext is null)
            {
                PublishNext(null);
            }
            else if (parent.DataContext is T value)
            {
                PublishNext(value);
            }
            else
            {
                // TODO: Log DataContext is unexpected type.
            }
        }

        private void StartListeningToDataContext(IVisual visual)
        {
            if (visual is IStyledElement styled)
            {
                styled.PropertyChanged += ParentPropertyChanged;
            }
        }

        private void StopListeningToDataContext(IVisual visual)
        {
            if (visual is IStyledElement styled)
            {
                styled.PropertyChanged -= ParentPropertyChanged;
            }
        }

        private void SourcePropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.VisualParentProperty)
            {
                StopListeningToDataContext(_source.VisualParent);
                StartListeningToDataContext(_source.VisualParent);
                PublishValue();
            }
        }

        private void ParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.DataContextProperty)
            {
                PublishValue();
            }
        }
    }
}
