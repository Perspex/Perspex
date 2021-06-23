using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines layout-related functionality for a control.
    /// </summary>
    public interface ILayoutable : IVisual
    {
        /// <summary>
        /// Gets the size that this element computed during the measure pass of the layout process.
        /// </summary>
        Size DesiredSize { get; }

        /// <summary>
        /// Gets the width of the element.
        /// </summary>
        double Width { get; }

        /// <summary>
        /// Gets the height of the element.
        /// </summary>
        double Height { get; }

        /// <summary>
        /// Gets the minimum width of the element.
        /// </summary>
        double MinWidth { get; }

        /// <summary>
        /// Gets the maximum width of the element.
        /// </summary>
        double MaxWidth { get; }

        /// <summary>
        /// Gets the minimum height of the element.
        /// </summary>
        double MinHeight { get; }

        /// <summary>
        /// Gets the maximum height of the element.
        /// </summary>
        double MaxHeight { get; }

        /// <summary>
        /// Gets the margin around the element.
        /// </summary>
        Thickness Margin { get; }

        /// <summary>
        /// Gets the element's preferred horizontal alignment in its parent.
        /// </summary>
        HorizontalAlignment HorizontalAlignment { get; }

        /// <summary>
        /// Gets the element's preferred vertical alignment in its parent.
        /// </summary>
        VerticalAlignment VerticalAlignment { get; }

        /// <summary>
        /// Gets a value indicating whether the control's layout measure is valid.
        /// </summary>
        bool IsMeasureValid { get; }

        /// <summary>
        /// Gets a value indicating whether the control's layouts arrange is valid.
        /// </summary>
        bool IsArrangeValid { get; }

        /// <summary>
        /// Gets the available size passed in the previous layout pass, if any.
        /// </summary>
        Size? PreviousMeasure { get; }

        /// <summary>
        /// Gets the layout rect passed in the previous layout pass, if any.
        /// </summary>
        Rect? PreviousArrange { get; }

        /// <summary>
        /// Creates the visual children of the control, if necessary
        /// </summary>
        void ApplyTemplate();

        /// <summary>
        /// Carries out a measure of the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        void Measure(Size availableSize);

        /// <summary>
        /// Arranges the control and its children.
        /// </summary>
        /// <param name="rect">The control's new bounds.</param>
        void Arrange(Rect rect);

        /// <summary>
        /// Invalidates the measurement of the control and queues a new layout pass.
        /// </summary>
        void InvalidateMeasure();

        /// <summary>
        /// Invalidates the arrangement of the control and queues a new layout pass.
        /// </summary>
        void InvalidateArrange();

        /// <summary>
        /// Called when a child control's desired size changes.
        /// </summary>
        /// <param name="control">The child control.</param>
        void ChildDesiredSizeChanged(ILayoutable control);

        /// <summary>
        /// Used by the <see cref="LayoutManager"/> to notify the control that its effective
        /// viewport is changed.
        /// </summary>
        /// <param name="e">The viewport information.</param>
        void EffectiveViewportChanged(EffectiveViewportChangedEventArgs e);
    }
}
