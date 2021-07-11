using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The UniformGrid control presents information within a Grid with even spacing.
    /// </summary>
    public partial class UniformGrid : Grid
    {
        static UniformGrid()
        {
            AffectsMeasure<UniformGrid>(ColumnsProperty, RowsProperty, FirstColumnProperty, OrientationProperty);
        }

        // Internal list we use to keep track of items that we don't have space to layout.
        private List<Control> _overflow = new List<Control>();

        /// <summary>
        /// The <see cref="TakenSpotsReferenceHolder"/> instance in use, if any.
        /// </summary>
        private TakenSpotsReferenceHolder _spotref;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Get all Visible FrameworkElement Children
            var visible = Children.OfType<Control>().Where(item => item.IsVisible).ToArray();

            var (rows, columns) = GetDimensions(visible, Rows, Columns, FirstColumn);

            // Now that we know size, setup automatic rows/columns
            // to utilize Grid for UniformGrid behavior.
            // We also interleave any specified rows/columns with fixed sizes.
            SetupRowDefinitions(rows);
            SetupColumnDefinitions(columns);

            TakenSpotsReferenceHolder spotref;

            // If the last spot holder matches the size currently in use, just reset
            // that instance and reuse it to avoid allocating a new bit array.
            if (_spotref != null && _spotref.Height == rows && _spotref.Width == columns)
            {
                spotref = _spotref;

                spotref.Reset();
            }
            else
            {
                spotref = _spotref = new TakenSpotsReferenceHolder(rows, columns);
            }

            // Figure out which children we should automatically layout and where available openings are.
            foreach (var child in visible)
            {
                var row = GetRow(child);
                var col = GetColumn(child);
                var rowspan = GetRowSpan(child);
                var colspan = GetColumnSpan(child);

                // If an element needs to be forced in the 0, 0 position,
                // they should manually set UniformGrid.AutoLayout to False for that element.
                if ((row == 0 && col == 0 && GetAutoLayout(child) == null) ||
                    GetAutoLayout(child) == true)
                {
                    SetAutoLayout(child, true);
                }
                else
                {
                    SetAutoLayout(child, false);

                    spotref.Fill(true, row, col, colspan, rowspan);
                }
            }

            // Setup available size with our known dimensions now.
            // UniformGrid expands size based on largest singular item.
            double columnSpacingSize = 0;
            double rowSpacingSize = 0;

            // TODO: implement Row/ColumnSpacing
            //columnSpacingSize = ColumnSpacing * (columns - 1);
            //rowSpacingSize = RowSpacing * (rows - 1);

            Size childSize = new Size(
                (availableSize.Width - columnSpacingSize) / columns,
                (availableSize.Height - rowSpacingSize) / rows);

            double maxWidth = 0.0;
            double maxHeight = 0.0;

            // Set Grid Row/Col for every child with autolayout = true
            // Backwards with FlowDirection
            var freespots = GetFreeSpot(spotref, FirstColumn, Orientation == Avalonia.Layout.Orientation.Vertical).GetEnumerator();
            foreach (var child in visible)
            {
                // Set location if we're in charge
                if (GetAutoLayout(child) == true)
                {
                    if (freespots.MoveNext())
                    {
                        var (row, column) = freespots.Current;

                        SetRow(child, row);
                        SetColumn(child, column);

                        var rowspan = GetRowSpan(child);
                        var colspan = GetColumnSpan(child);

                        if (rowspan > 1 || colspan > 1)
                        {
                            // TODO: Need to tie this into iterator
                            spotref.Fill(true, row, column, colspan, rowspan);
                        }
                    }
                    else
                    {
                        // We've run out of spots as the developer has
                        // most likely given us a fixed size and too many elements
                        // Therefore, tell this element it has no size and move on.
                        child.Measure(Size.Empty);

                        _overflow.Add(child);

                        continue;
                    }
                }
                else if (GetRow(child) < 0 || GetRow(child) >= rows ||
                         GetColumn(child) < 0 || GetColumn(child) >= columns)
                {
                    // A child is specifying a location, but that location is outside
                    // of our grid space, so we should hide it instead.
                    child.Measure(Size.Empty);

                    _overflow.Add(child);

                    continue;
                }

                // Get measurement for max child
                child.Measure(childSize);

                maxWidth = Math.Max(child.DesiredSize.Width, maxWidth);
                maxHeight = Math.Max(child.DesiredSize.Height, maxHeight);
            }

            // Return our desired size based on the largest child we found, our dimensions, and spacing.
            var desiredSize = new Size((maxWidth * columns) + columnSpacingSize, (maxHeight * rows) + rowSpacingSize);

            // Required to perform regular grid measurement, but ignore result.
            base.MeasureOverride(availableSize);

            return desiredSize;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Have grid to the bulk of our heavy lifting.
            var size = base.ArrangeOverride(finalSize);

            // Make sure all overflown elements have no size.
            foreach (var child in _overflow)
            {
                child.Arrange(default);
            }

            _overflow = new List<Control>(); // Reset for next time.

            return size;
        }
    }
}
