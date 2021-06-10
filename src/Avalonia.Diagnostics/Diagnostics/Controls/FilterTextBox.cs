using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Controls
{
    internal class FilterTextBox : TextBox, IStyleable
    {
        public static readonly DirectProperty<FilterTextBox, bool> UseRegexFilterProperty =
            AvaloniaProperty.RegisterDirect<FilterTextBox, bool>(nameof(UseRegexFilter),
                o => o.UseRegexFilter, (o, v) => o.UseRegexFilter = v,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly DirectProperty<FilterTextBox, bool> UseCaseSensitiveFilterProperty =
            AvaloniaProperty.RegisterDirect<FilterTextBox, bool>(nameof(UseCaseSensitiveFilter),
                o => o.UseCaseSensitiveFilter, (o, v) => o.UseCaseSensitiveFilter = v,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly DirectProperty<FilterTextBox, bool> UseWholeWordFilterProperty =
            AvaloniaProperty.RegisterDirect<FilterTextBox, bool>(nameof(UseWholeWordFilter),
                o => o.UseWholeWordFilter, (o, v) => o.UseWholeWordFilter = v,
                defaultBindingMode: BindingMode.TwoWay);

        private bool _useRegexFilter, _useCaseSensitiveFilter, _useWholeWordFilter;

        public FilterTextBox()
        {
            Classes.Add("filter-text-box");
        }

        public bool UseRegexFilter
        {
            get => _useRegexFilter;
            set => SetAndRaise(UseRegexFilterProperty, ref _useRegexFilter, value);
        }

        public bool UseCaseSensitiveFilter
        {
            get => _useCaseSensitiveFilter;
            set => SetAndRaise(UseCaseSensitiveFilterProperty, ref _useCaseSensitiveFilter, value);
        }

        public bool UseWholeWordFilter
        {
            get => _useWholeWordFilter;
            set => SetAndRaise(UseWholeWordFilterProperty, ref _useWholeWordFilter, value);
        }

        Type IStyleable.StyleKey => typeof(TextBox);
    }
}
