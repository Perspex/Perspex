using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Utilities;

namespace Avalonia.Data.Converters
{
    class MethodToCommandConverter : ICommand
    {
        readonly static Func<object, bool> AlwaysEnabled = (_) => true;
        readonly static MethodInfo tryConvert = typeof(TypeUtilities)
            .GetMethod(nameof(TypeUtilities.TryConvert), BindingFlags.Public | BindingFlags.Static);
        readonly static PropertyInfo currentCulture = typeof(CultureInfo)
            .GetProperty(nameof(CultureInfo.CurrentCulture), BindingFlags.Public | BindingFlags.Static);
        readonly Func<object, bool> canExecute;
        readonly Action<object> execute;
        readonly WeakPropertyChangedProxy weakPropertyChanged;
        readonly PropertyChangedEventHandler propertyChangedEventHandler;
        readonly string[] dependencyProperties;

        public MethodToCommandConverter(Delegate action)
        {
            var target = action.Target;
            var canExecuteMethodName = "Can" + action.Method.Name;
            var parameters = action.Method.GetParameters();
            var parameterInfo = parameters.Length == 0 ? null : parameters[0].ParameterType;

            if (parameterInfo == null)
            {
                execute = CreateExecute(target, action.Method);
            }
            else
            {
                execute = CreateExecute(target, action.Method, parameterInfo);
            }

            var canExecuteMethod = action.Method.DeclaringType.GetRuntimeMethods()
                .FirstOrDefault(m => m.Name == canExecuteMethodName
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(object));
            if (canExecuteMethod == null)
            {
                canExecute = AlwaysEnabled;
            }
            else
            {
                canExecute = CreateCanExecute(target, canExecuteMethod);
                dependencyProperties = canExecuteMethod
                    .GetCustomAttributes(typeof(Metadata.DependsOnAttribute), true)
                    .OfType<Metadata.DependsOnAttribute>()
                    .Select(x => x.Name)
                    .ToArray();
                if (dependencyProperties.Any()
                    && target is INotifyPropertyChanged inpc)
                {
                    propertyChangedEventHandler = OnPropertyChanged;
                    weakPropertyChanged = new WeakPropertyChangedProxy(inpc, propertyChangedEventHandler);
                }
            }
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)
                               || dependencyProperties?.Contains(args.PropertyName) == true)
            {
                Threading.Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)
                    , Threading.DispatcherPriority.Input);
            }
        }

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

        public bool CanExecute(object parameter) => canExecute(parameter);

        public void Execute(object parameter) => execute(parameter);


        static Action<object> CreateExecute(object target
            , System.Reflection.MethodInfo method)
        {

            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = ConvertTarget(target, method);

            var call = Expression.Call
            (
                instance,
                method
            );


            return Expression
                .Lambda<Action<object>>(call, parameter)
                .Compile();
        }

        static Action<object> CreateExecute(object target
            , System.Reflection.MethodInfo method
            , Type parameterType)
        {

            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = ConvertTarget(target, method);

            Expression body;

            if (parameterType == typeof(object))
            {
                body = Expression.Call(instance,
                    method,
                    parameter
                    );
            }
            else
            {
                var arg0 = Expression.Variable(typeof(object), "argX");
                var convertCall = Expression.Call(tryConvert,
                     Expression.Constant(parameterType),
                     parameter,
                      Expression.Property(null, currentCulture),
                     arg0
                    );

                var call = Expression.Call(instance,
                    method,
                    Expression.Convert(arg0, parameterType)
                    );
                body = Expression.Block(new[] { arg0 },
                    convertCall,
                    call
                    );

            }
            Action<object> action = null;
            try
            {
                action = Expression
                   .Lambda<Action<object>>(body, parameter)
                   .Compile();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return action;
        }

        static Func<object, bool> CreateCanExecute(object target
            , System.Reflection.MethodInfo method)
        {
            var parameter = Expression.Parameter(typeof(object), "parameter");
            var instance = ConvertTarget(target, method);
            var call = Expression.Call
            (
                instance,
                method,
                parameter
            );
            return Expression
                .Lambda<Func<object, bool>>(call, parameter)
                .Compile();
        }

        private static Expression ConvertTarget(object target, MethodInfo method) =>
            target is null ? null : Expression.Convert(Expression.Constant(target), method.DeclaringType);

        internal class WeakPropertyChangedProxy
        {
            readonly WeakReference<PropertyChangedEventHandler> _listener = new WeakReference<PropertyChangedEventHandler>(null);
            readonly PropertyChangedEventHandler _handler;
            internal WeakReference<INotifyPropertyChanged> Source { get; } = new WeakReference<INotifyPropertyChanged>(null);

            public WeakPropertyChangedProxy()
            {
                _handler = new PropertyChangedEventHandler(OnPropertyChanged);
            }

            public WeakPropertyChangedProxy(INotifyPropertyChanged source, PropertyChangedEventHandler listener) : this()
            {
                SubscribeTo(source, listener);
            }

            public void SubscribeTo(INotifyPropertyChanged source, PropertyChangedEventHandler listener)
            {
                source.PropertyChanged += _handler;

                Source.SetTarget(source);
                _listener.SetTarget(listener);
            }

            public void Unsubscribe()
            {
                if (Source.TryGetTarget(out INotifyPropertyChanged source) && source != null)
                    source.PropertyChanged -= _handler;

                Source.SetTarget(null);
                _listener.SetTarget(null);
            }

            void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (_listener.TryGetTarget(out var handler) && handler != null)
                    handler(sender, e);
                else
                    Unsubscribe();
            }

        }
    }
}
