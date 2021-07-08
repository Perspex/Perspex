using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Data
{
    internal class DataContextRoot<T> : SingleSubscriberObservableBase<T>
        where T : class
    {
        private readonly IStyledElement _source;

        public DataContextRoot(IStyledElement source)
        {
            _source = source;
        }

        protected override void Subscribed()
        {
            _source.PropertyChanged += PropertyChanged;
            PublishValue();
        }

        protected override void Unsubscribed()
        {
            _source.PropertyChanged -= PropertyChanged;
        }

        private void PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.DataContextProperty)
            {
                PublishValue();
            }
        }

        private void PublishValue()
        {
            if (_source.DataContext is null)
            {
                PublishNext(null);
            }
            else if (_source.DataContext is T value)
            {
                PublishNext(value);
            }
            else
            {
                // TODO: Log DataContext is unexpected type.
            }
        }
    }
}
