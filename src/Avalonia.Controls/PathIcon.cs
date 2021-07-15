using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class PathIcon : IconElement
    {
        static PathIcon()
        {
            AffectsRender<PathIcon>(DataProperty);
        }

        /// <inheritdoc cref="Path.DataProperty" />
        public static readonly StyledProperty<Geometry> DataProperty =
            Path.DataProperty.AddOwner<PathIcon>();

        /// <inheritdoc cref="Path.Data" />
        public Geometry Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }

    public class PathIconSource : IconSource
    {
        /// <inheritdoc cref="Path.DataProperty" />
        public static readonly StyledProperty<Geometry> DataProperty =
            Path.DataProperty.AddOwner<PathIcon>();

        /// <inheritdoc cref="Path.Data" />
        public Geometry Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public override IDataTemplate IconElementTemplate { get; } = new FuncDataTemplate<PathIconSource>((source, _) => new PathIcon
        {
            [!ForegroundProperty] = source[!ForegroundProperty],
            [!DataProperty] = source[!DataProperty]
        });
    }
}
