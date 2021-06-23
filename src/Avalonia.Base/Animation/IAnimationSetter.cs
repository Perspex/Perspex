namespace Avalonia.Animation
{
    public interface IAnimationSetter
    {
        AvaloniaProperty Property { get; set; }
        object Value { get; set; }
    }
}
