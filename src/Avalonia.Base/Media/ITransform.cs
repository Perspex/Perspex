using System.ComponentModel;

namespace Avalonia.Media
{
    [TypeConverter(typeof(TransformConverter))]
    public interface ITransform
    {
        Matrix Value { get; }
    }
}
