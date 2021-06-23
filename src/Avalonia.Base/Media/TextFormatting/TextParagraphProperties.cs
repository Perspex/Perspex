namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Provides a set of properties that are used during the paragraph layout.
    /// </summary>
    public abstract class TextParagraphProperties
    {
        /// <summary>
        /// This property specifies whether the primary text advance 
        /// direction shall be left-to-right, right-to-left, or top-to-bottom.
        /// </summary>
        public abstract FlowDirection FlowDirection { get; }

        /// <summary>
        /// Gets the text alignment.
        /// </summary>
        public abstract TextAlignment TextAlignment { get; }

        /// <summary>
        /// Paragraph's line height
        /// </summary>
        public abstract double LineHeight { get; }

        /// <summary>
        /// Indicates the first line of the paragraph.
        /// </summary>
        public abstract bool FirstLineInParagraph { get; }

        /// <summary>
        /// If true, the formatted line may always be collapsed. If false (the default),
        /// only lines that overflow the paragraph width are collapsed.
        /// </summary>
        public virtual bool AlwaysCollapsible
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the default text style.
        /// </summary>
        public abstract TextRunProperties DefaultTextRunProperties { get; }

        /// <summary>
        /// If not null, text decorations to apply to all runs in the line. This is in addition
        /// to any text decorations specified by the TextRunProperties for individual text runs.
        /// </summary>
        public virtual TextDecorationCollection TextDecorations => null;

        /// <summary>
        /// Gets the text wrapping.
        /// </summary>
        public abstract TextWrapping TextWrapping { get; }

        /// <summary>
        /// Line indentation
        /// </summary>
        public abstract double Indent { get; }

        /// <summary>
        /// Paragraph indentation
        /// </summary>
        public virtual double ParagraphIndent
        {
            get { return 0; }
        }
    }
}
