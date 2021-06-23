using Avalonia.Collections;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a text decoration, which is a visual ornamentation that is added to text (such as an underline).
    /// </summary>
    public class TextDecoration : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Location"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationLocation> LocationProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationLocation>(nameof(Location));

        /// <summary>
        /// Defines the <see cref="Stroke"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> StrokeProperty =
            AvaloniaProperty.Register<TextDecoration, IBrush>(nameof(Stroke));

        /// <summary>
        /// Defines the <see cref="StrokeThicknessUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> StrokeThicknessUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(StrokeThicknessUnit));

        /// <summary>
        /// Defines the <see cref="StrokeDashArray"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>> StrokeDashArrayProperty =
            AvaloniaProperty.Register<TextDecoration, AvaloniaList<double>>(nameof(StrokeDashArray));

        /// <summary>
        /// Defines the <see cref="StrokeDashOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeDashOffsetProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeDashOffset));

        /// <summary>
        /// Defines the <see cref="StrokeThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeThickness), 1);

        /// <summary>
        /// Defines the <see cref="StrokeLineCap"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
            AvaloniaProperty.Register<TextDecoration, PenLineCap>(nameof(StrokeLineCap));

        /// <summary>
        /// Defines the <see cref="StrokeOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeOffsetProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeOffset));

        /// <summary>
        /// Defines the <see cref="StrokeOffsetUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> StrokeOffsetUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(StrokeOffsetUnit));

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public TextDecorationLocation Location
        {
            get => GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the <see cref="TextDecoration"/> is painted.
        /// </summary>
        public IBrush Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// Gets the units in which the thickness of the <see cref="TextDecoration"/> is expressed.
        /// </summary>
        public TextDecorationUnit StrokeThicknessUnit
        {
            get => GetValue(StrokeThicknessUnitProperty);
            set => SetValue(StrokeThicknessUnitProperty, value);
        }

        /// <summary>
        /// Gets or sets a collection of <see cref="double"/> values that indicate the pattern of dashes and gaps
        /// that is used to draw the <see cref="TextDecoration"/>.
        /// </summary>
        public AvaloniaList<double> StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
        /// </summary>
        public double StrokeDashOffset
        {
            get { return GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the <see cref="TextDecoration"/>.
        /// </summary>
        public double StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
        /// </summary>
        public PenLineCap StrokeLineCap
        {
            get { return GetValue(StrokeLineCapProperty); }
            set { SetValue(StrokeLineCapProperty, value); }
        }

        /// <summary>
        /// The stroke's offset.
        /// </summary>
        /// <value>
        /// The pen offset.
        /// </value>
        public double StrokeOffset
        {
            get => GetValue(StrokeOffsetProperty);
            set => SetValue(StrokeOffsetProperty, value);
        }

        /// <summary>
        /// Gets the units in which the <see cref="StrokeOffset"/> value is expressed.
        /// </summary>
        public TextDecorationUnit StrokeOffsetUnit
        {
            get => GetValue(StrokeOffsetUnitProperty);
            set => SetValue(StrokeOffsetUnitProperty, value);
        }

        /// <summary>
        /// Draws the <see cref="TextDecoration"/> at given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="shapedTextCharacters">The shaped characters that are decorated.</param>
        internal void Draw(DrawingContext drawingContext, ShapedTextCharacters shapedTextCharacters)
        {
            var fontRenderingEmSize = shapedTextCharacters.Properties.FontRenderingEmSize;
            var fontMetrics = shapedTextCharacters.FontMetrics;
            var thickness = StrokeThickness;

            switch (StrokeThicknessUnit)
            {
                case TextDecorationUnit.FontRecommended:
                    switch (Location)
                    {
                        case TextDecorationLocation.Underline:
                            thickness = fontMetrics.UnderlineThickness;
                            break;
                        case TextDecorationLocation.Strikethrough:
                            thickness = fontMetrics.StrikethroughThickness;
                            break;
                    }

                    break;
                case TextDecorationUnit.FontRenderingEmSize:
                    thickness = fontRenderingEmSize * thickness;
                    break;
            }

            var origin = new Point();

            switch (Location)
            {
                case TextDecorationLocation.Baseline:
                    origin += shapedTextCharacters.GlyphRun.BaselineOrigin;
                    break;
                case TextDecorationLocation.Strikethrough:
                    origin += new Point(shapedTextCharacters.GlyphRun.BaselineOrigin.X,
                        shapedTextCharacters.GlyphRun.BaselineOrigin.Y + fontMetrics.StrikethroughPosition);
                    break;
                case TextDecorationLocation.Underline:
                    origin += new Point(shapedTextCharacters.GlyphRun.BaselineOrigin.X,
                        shapedTextCharacters.GlyphRun.BaselineOrigin.Y + fontMetrics.UnderlinePosition);
                    break;
            }

            switch (StrokeOffsetUnit)
            {
                case TextDecorationUnit.FontRenderingEmSize:
                    origin += new Point(0, StrokeOffset * fontRenderingEmSize);
                    break;
                case TextDecorationUnit.Pixel:
                    origin += new Point(0, StrokeOffset);
                    break;
            }

            var pen = new Pen(Stroke ?? shapedTextCharacters.Properties.ForegroundBrush, thickness,
                new DashStyle(StrokeDashArray, StrokeDashOffset), StrokeLineCap);

            drawingContext.DrawLine(pen, origin, origin + new Point(shapedTextCharacters.Size.Width, 0));
        }
    }
}
