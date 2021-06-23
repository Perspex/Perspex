using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class ChildNameScope : INameScope
    {
        private readonly INameScope _parentScope;
        private readonly NameScope _inner = new NameScope();

        public ChildNameScope(INameScope parentScope)
        {
            _parentScope = parentScope;
        }
        
        public void Register(string name, object element) => _inner.Register(name, element);

        public SynchronousCompletionAsyncResult<object> FindAsync(string name)
        {
            var found = Find(name);
            if (found != null)
                return new SynchronousCompletionAsyncResult<object>(found);
            // Not found and both current and parent scope are in completed state
            if(IsCompleted)
                return new SynchronousCompletionAsyncResult<object>(null);
            return DoFindAsync(name);
        }

        public SynchronousCompletionAsyncResult<object> DoFindAsync(string name)
        {
            var src = new SynchronousCompletionAsyncResultSource<object>();

            void ParentSearch()
            {
                var parentSearch = _parentScope.FindAsync(name);
                if (parentSearch.IsCompleted)
                    src.SetResult(parentSearch.GetResult());
                else
                    parentSearch.OnCompleted(() => src.SetResult(parentSearch.GetResult()));
            }
            if (!_inner.IsCompleted)
            {
                // Guaranteed to be incomplete at this point
                var innerSearch = _inner.FindAsync(name);
                innerSearch.OnCompleted(() =>
                {
                    var value = innerSearch.GetResult();
                    if (value != null)
                        src.SetResult(value);
                    else ParentSearch();
                });
            }
            else
                ParentSearch();

            return src.AsyncResult;
        }

        public object Find(string name)
        {
            var found = _inner.Find(name);
            if (found != null)
                return found;
            if (_inner.IsCompleted)
                return _parentScope.Find(name);
            return null;
        }

        public void Complete() => _inner.Complete();

        public bool IsCompleted => _inner.IsCompleted && _parentScope.IsCompleted;
    }
}
