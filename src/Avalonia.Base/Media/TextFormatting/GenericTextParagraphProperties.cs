namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextParagraphProperties
    /// </summary>
    public sealed class GenericTextParagraphProperties : TextParagraphProperties
    {
        private FlowDirection _flowDirection;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrap;
        private double _lineHeight;

        /// <summary>
        /// Constructing TextParagraphProperties
        /// </summary>
        /// <param name="defaultTextRunProperties">default paragraph's default run properties</param>
        /// <param name="textAlignment">logical horizontal alignment</param>
        /// <param name="textWrap">text wrap option</param>
        /// <param name="lineHeight">Paragraph line height</param>
        public GenericTextParagraphProperties(TextRunProperties defaultTextRunProperties,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrap = TextWrapping.NoWrap,
            double lineHeight = 0)
        {
            DefaultTextRunProperties = defaultTextRunProperties;
            _textAlignment = textAlignment;
            _textWrap = textWrap;
            _lineHeight = lineHeight;
        }

        /// <summary>
        /// Constructing TextParagraphProperties
        /// </summary>
        /// <param name="flowDirection">text flow direction</param>
        /// <param name="textAlignment">logical horizontal alignment</param>
        /// <param name="firstLineInParagraph">true if the paragraph is the first line in the paragraph</param>
        /// <param name="alwaysCollapsible">true if the line is always collapsible</param>
        /// <param name="defaultTextRunProperties">default paragraph's default run properties</param>
        /// <param name="textWrap">text wrap option</param>
        /// <param name="lineHeight">Paragraph line height</param>
        /// <param name="indent">line indentation</param>
        public GenericTextParagraphProperties(
            FlowDirection flowDirection,
            TextAlignment textAlignment,
            bool firstLineInParagraph,
            bool alwaysCollapsible,
            TextRunProperties defaultTextRunProperties,
            TextWrapping textWrap,
            double lineHeight,
            double indent
            )
        {
            _flowDirection = flowDirection;
            _textAlignment = textAlignment;
            FirstLineInParagraph = firstLineInParagraph;
            AlwaysCollapsible = alwaysCollapsible;
            DefaultTextRunProperties = defaultTextRunProperties;
            _textWrap = textWrap;
            _lineHeight = lineHeight;
            Indent = indent;
        }

        /// <summary>
        /// Constructing TextParagraphProperties from another one
        /// </summary>
        /// <param name="textParagraphProperties">source line props</param>
        public GenericTextParagraphProperties(TextParagraphProperties textParagraphProperties)
            : this(textParagraphProperties.FlowDirection, 
                textParagraphProperties.TextAlignment,
                textParagraphProperties.FirstLineInParagraph, 
                textParagraphProperties.AlwaysCollapsible,
                textParagraphProperties.DefaultTextRunProperties, 
                textParagraphProperties.TextWrapping,
                textParagraphProperties.LineHeight,
                textParagraphProperties.Indent)
        {
        }

        /// <summary>
        /// This property specifies whether the primary text advance
        /// direction shall be left-to-right, right-to-left, or top-to-bottom.
        /// </summary>
        public override FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }

        /// <summary>
        /// This property describes how inline content of a block is aligned.
        /// </summary>
        public override TextAlignment TextAlignment
        {
            get { return _textAlignment; }
        }

        /// <summary>
        /// Paragraph's line height
        /// </summary>
        public override double LineHeight
        {
            get { return _lineHeight; }
        }

        /// <summary>
        /// Indicates the first line of the paragraph.
        /// </summary>
        public override bool FirstLineInParagraph { get; }

        /// <summary>
        /// If true, the formatted line may always be collapsed. If false (the default),
        /// only lines that overflow the paragraph width are collapsed.
        /// </summary>
        public override bool AlwaysCollapsible { get; }

        /// <summary>
        /// Paragraph's default run properties
        /// </summary>
        public override TextRunProperties DefaultTextRunProperties { get; }

        /// <summary>
        /// This property controls whether or not text wraps when it reaches the flow edge
        /// of its containing block box
        /// </summary>
        public override TextWrapping TextWrapping
        {
            get { return _textWrap; }
        }

        /// <summary>
        /// Line indentation
        /// </summary>
        public override double Indent { get; }

        /// <summary>
        /// Set text flow direction
        /// </summary>
        internal void SetFlowDirection(FlowDirection flowDirection)
        {
            _flowDirection = flowDirection;
        }


        /// <summary>
        /// Set text alignment
        /// </summary>
        internal void SetTextAlignment(TextAlignment textAlignment)
        {
            _textAlignment = textAlignment;
        }


        /// <summary>
        /// Set line height
        /// </summary>
        internal void SetLineHeight(double lineHeight)
        {
            _lineHeight = lineHeight;
        }

        /// <summary>
        /// Set text wrap
        /// </summary>
        internal void SetTextWrapping(TextWrapping textWrap)
        {
            _textWrap = textWrap;
        }
    }
}
