using System.Reflection;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ClrPropertyViewModel : PropertyViewModel
    {
        private readonly object _target;
        private string _type;
        private object? _value;

#nullable disable
        // Remove "nullable disable" after MemberNotNull will work on our CI.
        public ClrPropertyViewModel(object o, PropertyInfo property)
#nullable restore
        {
            _target = o;
            Property = property;

            if (property.DeclaringType == null || !property.DeclaringType.IsInterface)
            {
                Name = property.Name;
            }
            else
            {
                Name = property.DeclaringType.Name + '.' + property.Name;
            }

            Update();
        }

        public PropertyInfo Property { get; }
        public override object Key => Name;
        public override string Name { get; }
        public override string Group => "CLR Properties";

        public override string Type => _type;

        public override string Value 
        {
            get => ConvertToString(_value);
            set
            {
                try
                {
                    var convertedValue = ConvertFromString(value, Property.PropertyType);
                    Property.SetValue(_target, convertedValue);
                }
                catch { }
            }
        }

        // [MemberNotNull(nameof(_type))]
        public override void Update()
        {
            var val = Property.GetValue(_target);
            RaiseAndSetIfChanged(ref _value, val, nameof(Value));
            RaiseAndSetIfChanged(ref _type, _value?.GetType().Name ?? Property.PropertyType.Name, nameof(Type));
        }
    }
}
