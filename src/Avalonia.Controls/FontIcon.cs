using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an icon that uses a glyph from the specified font.
    /// </summary>
    public class FontIcon : IconElement
    {
        /// <summary>
        /// Gets the identifier for the <see cref="Glyph"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> GlyphProperty =
            AvaloniaProperty.Register<FontIcon, string>(nameof(Glyph));

        /// <summary>
        /// Gets or sets the character code that identifies the icon glyph.
        /// </summary>
        public string Glyph
        {
            get => GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }
    }

    /// <summary>
    /// Represents an icon source that uses a glyph from the specified font.
    /// </summary>
    public class FontIconSource : IconSource
    {
        /// <inheritdoc cref="FontIcon.GlyphProperty" />
        public static readonly StyledProperty<string> GlyphProperty =
            FontIcon.GlyphProperty.AddOwner<FontIconSource>();

        /// <inheritdoc cref="TextBlock.FontFamilyProperty" />
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<FontIconSource>();

        /// <inheritdoc cref="TextBlock.FontSizeProperty" />
        public static readonly StyledProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<FontIconSource>();

        /// <inheritdoc cref="TextBlock.FontStyleProperty" />
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<FontIconSource>();

        /// <inheritdoc cref="TextBlock.FontWeightProperty" />
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<FontIconSource>();

        /// <inheritdoc cref="FontIcon.Glyph" />
        public string Glyph
        {
            get => GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <inheritdoc cref="TextBlock.FontFamily" />
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <inheritdoc cref="TextBlock.FontSize" />
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <inheritdoc cref="TextBlock.FontStyle" />
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <inheritdoc cref="TextBlock.FontWeight" />
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public override IDataTemplate IconElementTemplate { get; } = new FuncDataTemplate<FontIconSource>((source, _) => new FontIcon
        {
            [!ForegroundProperty] = source[!ForegroundProperty],
            [!GlyphProperty] = source[!GlyphProperty],
            [!FontFamilyProperty] = source[!FontFamilyProperty],
            [!FontSizeProperty] = source[!FontSizeProperty],
            [!FontStyleProperty] = source[!FontStyleProperty],
            [!FontWeightProperty] = source[!FontWeightProperty]
        });
    }
}
