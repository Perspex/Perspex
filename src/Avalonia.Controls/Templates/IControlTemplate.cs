using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    public interface IControlTemplate : ITemplate<ITemplatedControl, TemplateResult<IControl>>
    {
    }

    public class TemplateResult<T>
    {
        public T Result { get; }
        public INameScope NameScope { get; }

        public TemplateResult(T result, INameScope nameScope)
        {
            Result = result;
            NameScope = nameScope;
        }

        public void Deconstruct(out T result, out INameScope scope)
        {
            result = Result;
            scope = NameScope;
        }
    }
}
