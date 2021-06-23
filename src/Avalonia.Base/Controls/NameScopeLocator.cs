using System;
using System.Reactive.Disposables;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NameScopeLocator
    {
        /// <summary>
        /// Tracks a named control relative to another control.
        /// </summary>
        /// <param name="scope">The scope relative from which the object should be resolved.</param>
        /// <param name="name">The name of the object to find.</param>
        public static IObservable<object> Track(INameScope scope, string name)
        {
            return new NeverEndingSynchronousCompletionAsyncResultObservable<object>(scope.FindAsync(name));
        }
        
        // This class is implemented in such weird way because for some reason
        // our binding system doesn't expect OnCompleted to be ever called and
        // seems to treat it as binding cancellation or something 

        private class NeverEndingSynchronousCompletionAsyncResultObservable<T> : IObservable<T>
        {
            private T _value;
            private SynchronousCompletionAsyncResult<T>? _asyncResult;

            public NeverEndingSynchronousCompletionAsyncResultObservable(SynchronousCompletionAsyncResult<T> task)
            {
                if (task.IsCompleted)
                    _value = task.GetResult();
                else
                    _asyncResult = task;
            }
            
            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (_asyncResult?.IsCompleted == true)
                {
                    _value = _asyncResult.Value.GetResult();
                    _asyncResult = null;
                }

                if (_asyncResult != null)
                    _asyncResult.Value.OnCompleted(() =>
                    {
                        observer.OnNext(_asyncResult.Value.GetResult());
                    });
                else
                    observer.OnNext(_value);
                
                return Disposable.Empty;
            }
        }
    }
}
