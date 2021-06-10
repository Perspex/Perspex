using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class FilterViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();
        private string _filterString = string.Empty;
        private bool _useRegexFilter, _useCaseSensitiveFilter, _useWholeWordFilter;
        private Regex? _filterRegex;

        public event EventHandler? RefreshFilter;

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool Filter(string input)
        {
            return _filterRegex?.IsMatch(input) ?? true;
        }

        private void UpdateFilterRegex()
        {
            void ClearError()
            {
                if (_errors.Remove(nameof(FilterString)))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
                }
            }

            try
            {
                var options = RegexOptions.Compiled;
                var pattern = UseRegexFilter
                    ? FilterString.Trim() : Regex.Escape(FilterString.Trim());
                if (!UseCaseSensitiveFilter)
                {
                    options |= RegexOptions.IgnoreCase;
                }
                if (UseWholeWordFilter)
                {
                    pattern = $"\\b(?:{pattern})\\b";
                }

                _filterRegex = new Regex(pattern, options);
                ClearError();
            }
            catch (Exception exception)
            {
                _errors[nameof(FilterString)] = exception.Message;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
            }
        }

        public string FilterString
        {
            get => _filterString;
            set
            {
                if (RaiseAndSetIfChanged(ref _filterString, value))
                {
                    UpdateFilterRegex();
                    RefreshFilter?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool UseRegexFilter
        {
            get => _useRegexFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _useRegexFilter, value))
                {
                    UpdateFilterRegex();
                    RefreshFilter?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool UseCaseSensitiveFilter
        {
            get => _useCaseSensitiveFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _useCaseSensitiveFilter, value))
                {
                    UpdateFilterRegex();
                    RefreshFilter?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool UseWholeWordFilter
        {
            get => _useWholeWordFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _useWholeWordFilter, value))
                {
                    UpdateFilterRegex();
                    RefreshFilter?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName != null
                && _errors.TryGetValue(propertyName, out var error))
            {
                yield return error;
            }
        }
    }
}
