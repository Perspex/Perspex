#nullable enable
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an icon that uses an IconSource as its content.
    /// </summary>
    /// <remarks>
    /// <see cref="Avalonia.Controls.IconSource"/> is similar to IconElement. However, because it is not a <see cref="Control"/>, it can be shared.
    /// <see cref="IconSourceElement"/> provides a wrapper that lets you use an IconSource in places that require an IconElement.
    /// </remarks>
    public class IconSourceElement : IconElement
    {
        /// <summary>
        /// Identifies the IconSource dependency property.
        /// </summary>
        public static readonly StyledProperty<IconSource?> IconSourceProperty =
            AvaloniaProperty.Register<IconSourceElement, IconSource?>(nameof(IconSource));

        /// <summary>
        /// Gets or sets the IconSource used as the icon content.
        /// </summary>
        [Content]
        public IconSource? IconSource
        {
            get => GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }
    }

    /// <summary>
    /// Represents the base class for an icon source.
    /// </summary>
    /// <remarks>
    /// <see cref="IconSource"/> is similar to IconElement. However, because it is not a <see cref="Control"/>, it can be shared.
    /// </remarks>
    public abstract class IconSource : AvaloniaObject
    {
        /// <inheritdoc cref="TemplatedControl.ForegroundProperty" />
        public static StyledProperty<IBrush?> ForegroundProperty =
            TemplatedControl.ForegroundProperty.AddOwner<IconSource>();

        /// <inheritdoc cref="TemplatedControl.Foreground" />
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Gets the data template used to display <see cref="IconElement"/>.
        /// </summary>
        public abstract IDataTemplate IconElementTemplate { get; }
    }
}
