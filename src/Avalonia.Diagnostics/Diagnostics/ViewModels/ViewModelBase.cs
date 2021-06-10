using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ViewModelBase : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _propertyChanged;
        private List<string> events = new List<string>();

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { _propertyChanged += value; events.Add("added"); }
            remove { _propertyChanged -= value; events.Add("removed"); }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        protected bool RaiseAndSetIfChanged<T>([NotNullIfNotNull("value")] ref T field, T value, [CallerMemberName] string propertyName = null!)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null!)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
        }
    }
}
