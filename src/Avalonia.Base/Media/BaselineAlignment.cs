namespace Avalonia.Media
{
    /// <summary>
    /// Enum specifying where a box should be positioned Vertically
    /// </summary>
    public enum BaselineAlignment
    {
        /// <summary>Align top toward top of container</summary>
        Top,

        /// <summary>Center vertically</summary>
        Center,

        /// <summary>Align bottom toward bottom of container</summary>
        Bottom,

        /// <summary>Align at baseline</summary>
        Baseline,

        /// <summary>Align toward text's top of container</summary>
        TextTop,

        /// <summary>Align toward text's bottom of container</summary>
        TextBottom,

        /// <summary>Align baseline to subscript position of container</summary>
        Subscript,

        /// <summary>Align baseline to superscript position of container</summary>
        Superscript,
    }
}
