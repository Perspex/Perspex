// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Media;
using System;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Used within the template of a <see cref="T:Avalonia.Controls.DataGrid" /> to specify the 
    /// location in the control's visual tree where the column headers are to be added.
    /// </summary>
    public sealed class DataGridColumnHeadersPresenter : Panel
    {
        private Control _dragIndicator;
        private IControl _dropLocationIndicator;

        /// <summary>
        /// Tracks which column is currently being dragged.
        /// </summary>
        internal DataGridColumn DragColumn
        {
            get;
            set;
        }

        /// <summary>
        /// The current drag indicator control.  This value is null if no column is being dragged.
        /// </summary>
        internal Control DragIndicator
        {
            get
            {
                return _dragIndicator;
            }
            set
            {
                if (value != _dragIndicator)
                {
                    if (Children.Contains(_dragIndicator))
                    {
                        Children.Remove(_dragIndicator);
                    }
                    _dragIndicator = value;
                    if (_dragIndicator != null)
                    {
                        Children.Add(_dragIndicator);
                    }
                }
            }
        }

        /// <summary>
        /// The distance, in pixels, that the DragIndicator should be positioned away from the corresponding DragColumn.
        /// </summary>
        internal Double DragIndicatorOffset
        {
            get;
            set;
        }

        /// <summary>
        /// The drop location indicator control.  This value is null if no column is being dragged.
        /// </summary>
        internal IControl DropLocationIndicator
        {
            get
            {
                return _dropLocationIndicator;
            }
            set
            {
                if (value != _dropLocationIndicator)
                {
                    if (Children.Contains(_dropLocationIndicator))
                    {
                        Children.Remove(_dropLocationIndicator);
                    }
                    _dropLocationIndicator = value;
                    if (_dropLocationIndicator != null)
                    {
                        Children.Add(_dropLocationIndicator);
                    }
                }
            }
        }

        /// <summary>
        /// The distance, in pixels, that the drop location indicator should be positioned away from the left edge
        /// of the ColumnsHeaderPresenter.
        /// </summary>
        internal double DropLocationIndicatorOffset
        {
            get;
            set;
        }

        internal DataGrid OwningGrid
        {
            get;
            set;
        }

        /// <summary>
        /// Arranges the content of the <see cref="T:Avalonia.Controls.Primitives.DataGridColumnHeadersPresenter" />.
        /// </summary>
        /// <returns>
        /// The actual size used by the <see cref="T:Avalonia.Controls.Primitives.DataGridColumnHeadersPresenter" />.
        /// </returns>
        /// <param name="finalSize">
        /// The final area within the parent that this element should use to arrange itself and its children.
        /// </param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (OwningGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            if (OwningGrid.AutoSizingColumns)
            {
                // When we initially load an auto-column, we have to wait for all the rows to be measured
                // before we know its final desired size.  We need to trigger a new round of measures now
                // that the final sizes have been calculated.
                OwningGrid.AutoSizingColumns = false;
                return base.ArrangeOverride(finalSize);
            }

            double dragIndicatorLeftEdge = 0;
            double frozenLeftEdge = 0;
            double scrollingLeftEdge = -OwningGrid.HorizontalOffset;
            foreach (DataGridColumn dataGridColumn in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                DataGridColumnHeader columnHeader = dataGridColumn.HeaderCell;
                Debug.Assert(columnHeader.OwningColumn == dataGridColumn);

                if (dataGridColumn.IsFrozen)
                {
                    columnHeader.Arrange(new Rect(frozenLeftEdge, 0, dataGridColumn.LayoutRoundedWidth, finalSize.Height));
                    columnHeader.Clip = null; // The layout system could have clipped this because it's not aware of our render transform
                    if (DragColumn == dataGridColumn && DragIndicator != null)
                    {
                        dragIndicatorLeftEdge = frozenLeftEdge + DragIndicatorOffset;
                    }
                    frozenLeftEdge += dataGridColumn.ActualWidth;
                }
                else
                {
                    columnHeader.Arrange(new Rect(scrollingLeftEdge, 0, dataGridColumn.LayoutRoundedWidth, finalSize.Height));
                    EnsureColumnHeaderClip(columnHeader, dataGridColumn.ActualWidth, finalSize.Height, frozenLeftEdge, scrollingLeftEdge);
                    if (DragColumn == dataGridColumn && DragIndicator != null)
                    {
                        dragIndicatorLeftEdge = scrollingLeftEdge + DragIndicatorOffset;
                    }
                }
                scrollingLeftEdge += dataGridColumn.ActualWidth;
            }
            if (DragColumn != null)
            {
                if (DragIndicator != null)
                {
                    EnsureColumnReorderingClip(DragIndicator, finalSize.Height, frozenLeftEdge, dragIndicatorLeftEdge);

                    var height = DragIndicator.Bounds.Height;
                    if (height <= 0)
                        height = DragIndicator.DesiredSize.Height;

                    DragIndicator.Arrange(new Rect(dragIndicatorLeftEdge, 0, DragIndicator.Bounds.Width, height));
                }
                if (DropLocationIndicator != null)
                {
                    if (DropLocationIndicator is Control element)
                    {
                        EnsureColumnReorderingClip(element, finalSize.Height, frozenLeftEdge, DropLocationIndicatorOffset);
                    }

                    DropLocationIndicator.Arrange(new Rect(DropLocationIndicatorOffset, 0, DropLocationIndicator.Bounds.Width, DropLocationIndicator.Bounds.Height));
                }
            }

            // Arrange filler
            OwningGrid.OnFillerColumnWidthNeeded(finalSize.Width);
            DataGridFillerColumn fillerColumn = OwningGrid.ColumnsInternal.FillerColumn;
            if (fillerColumn.FillerWidth > 0)
            {
                fillerColumn.HeaderCell.IsVisible = true;
                fillerColumn.HeaderCell.Arrange(new Rect(scrollingLeftEdge, 0, fillerColumn.FillerWidth, finalSize.Height));
            }
            else
            {
                fillerColumn.HeaderCell.IsVisible = false;
            }

            // This needs to be updated after the filler column is configured
            DataGridColumn lastVisibleColumn = OwningGrid.ColumnsInternal.LastVisibleColumn;
            if (lastVisibleColumn != null)
            {
                lastVisibleColumn.HeaderCell.UpdateSeparatorVisibility(lastVisibleColumn);
            }
            return finalSize;
        }

        private static void EnsureColumnHeaderClip(DataGridColumnHeader columnHeader, double width, double height, double frozenLeftEdge, double columnHeaderLeftEdge)
        {
            // Clip the cell only if it's scrolled under frozen columns.  Unfortunately, we need to clip in this case
            // because cells could be transparent
            if (frozenLeftEdge > columnHeaderLeftEdge)
            {
                RectangleGeometry rg = new RectangleGeometry();
                double xClip = Math.Min(width, frozenLeftEdge - columnHeaderLeftEdge);
                rg.Rect = new Rect(xClip, 0, width - xClip, height);
                columnHeader.Clip = rg;
            }
            else
            {
                columnHeader.Clip = null;
            }
        }

        /// <summary>
        /// Clips the DragIndicator and DropLocationIndicator controls according to current ColumnHeaderPresenter constraints.
        /// </summary>
        /// <param name="control">The DragIndicator or DropLocationIndicator</param>
        /// <param name="height">The available height</param>
        /// <param name="frozenColumnsWidth">The width of the frozen column region</param>
        /// <param name="controlLeftEdge">The left edge of the control to clip</param>
        private void EnsureColumnReorderingClip(Control control, double height, double frozenColumnsWidth, double controlLeftEdge)
        {
            double leftEdge = 0;
            double rightEdge = OwningGrid.CellsWidth;
            double width = control.Bounds.Width;
            if (DragColumn.IsFrozen)
            {
                // If we're dragging a frozen column, we want to clip the corresponding DragIndicator control when it goes
                // into the scrolling columns region, but not the DropLocationIndicator.
                if (control == DragIndicator)
                {
                    rightEdge = Math.Min(rightEdge, frozenColumnsWidth);
                }
            }
            else if (OwningGrid.FrozenColumnCount > 0)
            {
                // If we're dragging a scrolling column, we want to clip both the DragIndicator and the DropLocationIndicator
                // controls when they go into the frozen column range.
                leftEdge = frozenColumnsWidth;
            }
            RectangleGeometry rg = null;
            if (leftEdge > controlLeftEdge)
            {
                rg = new RectangleGeometry();
                double xClip = Math.Min(width, leftEdge - controlLeftEdge);
                rg.Rect = new Rect(xClip, 0, width - xClip, height);
            }
            if (controlLeftEdge + width >= rightEdge)
            {
                if (rg == null)
                {
                    rg = new RectangleGeometry();
                }
                rg.Rect = new Rect(rg.Rect.X, rg.Rect.Y, Math.Max(0, rightEdge - controlLeftEdge - rg.Rect.X), height);
            }
            control.Clip = rg;
        }

        /// <summary>
        /// Measures the children of a <see cref="T:Avalonia.Controls.Primitives.DataGridColumnHeadersPresenter" /> to 
        /// prepare for arranging them during the <see cref="M:System.Windows.FrameworkElement.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Indicates an upper limit that child elements should not exceed.
        /// </param>
        /// <returns>
        /// The size that the <see cref="T:Avalonia.Controls.Primitives.DataGridColumnHeadersPresenter" /> determines it needs during layout, based on its calculations of child object allocated sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (OwningGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }
            if (!OwningGrid.AreColumnHeadersVisible)
            {
                return Size.Empty;
            }
            double height = OwningGrid.ColumnHeaderHeight;
            bool autoSizeHeight;
            if (double.IsNaN(height))
            {
                // No explicit height values were set so we can autosize
                height = 0;
                autoSizeHeight = true;
            }
            else
            {
                autoSizeHeight = false;
            }

            double totalDisplayWidth = 0;
            OwningGrid.ColumnsInternal.EnsureVisibleEdgedColumnsWidth();
            DataGridColumn lastVisibleColumn = OwningGrid.ColumnsInternal.LastVisibleColumn;
            foreach (DataGridColumn column in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                // Measure each column header
                bool autoGrowWidth = column.Width.IsAuto || column.Width.IsSizeToHeader;
                DataGridColumnHeader columnHeader = column.HeaderCell;
                if (column != lastVisibleColumn)
                {
                    columnHeader.UpdateSeparatorVisibility(lastVisibleColumn);
                }

                // If we're not using star sizing or the current column can't be resized,
                // then just set the display width according to the column's desired width
                if (!OwningGrid.UsesStarSizing || (!column.ActualCanUserResize && !column.Width.IsStar))
                {
                    // In the edge-case where we're given infinite width and we have star columns, the 
                    // star columns grow to their predefined limit of 10,000 (or their MaxWidth)
                    double newDisplayWidth = column.Width.IsStar ?
                        Math.Min(column.ActualMaxWidth, DataGrid.DATAGRID_maximumStarColumnWidth) :
                        Math.Max(column.ActualMinWidth, Math.Min(column.ActualMaxWidth, column.Width.DesiredValue));
                    column.SetWidthDisplayValue(newDisplayWidth);
                }

                // If we're auto-growing the column based on the header content, we want to measure it at its maximum value
                if (autoGrowWidth)
                {
                    columnHeader.Measure(new Size(column.ActualMaxWidth, double.PositiveInfinity));
                    OwningGrid.AutoSizeColumn(column, columnHeader.DesiredSize.Width);
                    column.ComputeLayoutRoundedWidth(totalDisplayWidth);
                }
                else if (!OwningGrid.UsesStarSizing)
                {
                    column.ComputeLayoutRoundedWidth(totalDisplayWidth);
                    columnHeader.Measure(new Size(column.LayoutRoundedWidth, double.PositiveInfinity));
                }

                // We need to track the largest height in order to auto-size
                if (autoSizeHeight)
                {
                    height = Math.Max(height, columnHeader.DesiredSize.Height);
                }
                totalDisplayWidth += column.ActualWidth;
            }

            // If we're using star sizing (and we're not waiting for an auto-column to finish growing)
            // then we will resize all the columns to fit the available space.
            if (OwningGrid.UsesStarSizing && !OwningGrid.AutoSizingColumns)
            {
                double adjustment = Double.IsPositiveInfinity(availableSize.Width) ? OwningGrid.CellsWidth : availableSize.Width - totalDisplayWidth;
                totalDisplayWidth += adjustment - OwningGrid.AdjustColumnWidths(0, adjustment, false);

                // Since we didn't know the final widths of the columns until we resized,
                // we waited until now to measure each header
                double leftEdge = 0;
                foreach (DataGridColumn column in OwningGrid.ColumnsInternal.GetVisibleColumns())
                {
                    column.ComputeLayoutRoundedWidth(leftEdge);
                    column.HeaderCell.Measure(new Size(column.LayoutRoundedWidth, double.PositiveInfinity));
                    if (autoSizeHeight)
                    {
                        height = Math.Max(height, column.HeaderCell.DesiredSize.Height);
                    }
                    leftEdge += column.ActualWidth;
                }
            }

            // Add the filler column if it's not represented.  We won't know whether we need it or not until Arrange
            DataGridFillerColumn fillerColumn = OwningGrid.ColumnsInternal.FillerColumn;
            if (!fillerColumn.IsRepresented)
            {
                Debug.Assert(!Children.Contains(fillerColumn.HeaderCell));
                fillerColumn.HeaderCell.AreSeparatorsVisible = false;
                Children.Insert(OwningGrid.ColumnsInternal.Count, fillerColumn.HeaderCell);
                fillerColumn.IsRepresented = true;
                // Optimize for the case where we don't need the filler cell 
                fillerColumn.HeaderCell.IsVisible = false;
            }
            fillerColumn.HeaderCell.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (DragIndicator != null)
            {
                DragIndicator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            if (DropLocationIndicator != null)
            {
                DropLocationIndicator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            OwningGrid.ColumnsInternal.EnsureVisibleEdgedColumnsWidth();
            return new Size(OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth, height);
        }
    }
}
