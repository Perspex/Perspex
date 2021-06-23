using System.Windows.Input;

namespace Avalonia.Input
{
    public class KeyBinding : AvaloniaObject
    {
        public static readonly StyledProperty<ICommand> CommandProperty =
            AvaloniaProperty.Register<KeyBinding, ICommand>(nameof(Command));

        public ICommand Command
        {
            get { return GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly StyledProperty<object> CommandParameterProperty =
            AvaloniaProperty.Register<KeyBinding, object>(nameof(CommandParameter));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly StyledProperty<KeyGesture> GestureProperty =
            AvaloniaProperty.Register<KeyBinding, KeyGesture>(nameof(Gesture));

        public KeyGesture Gesture
        {
            get { return GetValue(GestureProperty); }
            set { SetValue(GestureProperty, value); }
        }

        public void TryHandle(KeyEventArgs args)
        {
            if (Gesture?.Matches(args) == true)
            {
                args.Handled = true;
                if (Command?.CanExecute(CommandParameter) == true)
                    Command.Execute(CommandParameter);
            }
        }
    }
}
