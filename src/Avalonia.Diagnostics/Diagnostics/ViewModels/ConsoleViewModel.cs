using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Diagnostics.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ConsoleViewModel : ViewModelBase
    {
        private readonly ConsoleContext _context;
        private readonly Action<ConsoleContext> _updateContext;
        private int _historyIndex = -1;
        private string _input;
        private bool _isVisible;
        private ScriptState<object>? _state;

        public ConsoleViewModel(Action<ConsoleContext> updateContext)
        {
            _context = new ConsoleContext(this);
            _input = string.Empty;
            _updateContext = updateContext;
        }

        public string Input
        {
            get => _input;
            set => RaiseAndSetIfChanged(ref _input, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public AvaloniaList<ConsoleHistoryItem> History { get; } = new AvaloniaList<ConsoleHistoryItem>();

        public async Task Execute()
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                return;
            }

            try
            {
                var options = ScriptOptions.Default
                    .AddReferences(Assembly.GetAssembly(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo)));

                _updateContext(_context);

                if (_state == null)
                {
                    _state = await CSharpScript.RunAsync(Input, options: options, globals: _context);
                }
                else
                {
                    _state = await _state.ContinueWithAsync(Input);
                }

                if (_state.ReturnValue != ConsoleContext.NoOutput)
                {
                    History.Add(new ConsoleHistoryItem(Input, _state.ReturnValue ?? "(null)"));
                }
            }
            catch (Exception ex)
            {
                History.Add(new ConsoleHistoryItem(Input, ex));
            }

            Input = string.Empty;
            _historyIndex = -1;
        }

        public void HistoryUp()
        {
            if (History.Count > 0)
            {
                if (_historyIndex == -1)
                {
                    _historyIndex = History.Count - 1;
                }
                else if (_historyIndex > 0)
                {
                    --_historyIndex;
                }

                Input = History[_historyIndex].Input;
            }
        }

        public void HistoryDown()
        {
            if (History.Count > 0 && _historyIndex >= 0)
            {
                if (_historyIndex == History.Count - 1)
                {
                    _historyIndex = -1;
                    Input = string.Empty;
                }
                else
                {
                    Input = History[++_historyIndex].Input;
                }
            }
        }

        public void ToggleVisibility() => IsVisible = !IsVisible;
    }
}
