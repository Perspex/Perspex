using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an icon that uses a image source as its content.
    /// </summary>
    public class ImageIcon : IconElement
    {
        /// <inheritdoc cref="Image.SourceProperty" />
        public static readonly StyledProperty<IImage> SourceProperty =
            Image.SourceProperty.AddOwner<ImageIcon>();

        /// <inheritdoc cref="Image.Source" />
        public IImage Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
    }

    /// <summary>
    /// Represents an icon source that uses a image source as its content.
    /// </summary>
    public class ImageIconSource : IconSource
    {
        /// <inheritdoc cref="Image.SourceProperty" />
        public static readonly StyledProperty<IImage> SourceProperty =
            Image.SourceProperty.AddOwner<ImageIcon>();

        /// <inheritdoc cref="Image.Source" />
        public IImage Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public override IDataTemplate IconElementTemplate { get; } = new FuncDataTemplate<ImageIconSource>((source, _) => new ImageIcon
        {
            [!ForegroundProperty] = source[!ForegroundProperty],
            [!SourceProperty] = source[!SourceProperty]
        });
    }
}
