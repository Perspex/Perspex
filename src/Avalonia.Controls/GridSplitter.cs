// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents the control that redistributes space between columns or rows of a <see cref="Grid"/> control.
    /// </summary>
    public class GridSplitter : Thumb
    {
        /// <summary>
        /// Defines the <see cref="ResizeDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<GridResizeDirection> ResizeDirectionProperty =
            AvaloniaProperty.Register<GridSplitter, GridResizeDirection>(nameof(ResizeDirection));

        /// <summary>
        /// Defines the <see cref="ResizeBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<GridResizeBehavior> ResizeBehaviorProperty =
            AvaloniaProperty.Register<GridSplitter, GridResizeBehavior>(nameof(ResizeBehavior));

        /// <summary>
        /// Defines the <see cref="ShowsPreview"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowsPreviewProperty =
            AvaloniaProperty.Register<GridSplitter, bool>(nameof(ShowsPreview));

        /// <summary>
        /// Defines the <see cref="KeyboardIncrement"/> property.
        /// </summary>
        public static readonly StyledProperty<double> KeyboardIncrementProperty =
            AvaloniaProperty.Register<GridSplitter, double>(nameof(KeyboardIncrement), 10d);

        /// <summary>
        /// Defines the <see cref="DragIncrement"/> property.
        /// </summary>
        public static readonly StyledProperty<double> DragIncrementProperty =
            AvaloniaProperty.Register<GridSplitter, double>(nameof(DragIncrement), 1d);

        /// <summary>
        /// Defines the <see cref="PreviewContent"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IControl>> PreviewContentProperty =
            AvaloniaProperty.Register<GridSplitter, ITemplate<IControl>>(nameof(PreviewContent));

        private static readonly Cursor s_columnSplitterCursor = new Cursor(StandardCursorType.SizeWestEast);
        private static readonly Cursor s_rowSplitterCursor = new Cursor(StandardCursorType.SizeNorthSouth);

        private ResizeData _resizeData;

        /// <summary>
        /// Indicates whether the Splitter resizes the Columns, Rows, or Both.
        /// </summary>
        public GridResizeDirection ResizeDirection
        {
            get => GetValue(ResizeDirectionProperty);
            set => SetValue(ResizeDirectionProperty, value);
        }

        /// <summary>
        /// Indicates which Columns or Rows the Splitter resizes.
        /// </summary>
        public GridResizeBehavior ResizeBehavior
        {
            get => GetValue(ResizeBehaviorProperty);
            set => SetValue(ResizeBehaviorProperty, value);
        }

        /// <summary>
        /// Indicates whether to Preview the column resizing without updating layout.
        /// </summary>
        public bool ShowsPreview
        {
            get => GetValue(ShowsPreviewProperty);
            set => SetValue(ShowsPreviewProperty, value);
        }

        /// <summary>
        /// The Distance to move the splitter when pressing the keyboard arrow keys.
        /// </summary>
        public double KeyboardIncrement
        {
            get => GetValue(KeyboardIncrementProperty);
            set => SetValue(KeyboardIncrementProperty, value);
        }

        /// <summary>
        /// Restricts splitter to move a multiple of the specified units.
        /// </summary>
        public double DragIncrement
        {
            get => GetValue(DragIncrementProperty);
            set => SetValue(DragIncrementProperty, value);
        }

        /// <summary>
        /// Gets or sets content that will be shown when <see cref="ShowsPreview"/> is enabled and user starts resize operation.
        /// </summary>
        public ITemplate<IControl> PreviewContent
        {
            get => GetValue(PreviewContentProperty);
            set => SetValue(PreviewContentProperty, value);
        }

        /// <summary>
        /// Converts BasedOnAlignment direction to Rows, Columns, or Both depending on its width/height.
        /// </summary>
        internal GridResizeDirection GetEffectiveResizeDirection()
        {
            GridResizeDirection direction = ResizeDirection;

            if (direction != GridResizeDirection.Auto)
            {
                return direction;
            }

            // When HorizontalAlignment is Left, Right or Center, resize Columns.
            if (HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                direction = GridResizeDirection.Columns;
            }
            else if (VerticalAlignment != VerticalAlignment.Stretch)
            {
                direction = GridResizeDirection.Rows;
            }
            else if (Bounds.Width <= Bounds.Height) // Fall back to Width vs Height.
            {
                direction = GridResizeDirection.Columns;
            }
            else
            {
                direction = GridResizeDirection.Rows;
            }

            return direction;
        }

        /// <summary>
        /// Convert BasedOnAlignment to Next/Prev/Both depending on alignment and Direction.
        /// </summary>
        private GridResizeBehavior GetEffectiveResizeBehavior(GridResizeDirection direction)
        {
            GridResizeBehavior resizeBehavior = ResizeBehavior;

            if (resizeBehavior == GridResizeBehavior.BasedOnAlignment)
            {
                if (direction == GridResizeDirection.Columns)
                {
                    switch (HorizontalAlignment)
                    {
                        case HorizontalAlignment.Left:
                            resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                            break;
                        case HorizontalAlignment.Right:
                            resizeBehavior = GridResizeBehavior.CurrentAndNext;
                            break;
                        default:
                            resizeBehavior = GridResizeBehavior.PreviousAndNext;
                            break;
                    }
                }
                else
                {
                    switch (VerticalAlignment)
                    {
                        case VerticalAlignment.Top:
                            resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                            break;
                        case VerticalAlignment.Bottom:
                            resizeBehavior = GridResizeBehavior.CurrentAndNext;
                            break;
                        default:
                            resizeBehavior = GridResizeBehavior.PreviousAndNext;
                            break;
                    }
                }
            }

            return resizeBehavior;
        }

        /// <summary>
        /// Removes preview adorner from the grid.
        /// </summary>
        private void RemovePreviewAdorner()
        {
            if (_resizeData.Adorner != null)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
                layer.Children.Remove(_resizeData.Adorner);
            }
        }

        /// <summary>
        /// Initialize the data needed for resizing.
        /// </summary>
        private void InitializeData(bool showsPreview)
        {
            // If not in a grid or can't resize, do nothing.
            if (Parent is Grid grid)
            {
                GridResizeDirection resizeDirection = GetEffectiveResizeDirection();

                // Setup data used for resizing.
                _resizeData = new ResizeData
                {
                    Grid = grid,
                    ShowsPreview = showsPreview,
                    ResizeDirection = resizeDirection,
                    SplitterLength = Math.Min(Bounds.Width, Bounds.Height),
                    ResizeBehavior = GetEffectiveResizeBehavior(resizeDirection)
                };

                // Store the rows and columns to resize on drag events.
                if (!SetupDefinitionsToResize())
                {
                    // Unable to resize, clear data.
                    _resizeData = null;
                    return;
                }

                // Setup the preview in the adorner if ShowsPreview is true.
                SetupPreviewAdorner();
            }
        }

        /// <summary>
        /// Returns true if GridSplitter can resize rows/columns.
        /// </summary>
        private bool SetupDefinitionsToResize()
        {
            int gridSpan = GetValue(_resizeData.ResizeDirection == GridResizeDirection.Columns ?
                Grid.ColumnSpanProperty :
                Grid.RowSpanProperty);

            if (gridSpan == 1)
            {
                var splitterIndex = GetValue(_resizeData.ResizeDirection == GridResizeDirection.Columns ?
                    Grid.ColumnProperty :
                    Grid.RowProperty);

                // Select the columns based on behavior.
                int index1, index2;

                switch (_resizeData.ResizeBehavior)
                {
                    case GridResizeBehavior.PreviousAndCurrent:
                        // Get current and previous.
                        index1 = splitterIndex - 1;
                        index2 = splitterIndex;
                        break;
                    case GridResizeBehavior.CurrentAndNext:
                        // Get current and next.
                        index1 = splitterIndex;
                        index2 = splitterIndex + 1;
                        break;
                    default: // GridResizeBehavior.PreviousAndNext.
                        // Get previous and next.
                        index1 = splitterIndex - 1;
                        index2 = splitterIndex + 1;
                        break;
                }

                // Get count of rows/columns in the resize direction.
                int count = _resizeData.ResizeDirection == GridResizeDirection.Columns ?
                    _resizeData.Grid.ColumnDefinitions?.Count ?? 0  :
                    _resizeData.Grid.RowDefinitions?.Count ?? 0;

                if (index1 >= 0 && index2 < count)
                {
                    _resizeData.SplitterIndex = splitterIndex;

                    _resizeData.Definition1Index = index1;
                    _resizeData.Definition1 = GetGridDefinition(_resizeData.Grid, index1, _resizeData.ResizeDirection);
                    _resizeData.OriginalDefinition1Length =
                        _resizeData.Definition1.UserSizeValueCache; // Save Size if user cancels.
                    _resizeData.OriginalDefinition1ActualLength = GetActualLength(_resizeData.Definition1);

                    _resizeData.Definition2Index = index2;
                    _resizeData.Definition2 = GetGridDefinition(_resizeData.Grid, index2, _resizeData.ResizeDirection);
                    _resizeData.OriginalDefinition2Length =
                        _resizeData.Definition2.UserSizeValueCache; // Save Size if user cancels.
                    _resizeData.OriginalDefinition2ActualLength = GetActualLength(_resizeData.Definition2);

                    // Determine how to resize the columns.
                    bool isStar1 = IsStar(_resizeData.Definition1);
                    bool isStar2 = IsStar(_resizeData.Definition2);

                    if (isStar1 && isStar2)
                    {
                        // If they are both stars, resize both.
                        _resizeData.SplitBehavior = SplitBehavior.Split;
                    }
                    else
                    {
                        // One column is fixed width, resize the first one that is fixed.
                        _resizeData.SplitBehavior = !isStar1 ? SplitBehavior.Resize1 : SplitBehavior.Resize2;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create the preview adorner and add it to the adorner layer.
        /// </summary>
        private void SetupPreviewAdorner()
        {
            if (_resizeData.ShowsPreview)
            {
                // Get the adorner layer and add an adorner to it.
                var adornerLayer = AdornerLayer.GetAdornerLayer(_resizeData.Grid);

                var previewContent = PreviewContent;

                // Can't display preview.
                if (adornerLayer == null)
                {
                    return;
                }

                IControl builtPreviewContent = previewContent?.Build();

                _resizeData.Adorner = new PreviewAdorner(builtPreviewContent);

                AdornerLayer.SetAdornedElement(_resizeData.Adorner, this);

                adornerLayer.Children.Add(_resizeData.Adorner);

                // Get constraints on preview's translation.
                GetDeltaConstraints(out _resizeData.MinChange, out _resizeData.MaxChange);
            }
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            GridResizeDirection direction = GetEffectiveResizeDirection();

            switch (direction)
            {
                case GridResizeDirection.Columns:
                    Cursor = s_columnSplitterCursor;
                    break;
                case GridResizeDirection.Rows:
                    Cursor = s_rowSplitterCursor;
                    break;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (_resizeData != null)
            {
                CancelResize();
            }
        }

        protected override void OnDragStarted(VectorEventArgs e)
        {
            base.OnDragStarted(e);

            // TODO: Looks like that sometimes thumb will raise multiple drag started events.
            // Debug.Assert(_resizeData == null, "_resizeData is not null, DragCompleted was not called");

            if (_resizeData != null)
            {
                return;
            }

            InitializeData(ShowsPreview);
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            base.OnDragDelta(e);

            if (_resizeData != null)
            {
                double horizontalChange = e.Vector.X;
                double verticalChange = e.Vector.Y;

                // Round change to nearest multiple of DragIncrement.
                double dragIncrement = DragIncrement;
                horizontalChange = Math.Round(horizontalChange / dragIncrement) * dragIncrement;
                verticalChange = Math.Round(verticalChange / dragIncrement) * dragIncrement;

                if (_resizeData.ShowsPreview)
                {
                    // Set the Translation of the Adorner to the distance from the thumb.
                    if (_resizeData.ResizeDirection == GridResizeDirection.Columns)
                    {
                        _resizeData.Adorner.OffsetX = Math.Min(
                            Math.Max(horizontalChange, _resizeData.MinChange),
                            _resizeData.MaxChange);
                    }
                    else
                    {
                        _resizeData.Adorner.OffsetY = Math.Min(
                            Math.Max(verticalChange, _resizeData.MinChange),
                            _resizeData.MaxChange);
                    }
                }
                else
                {
                    // Directly update the grid.
                    MoveSplitter(horizontalChange, verticalChange);
                }
            }
        }

        protected override void OnDragCompleted(VectorEventArgs e)
        {
            base.OnDragCompleted(e);

            if (_resizeData != null)
            {
                if (_resizeData.ShowsPreview)
                {
                    // Update the grid.
                    MoveSplitter(_resizeData.Adorner.OffsetX, _resizeData.Adorner.OffsetY);
                    RemovePreviewAdorner();
                }

                _resizeData = null;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Key key = e.Key;

            switch (key)
            {
                case Key.Escape:
                    if (_resizeData != null)
                    {
                        CancelResize();
                        e.Handled = true;
                    }

                    break;

                case Key.Left:
                    e.Handled = KeyboardMoveSplitter(-KeyboardIncrement, 0);
                    break;
                case Key.Right:
                    e.Handled = KeyboardMoveSplitter(KeyboardIncrement, 0);
                    break;
                case Key.Up:
                    e.Handled = KeyboardMoveSplitter(0, -KeyboardIncrement);
                    break;
                case Key.Down:
                    e.Handled = KeyboardMoveSplitter(0, KeyboardIncrement);
                    break;
            }
        }

        /// <summary>
        /// Cancels the resize operation.
        /// </summary>
        private void CancelResize()
        {
            // Restore original column/row lengths.
            if (_resizeData.ShowsPreview)
            {
                RemovePreviewAdorner();
            }
            else // Reset the columns/rows lengths to the saved values.
            {
                SetDefinitionLength(_resizeData.Definition1, _resizeData.OriginalDefinition1Length);
                SetDefinitionLength(_resizeData.Definition2, _resizeData.OriginalDefinition2Length);
            }

            _resizeData = null;
        }

        /// <summary>
        /// Returns true if the row/column has a star length.
        /// </summary>
        private static bool IsStar(DefinitionBase definition)
        {
            return definition.UserSizeValueCache.IsStar;
        }

        /// <summary>
        /// Gets Column or Row definition at index from grid based on resize direction.
        /// </summary>
        private static DefinitionBase GetGridDefinition(Grid grid, int index, GridResizeDirection direction)
        {
            return direction == GridResizeDirection.Columns ?
                (DefinitionBase)grid.ColumnDefinitions[index] :
                (DefinitionBase)grid.RowDefinitions[index];
        }

        /// <summary>
        /// Retrieves the ActualWidth or ActualHeight of the definition depending on its type Column or Row.
        /// </summary>
        private double GetActualLength(DefinitionBase definition)
        {
            var column = definition as ColumnDefinition;

            return column?.ActualWidth ?? ((RowDefinition)definition).ActualHeight;
        }

        /// <summary>
        /// Gets Column or Row definition at index from grid based on resize direction.
        /// </summary>
        private static void SetDefinitionLength(DefinitionBase definition, GridLength length)
        {
            definition.SetValue(
                definition is ColumnDefinition ? ColumnDefinition.WidthProperty : RowDefinition.HeightProperty, length);
        }

        /// <summary>
        /// Get the minimum and maximum Delta can be given definition constraints (MinWidth/MaxWidth).
        /// </summary>
        private void GetDeltaConstraints(out double minDelta, out double maxDelta)
        {
            double definition1Len = GetActualLength(_resizeData.Definition1);
            double definition1Min = _resizeData.Definition1.UserMinSizeValueCache;
            double definition1Max = _resizeData.Definition1.UserMaxSizeValueCache;

            double definition2Len = GetActualLength(_resizeData.Definition2);
            double definition2Min = _resizeData.Definition2.UserMinSizeValueCache;
            double definition2Max = _resizeData.Definition2.UserMaxSizeValueCache;

            // Set MinWidths to be greater than width of splitter.
            if (_resizeData.SplitterIndex == _resizeData.Definition1Index)
            {
                definition1Min = Math.Max(definition1Min, _resizeData.SplitterLength);
            }
            else if (_resizeData.SplitterIndex == _resizeData.Definition2Index)
            {
                definition2Min = Math.Max(definition2Min, _resizeData.SplitterLength);
            }

            // Determine the minimum and maximum the columns can be resized.
            minDelta = -Math.Min(definition1Len - definition1Min, definition2Max - definition2Len);
            maxDelta = Math.Min(definition1Max - definition1Len, definition2Len - definition2Min);
        }

        /// <summary>
        /// Sets the length of definition1 and definition2.
        /// </summary>
        private void SetLengths(double definition1Pixels, double definition2Pixels)
        {
            // For the case where both definition1 and 2 are stars, update all star values to match their current pixel values.
            if (_resizeData.SplitBehavior == SplitBehavior.Split)
            {
                var definitions = _resizeData.ResizeDirection == GridResizeDirection.Columns ?
                    (IAvaloniaReadOnlyList<DefinitionBase>)_resizeData.Grid.ColumnDefinitions :
                    (IAvaloniaReadOnlyList<DefinitionBase>)_resizeData.Grid.RowDefinitions;

                var definitionsCount = definitions.Count;

                for (var i = 0; i < definitionsCount; i++)
                {
                    DefinitionBase definition = definitions[i];

                    // For each definition, if it is a star, set is value to ActualLength in stars
                    // This makes 1 star == 1 pixel in length
                    if (i == _resizeData.Definition1Index)
                    {
                        SetDefinitionLength(definition, new GridLength(definition1Pixels, GridUnitType.Star));
                    }
                    else if (i == _resizeData.Definition2Index)
                    {
                        SetDefinitionLength(definition, new GridLength(definition2Pixels, GridUnitType.Star));
                    }
                    else if (IsStar(definition))
                    {
                        SetDefinitionLength(definition, new GridLength(GetActualLength(definition), GridUnitType.Star));
                    }
                }
            }
            else if (_resizeData.SplitBehavior == SplitBehavior.Resize1)
            {
                SetDefinitionLength(_resizeData.Definition1, new GridLength(definition1Pixels));
            }
            else
            {
                SetDefinitionLength(_resizeData.Definition2, new GridLength(definition2Pixels));
            }
        }

        /// <summary>
        /// Move the splitter by the given Delta's in the horizontal and vertical directions.
        /// </summary>
        private void MoveSplitter(double horizontalChange, double verticalChange)
        {
            Debug.Assert(_resizeData != null, "_resizeData should not be null when calling MoveSplitter");

            // Calculate the offset to adjust the splitter.
            var delta = _resizeData.ResizeDirection == GridResizeDirection.Columns ? horizontalChange : verticalChange;

            DefinitionBase definition1 = _resizeData.Definition1;
            DefinitionBase definition2 = _resizeData.Definition2;

            if (definition1 != null && definition2 != null)
            {
                double actualLength1 = GetActualLength(definition1);
                double actualLength2 = GetActualLength(definition2);

                // When splitting, Check to see if the total pixels spanned by the definitions 
                // is the same asbefore starting resize. If not cancel the drag
                if (_resizeData.SplitBehavior == SplitBehavior.Split &&
                    !MathUtilities.AreClose(
                        actualLength1 + actualLength2,
                        _resizeData.OriginalDefinition1ActualLength + _resizeData.OriginalDefinition2ActualLength))
                {
                    CancelResize();

                    return;
                }

                GetDeltaConstraints(out var min, out var max);

                // Constrain Delta to Min/MaxWidth of columns
                delta = Math.Min(Math.Max(delta, min), max);

                double definition1LengthNew = actualLength1 + delta;
                double definition2LengthNew = actualLength1 + actualLength2 - definition1LengthNew;

                SetLengths(definition1LengthNew, definition2LengthNew);
            }
        }

        /// <summary>
        /// Move the splitter using the Keyboard (Don't show preview).
        /// </summary>
        private bool KeyboardMoveSplitter(double horizontalChange, double verticalChange)
        {
            // If moving with the mouse, ignore keyboard motion.
            if (_resizeData != null)
            {
                return false; // Don't handle the event.
            }

            // Don't show preview.
            InitializeData(false); 
                
            // Check that we are actually able to resize.
            if (_resizeData == null)
            {
                return false; // Don't handle the event.
            }

            MoveSplitter(horizontalChange, verticalChange);

            _resizeData = null;

            return true;
        }

        /// <summary>
        /// This adorner draws the preview for the <see cref="GridSplitter"/>.
        /// It also positions the adorner.
        /// </summary>
        private sealed class PreviewAdorner : Decorator
        {
            private readonly TranslateTransform _translation;
            private readonly Decorator _decorator;
            
            public PreviewAdorner(IControl previewControl)
            {
                // Add a decorator to perform translations.
                _translation = new TranslateTransform();

                _decorator = new Decorator
                {
                    Child = previewControl, 
                    RenderTransform = _translation
                };

                Child = _decorator;
            }

            /// <summary>
            /// The Preview's Offset in the X direction from the GridSplitter.
            /// </summary>
            public double OffsetX
            {
                get => _translation.X;
                set => _translation.X = value;
            }

            /// <summary>
            /// The Preview's Offset in the Y direction from the GridSplitter.
            /// </summary>
            public double OffsetY
            {
                get => _translation.Y;
                set => _translation.Y = value;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                // Adorners always get clipped to the owner control. In this case we want
                // to constrain size to the splitter size but draw on top of the parent grid.
                Clip = null;

                return base.ArrangeOverride(finalSize);
            }
        }

        /// <summary>
        /// <see cref="GridSplitter"/> has special Behavior when columns are fixed.
        /// If the left column is fixed, splitter will only resize that column.
        /// Else if the right column is fixed, splitter will only resize the right column.
        /// </summary>
        private enum SplitBehavior
        {
            /// <summary>
            /// Both columns/rows are star lengths.
            /// </summary>
            Split,

            /// <summary>
            /// Resize 1 only.
            /// </summary>
            Resize1,

            /// <summary>
            /// Resize 2 only.
            /// </summary>
            Resize2
        }

        /// <summary>
        /// Stores data during the resizing operation.
        /// </summary>
        private class ResizeData
        {
            public bool ShowsPreview;
            public PreviewAdorner Adorner;

            // The constraints to keep the Preview within valid ranges.
            public double MinChange;
            public double MaxChange;

            // The grid to Resize.
            public Grid Grid;

            // Cache of Resize Direction and Behavior.
            public GridResizeDirection ResizeDirection;
            public GridResizeBehavior ResizeBehavior;

            // The columns/rows to resize.
            public DefinitionBase Definition1;
            public DefinitionBase Definition2;

            // Are the columns/rows star lengths.
            public SplitBehavior SplitBehavior;

            // The index of the splitter.
            public int SplitterIndex;

            // The indices of the columns/rows.
            public int Definition1Index;
            public int Definition2Index;

            // The original lengths of Definition1 and Definition2 (to restore lengths if user cancels resize).
            public GridLength OriginalDefinition1Length;
            public GridLength OriginalDefinition2Length;
            public double OriginalDefinition1ActualLength;
            public double OriginalDefinition2ActualLength;

            // The minimum of Width/Height of Splitter.  Used to ensure splitter 
            // isn't hidden by resizing a row/column smaller than the splitter.
            public double SplitterLength;
        }
    }

    /// <summary>
    /// Enum to indicate whether <see cref="GridSplitter"/> resizes Columns or Rows.
    /// </summary>
    public enum GridResizeDirection
    {
        /// <summary>
        /// Determines whether to resize rows or columns based on its Alignment and 
        /// width compared to height.
        /// </summary>
        Auto,

        /// <summary>
        /// Resize columns when dragging Splitter.
        /// </summary>
        Columns,

        /// <summary>
        /// Resize rows when dragging Splitter.
        /// </summary>
        Rows
    }

    /// <summary>
    /// Enum to indicate what Columns or Rows the <see cref="GridSplitter"/> resizes.
    /// </summary>
    public enum GridResizeBehavior
    {
        /// <summary>
        /// Determine which columns or rows to resize based on its Alignment.
        /// </summary>
        BasedOnAlignment,

        /// <summary>
        /// Resize the current and next Columns or Rows.
        /// </summary>
        CurrentAndNext,

        /// <summary>
        /// Resize the previous and current Columns or Rows.
        /// </summary>
        PreviousAndCurrent,

        /// <summary>
        /// Resize the previous and next Columns or Rows.
        /// </summary>
        PreviousAndNext
    }
}
