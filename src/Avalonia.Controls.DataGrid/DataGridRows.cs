// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Avalonia.Media;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Avalonia.Controls
{
    public partial class DataGrid
    {

        internal bool AreRowBottomGridLinesRequired
        {
            get
            {
                return (GridLinesVisibility == DataGridGridLinesVisibility.Horizontal || GridLinesVisibility == DataGridGridLinesVisibility.All) && HorizontalGridLinesBrush != null;
            }
        }

        internal int FirstVisibleSlot
        {
            get
            {
                return (SlotCount > 0) ? GetNextVisibleSlot(-1) : -1;
            }
        }

        internal int FrozenColumnCountWithFiller
        {
            get
            {
                int count = FrozenColumnCount;
                if (ColumnsInternal.RowGroupSpacerColumn.IsRepresented && (AreRowGroupHeadersFrozen || count > 0))
                {
                    // Either the RowGroupHeaders are frozen by default or the user set a frozen column count.  In both cases, we need to freeze
                    // one more column than the what the public value says
                    count++;
                }
                return count;
            }
        }

        internal int LastVisibleSlot
        {
            get
            {
                return (SlotCount > 0) ? GetPreviousVisibleSlot(SlotCount) : -1;
            }
        }

        // Cumulated height of all known rows, including the gridlines and details section.
        // This property returns an approximation of the actual total row heights and also
        // updates the RowHeightEstimate
        private double EdgedRowsHeightCalculated
        {
            get
            {
                // If we're not displaying any rows or if we have infinite space the, relative height of our rows is 0
                if (DisplayData.LastScrollingSlot == -1 || double.IsPositiveInfinity(AvailableSlotElementRoom))
                {
                    return 0;
                }
                Debug.Assert(DisplayData.LastScrollingSlot >= 0);
                Debug.Assert(_verticalOffset >= 0);
                Debug.Assert(NegVerticalOffset >= 0);

                // Height of all rows above the viewport
                double totalRowsHeight = _verticalOffset - NegVerticalOffset;

                // Add the height of all the rows currently displayed, AvailableRowRoom
                // is not always up to date enough for this
                foreach (Control element in DisplayData.GetScrollingElements())
                {
                    if (element is DataGridRow row)
                    {
                        totalRowsHeight += row.TargetHeight;
                    }
                    else
                    {
                        totalRowsHeight += element.DesiredSize.Height;
                    }
                }

                // Details up to and including viewport
                int detailsCount = GetDetailsCountInclusive(0, DisplayData.LastScrollingSlot);

                // Subtract details that were accounted for from the totalRowsHeight
                totalRowsHeight -= detailsCount * RowDetailsHeightEstimate;

                // Update the RowHeightEstimate if we have more row information
                if (DisplayData.LastScrollingSlot >= _lastEstimatedRow)
                {
                    _lastEstimatedRow = DisplayData.LastScrollingSlot;
                    RowHeightEstimate = totalRowsHeight / (_lastEstimatedRow + 1 - _collapsedSlotsTable.GetIndexCount(0, _lastEstimatedRow));
                }

                // Calculate estimates for what's beyond the viewport
                if (VisibleSlotCount > DisplayData.NumDisplayedScrollingElements)
                {
                    int remainingRowCount = (SlotCount - DisplayData.LastScrollingSlot - _collapsedSlotsTable.GetIndexCount(DisplayData.LastScrollingSlot, SlotCount - 1) - 1);

                    // Add estimation for the cell heights of all rows beyond our viewport
                    totalRowsHeight += RowHeightEstimate * remainingRowCount;

                    // Add the rest of the details beyond the viewport
                    detailsCount += GetDetailsCountInclusive(DisplayData.LastScrollingSlot + 1, SlotCount - 1);
                }

                // 
                double totalDetailsHeight = detailsCount * RowDetailsHeightEstimate;

                return totalRowsHeight + totalDetailsHeight;
            }
        }

        /// <summary>
        /// Clears the entire selection. Displayed rows are deselected explicitly to visualize
        /// potential transition effects
        /// </summary>
        internal void ClearRowSelection(bool resetAnchorSlot)
        {
            if (resetAnchorSlot)
            {
                AnchorSlot = -1;
            }
            if (_selectedItems.Count > 0)
            {
                _noSelectionChangeCount++;
                try
                {
                    // Individually deselecting displayed rows to view potential transitions
                    for (int slot = DisplayData.FirstScrollingSlot;
                         slot > -1 && slot <= DisplayData.LastScrollingSlot;
                         slot++)
                    {
                        if (DisplayData.GetDisplayedElement(slot) is DataGridRow row)
                        {
                            if (_selectedItems.ContainsSlot(row.Slot))
                            {
                                SelectSlot(row.Slot, false);
                            }
                        }
                    }
                    _selectedItems.ClearRows();
                    SelectionHasChanged = true;
                }
                finally
                {
                    NoSelectionChangeCount--;
                }
            }
        }

        /// <summary>
        /// Clears the entire selection except the indicated row. Displayed rows are deselected explicitly to 
        /// visualize potential transition effects. The row indicated is selected if it is not already.
        /// </summary>
        internal void ClearRowSelection(int slotException, bool setAnchorSlot)
        {
            _noSelectionChangeCount++;
            try
            {
                bool exceptionAlreadySelected = false;
                if (_selectedItems.Count > 0)
                {
                    // Individually deselecting displayed rows to view potential transitions
                    for (int slot = DisplayData.FirstScrollingSlot;
                         slot > -1 && slot <= DisplayData.LastScrollingSlot;
                         slot++)
                    {
                        if (slot != slotException && _selectedItems.ContainsSlot(slot))
                        {
                            SelectSlot(slot, false);
                            SelectionHasChanged = true;
                        }
                    }
                    exceptionAlreadySelected = _selectedItems.ContainsSlot(slotException);
                    int selectedCount = _selectedItems.Count;
                    if (selectedCount > 0)
                    {
                        if (selectedCount > 1)
                        {
                            SelectionHasChanged = true;
                        }
                        else
                        {
                            int currentlySelectedSlot = _selectedItems.GetIndexes().First();
                            if (currentlySelectedSlot != slotException)
                            {
                                SelectionHasChanged = true;
                            }
                        }
                        _selectedItems.ClearRows();
                    }
                }
                if (exceptionAlreadySelected)
                {
                    // Exception row was already selected. It just needs to be marked as selected again.
                    // No transition involved.
                    _selectedItems.SelectSlot(slotException, true /*select*/);
                    if (setAnchorSlot)
                    {
                        AnchorSlot = slotException;
                    }
                }
                else
                {
                    // Exception row was not selected. It needs to be selected with potential transition
                    SetRowSelection(slotException, true /*isSelected*/, setAnchorSlot);
                }
            }
            finally
            {
                NoSelectionChangeCount--;
            }
        }

        internal int GetCollapsedSlotCount(int startSlot, int endSlot)
        {
            return _collapsedSlotsTable.GetIndexCount(startSlot, endSlot);
        }

        internal int GetNextVisibleSlot(int slot)
        {
            return _collapsedSlotsTable.GetNextGap(slot);
        }

        internal int GetPreviousVisibleSlot(int slot)
        {
            return _collapsedSlotsTable.GetPreviousGap(slot);
        }

        /// <summary>
        /// Returns the row associated to the provided backend data item.
        /// </summary>
        /// <param name="dataItem">backend data item</param>
        /// <returns>null if the DataSource is null, the provided item in not in the source, or the item is not displayed; otherwise, the associated Row</returns>
        internal DataGridRow GetRowFromItem(object dataItem)
        {
            int rowIndex = DataConnection.IndexOf(dataItem);
            if (rowIndex < 0)
            {
                return null;
            }
            int slot = SlotFromRowIndex(rowIndex);
            return IsSlotVisible(slot) ? DisplayData.GetDisplayedElement(slot) as DataGridRow : null;
        }

        internal bool GetRowSelection(int slot)
        {
            Debug.Assert(slot != -1);
            return _selectedItems.ContainsSlot(slot);
        }

        internal void InsertElementAt(int slot, int rowIndex, object item, DataGridRowGroupInfo groupInfo, bool isCollapsed)
        {
            Debug.Assert(slot >= 0 && slot <= SlotCount);

            bool isRow = rowIndex != -1;
            if (isCollapsed)
            {
                InsertElement(slot, 
                    element: null, 
                    updateVerticalScrollBarOnly: true,
                    isCollapsed: true, 
                    isRow: isRow);
            }
            else if (SlotIsDisplayed(slot))
            {
                // Row at that index needs to be displayed
                if (isRow)
                {
                    InsertElement(slot, GenerateRow(rowIndex, slot, item), false /*updateVerticalScrollBarOnly*/, false /*isCollapsed*/, isRow);
                }
                else
                {
                    InsertElement(slot, GenerateRowGroupHeader(slot, groupInfo), 
                        updateVerticalScrollBarOnly: false,
                        isCollapsed: false, 
                        isRow: isRow);
                }
            }
            else
            {
                InsertElement(slot, 
                    element: null,
                    updateVerticalScrollBarOnly: _vScrollBar == null || _vScrollBar.IsVisible,
                    isCollapsed: false, 
                    isRow: isRow);
            }
        }

        internal void InsertRowAt(int rowIndex)
        {
            int slot = SlotFromRowIndex(rowIndex);
            object item = DataConnection.GetDataItem(rowIndex);

            // isCollapsed below is always false because we only use the method if we're not grouping
            InsertElementAt(slot, rowIndex, item, null/*DataGridRowGroupInfo*/, false /*isCollapsed*/);
        }

        internal bool IsColumnDisplayed(int columnIndex)
        {
            return columnIndex >= FirstDisplayedNonFillerColumnIndex && columnIndex <= DisplayData.LastTotallyDisplayedScrollingCol;
        }

        internal bool IsRowRecyclable(DataGridRow row)
        {
            return (row != EditingRow && row != _focusedRow);
        }

        internal bool IsSlotVisible(int slot)
        {
            return slot >= DisplayData.FirstScrollingSlot
               && slot <= DisplayData.LastScrollingSlot
               && slot != -1
               && !_collapsedSlotsTable.Contains(slot);
        }

        internal void OnRowsMeasure()
        {
            if (!MathUtilities.IsZero(DisplayData.PendingVerticalScrollHeight))
            {
                ScrollSlotsByHeight(DisplayData.PendingVerticalScrollHeight);
                DisplayData.PendingVerticalScrollHeight = 0;
            }
        }

        internal void RefreshRows(bool recycleRows, bool clearRows)
        {
            if (_measured)
            {
                // _desiredCurrentColumnIndex is used in MakeFirstDisplayedCellCurrentCell to set the
                // column position back to what it was before the refresh
                _desiredCurrentColumnIndex = CurrentColumnIndex;
                double verticalOffset = _verticalOffset;
                if (DisplayData.PendingVerticalScrollHeight > 0)
                {
                    // Use the pending vertical scrollbar position if there is one, in the case that the collection
                    // has been reset multiple times in a row.
                    verticalOffset = DisplayData.PendingVerticalScrollHeight;
                }
                _verticalOffset = 0;
                NegVerticalOffset = 0;

                if (clearRows)
                {
                    ClearRows(recycleRows);
                    ClearRowGroupHeadersTable();
                    PopulateRowGroupHeadersTable();
                }

                RefreshRowGroupHeaders();

                // Update the CurrentSlot because it might have changed
                if (recycleRows && DataConnection.CollectionView != null)
                {
                    CurrentSlot = DataConnection.CollectionView.CurrentPosition == -1
                        ? -1 : SlotFromRowIndex(DataConnection.CollectionView.CurrentPosition);
                    if (CurrentSlot == -1)
                    {
                        SetCurrentCellCore(-1, -1);
                    }
                }

                if (DataConnection != null && ColumnsItemsInternal.Count > 0)
                {
                    AddSlots(DataConnection.Count);
                    AddSlots(DataConnection.Count + RowGroupHeadersTable.IndexCount);

                    InvalidateMeasure();
                }

                EnsureRowGroupSpacerColumn();

                if (VerticalScrollBar != null)
                {
                    DisplayData.PendingVerticalScrollHeight = Math.Min(verticalOffset, VerticalScrollBar.Maximum);
                }
            }
            else
            {
                if (clearRows)
                {
                    ClearRows(recycleRows);
                }
                ClearRowGroupHeadersTable();
                PopulateRowGroupHeadersTable();
            }
        }

        internal void RemoveRowAt(int rowIndex, object item)
        {
            RemoveElementAt(SlotFromRowIndex(rowIndex), item, true);
        }

        internal int RowIndexFromSlot(int slot)
        {
            return slot - RowGroupHeadersTable.GetIndexCount(0, slot);
        }

        internal bool ScrollSlotIntoView(int slot, bool scrolledHorizontally)
        {
            Debug.Assert(_collapsedSlotsTable.Contains(slot) || !IsSlotOutOfBounds(slot));

            if (scrolledHorizontally && DisplayData.FirstScrollingSlot <= slot && DisplayData.LastScrollingSlot >= slot)
            {
                // If the slot is displayed and we scrolled horizontally, column virtualization could cause the rows to grow.
                // As a result we need to force measure on the rows we're displaying and recalculate our First and Last slots 
                // so they're accurate
                foreach (DataGridRow row in DisplayData.GetScrollingRows())
                {
                    row.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                UpdateDisplayedRows(DisplayData.FirstScrollingSlot, CellsHeight);
            }

            if (DisplayData.FirstScrollingSlot < slot && (DisplayData.LastScrollingSlot > slot || DisplayData.LastScrollingSlot == -1))
            {
                // The row is already displayed in its entirety
                return true;
            }
            else if (DisplayData.FirstScrollingSlot == slot && slot != -1)
            {
                if (!MathUtilities.IsZero(NegVerticalOffset))
                {
                    // First displayed row is partially scrolled of. Let's scroll it so that NegVerticalOffset becomes 0.
                    DisplayData.PendingVerticalScrollHeight = -NegVerticalOffset;
                    InvalidateRowsMeasure(false /*invalidateIndividualRows*/);
                }
                return true;
            }

            double deltaY = 0;
            int firstFullSlot;
            if (DisplayData.FirstScrollingSlot > slot)
            {
                // Scroll up to the new row so it becomes the first displayed row
                firstFullSlot = DisplayData.FirstScrollingSlot - 1;
                if (MathUtilities.GreaterThan(NegVerticalOffset, 0))
                {
                    deltaY = -NegVerticalOffset;
                }
                deltaY -= GetSlotElementsHeight(slot, firstFullSlot);
                if (DisplayData.FirstScrollingSlot - slot > 1)
                {
                    // 

                    ResetDisplayedRows();
                }
                NegVerticalOffset = 0;
                UpdateDisplayedRows(slot, CellsHeight);
            }
            else if (DisplayData.LastScrollingSlot <= slot)
            {
                // Scroll down to the new row so it's entirely displayed.  If the height of the row
                // is greater than the height of the DataGrid, then show the top of the row at the top
                // of the grid
                firstFullSlot = DisplayData.LastScrollingSlot;
                // Figure out how much of the last row is cut off
                double rowHeight = GetExactSlotElementHeight(DisplayData.LastScrollingSlot);
                double availableHeight = AvailableSlotElementRoom + rowHeight;
                if (MathUtilities.AreClose(rowHeight, availableHeight))
                {
                    if (DisplayData.LastScrollingSlot == slot)
                    {
                        // We're already at the very bottom so we don't need to scroll down further
                        return true;
                    }
                    else
                    {
                        // We're already showing the entire last row so don't count it as part of the delta
                        firstFullSlot++;
                    }
                }
                else if (rowHeight > availableHeight)
                {
                    firstFullSlot++;
                    deltaY += rowHeight - availableHeight;
                }
                // sum up the height of the rest of the full rows
                if (slot >= firstFullSlot)
                {
                    deltaY += GetSlotElementsHeight(firstFullSlot, slot);
                }
                // If the first row we're displaying is no longer adjacent to the rows we have
                // simply discard the ones we have
                if (slot - DisplayData.LastScrollingSlot > 1)
                {
                    ResetDisplayedRows();
                }
                if (MathUtilities.GreaterThanOrClose(GetExactSlotElementHeight(slot), CellsHeight))
                {
                    // The entire row won't fit in the DataGrid so we start showing it from the top
                    NegVerticalOffset = 0;
                    UpdateDisplayedRows(slot, CellsHeight);
                }
                else
                {
                    UpdateDisplayedRowsFromBottom(slot);
                }
            }

            _verticalOffset += deltaY;
            if (_verticalOffset < 0 || DisplayData.FirstScrollingSlot == 0)
            {
                // We scrolled too far because a row's height was larger than its approximation
                _verticalOffset = NegVerticalOffset;
            }

            // 
            Debug.Assert(MathUtilities.LessThanOrClose(NegVerticalOffset, _verticalOffset));

            SetVerticalOffset(_verticalOffset);

            InvalidateMeasure();
            InvalidateRowsMeasure(false /*invalidateIndividualRows*/);

            return true;
        }

        internal void SetRowSelection(int slot, bool isSelected, bool setAnchorSlot)
        {
            Debug.Assert(!(!isSelected && setAnchorSlot));
            Debug.Assert(!IsSlotOutOfSelectionBounds(slot));
            _noSelectionChangeCount++;
            try
            {
                if (SelectionMode == DataGridSelectionMode.Single && isSelected)
                {
                    Debug.Assert(_selectedItems.Count <= 1);
                    if (_selectedItems.Count > 0)
                    {
                        int currentlySelectedSlot = _selectedItems.GetIndexes().First();
                        if (currentlySelectedSlot != slot)
                        {
                            SelectSlot(currentlySelectedSlot, false);
                            SelectionHasChanged = true;
                        }
                    }
                }
                if (_selectedItems.ContainsSlot(slot) != isSelected)
                {
                    SelectSlot(slot, isSelected);
                    SelectionHasChanged = true;
                }
                if (setAnchorSlot)
                {
                    AnchorSlot = slot;
                }
            }
            finally
            {
                NoSelectionChangeCount--;
            }
        }

        // For now, all scenarios are for isSelected == true.
        internal void SetRowsSelection(int startSlot, int endSlot /*, bool isSelected*/)
        {
            Debug.Assert(startSlot >= 0 && startSlot < SlotCount);
            Debug.Assert(endSlot >= 0 && endSlot < SlotCount);
            Debug.Assert(startSlot <= endSlot);

            _noSelectionChangeCount++;
            try
            {
                if (/*isSelected &&*/ !_selectedItems.ContainsAll(startSlot, endSlot))
                {
                    // At least one row gets selected
                    SelectSlots(startSlot, endSlot, true);
                    SelectionHasChanged = true;
                }
            }
            finally
            {
                NoSelectionChangeCount--;
            }
        }

        internal int SlotFromRowIndex(int rowIndex)
        {
            return rowIndex + RowGroupHeadersTable.GetIndexCountBeforeGap(0, rowIndex);
        }

        private void AddSlotElement(int slot, Control element)
        {
#if DEBUG
            if (element is DataGridRow row)
            {
                Debug.Assert(row.OwningGrid == this);
                Debug.Assert(row.Cells.Count == ColumnsItemsInternal.Count);

                int columnIndex = 0;
                foreach (DataGridCell dataGridCell in row.Cells)
                {
                    Debug.Assert(dataGridCell.OwningRow == row);
                    Debug.Assert(dataGridCell.OwningColumn == ColumnsItemsInternal[columnIndex]);
                    columnIndex++;
                }
            }
#endif
            Debug.Assert(slot == SlotCount);

            OnAddedElement_Phase1(slot, element);
            SlotCount++;
            VisibleSlotCount++;
            OnAddedElement_Phase2(slot, updateVerticalScrollBarOnly: false);
            OnElementsChanged(grew: true);
        }

        private void AddSlots(int totalSlots)
        {
            SlotCount = 0;
            VisibleSlotCount = 0;
            IEnumerator<int> groupSlots = null;
            int nextGroupSlot = -1;
            if (RowGroupHeadersTable.RangeCount > 0)
            {
                groupSlots = RowGroupHeadersTable.GetIndexes().GetEnumerator();
                if (groupSlots != null && groupSlots.MoveNext())
                {
                    nextGroupSlot = groupSlots.Current;
                }
            }
            int slot = 0;
            int addedRows = 0;
            while (slot < totalSlots && AvailableSlotElementRoom > 0)
            {
                if (slot == nextGroupSlot)
                {
                    DataGridRowGroupInfo groupRowInfo = RowGroupHeadersTable.GetValueAt(slot);
                    AddSlotElement(slot, GenerateRowGroupHeader(slot, groupRowInfo));
                    nextGroupSlot = groupSlots.MoveNext() ? groupSlots.Current : -1;
                }
                else
                {
                    AddSlotElement(slot, GenerateRow(addedRows, slot));
                    addedRows++;
                }
                slot++;
            }

            if (slot < totalSlots)
            {
                SlotCount += totalSlots - slot;
                VisibleSlotCount += totalSlots - slot;
                OnAddedElement_Phase2(0,
                    updateVerticalScrollBarOnly: _vScrollBar == null || _vScrollBar.IsVisible);
                OnElementsChanged(grew: true);
            }
        }

        private void ApplyDisplayedRowsState(int startSlot, int endSlot)
        {
            int firstSlot = Math.Max(DisplayData.FirstScrollingSlot, startSlot);
            int lastSlot = Math.Min(DisplayData.LastScrollingSlot, endSlot);

            if (firstSlot >= 0)
            {
                Debug.Assert(lastSlot >= firstSlot);
                int slot = GetNextVisibleSlot(firstSlot - 1);
                while (slot <= lastSlot)
                {
                    if (DisplayData.GetDisplayedElement(slot) is DataGridRow row)
                    {
                        row.UpdatePseudoClasses(); ;
                    }
                    slot = GetNextVisibleSlot(slot);
                }
            }
        }

        private void ClearRows(bool recycle)
        {
            // Need to clean up recycled rows even if the RowCount is 0
            SetCurrentCellCore(-1, -1, commitEdit: false, endRowEdit: false);
            ClearRowSelection(resetAnchorSlot: true);
            UnloadElements(recycle);

            _showDetailsTable.Clear();
            SlotCount = 0;
            NegVerticalOffset = 0;
            SetVerticalOffset(0);
            ComputeScrollBarsLayout();
        }

        // Updates _collapsedSlotsTable and returns the number of pixels that were collapsed
        private double CollapseSlotsInTable(int startSlot, int endSlot, ref int slotsExpanded, int lastDisplayedSlot, ref double heightChangeBelowLastDisplayedSlot)
        {
            int firstSlot = startSlot;
            int lastSlot;
            double totalHeightChange = 0;
            // Figure out which slots actually need to be expanded since some might already be collapsed
            while (firstSlot <= endSlot)
            {
                firstSlot = _collapsedSlotsTable.GetNextGap(firstSlot - 1);
                int nextCollapsedSlot = _collapsedSlotsTable.GetNextIndex(firstSlot) - 1;
                lastSlot = nextCollapsedSlot == -2 ? endSlot : Math.Min(endSlot, nextCollapsedSlot);

                if (firstSlot <= lastSlot)
                {
                    double heightChange = GetHeightEstimate(firstSlot, lastSlot);
                    totalHeightChange -= heightChange;
                    slotsExpanded -= lastSlot - firstSlot + 1;

                    if (lastSlot > lastDisplayedSlot)
                    {
                        if (firstSlot > lastDisplayedSlot)
                        {
                            heightChangeBelowLastDisplayedSlot -= heightChange;
                        }
                        else
                        {
                            heightChangeBelowLastDisplayedSlot -= GetHeightEstimate(lastDisplayedSlot + 1, lastSlot);
                        }
                    }

                    firstSlot = lastSlot + 1;
                }
            }

            // Update _collapsedSlotsTable in one bulk operation
            _collapsedSlotsTable.AddValues(startSlot, endSlot - startSlot + 1, false);

            return totalHeightChange;
        }

        private static void CorrectRowAfterDeletion(DataGridRow row, bool rowDeleted)
        {
            row.Slot--;
            if (rowDeleted)
            {
                row.Index--;
            }
        }

        private static void CorrectRowAfterInsertion(DataGridRow row, bool rowInserted)
        {
            row.Slot++;
            if (rowInserted)
            {
                row.Index++;
            }
        }

        /// <summary>
        /// Adjusts the index of all displayed, loaded and edited rows after a row was deleted.
        /// Removes the deleted row from the list of loaded rows if present.
        /// </summary>
        private void CorrectSlotsAfterDeletion(int slotDeleted, bool wasRow)
        {
            Debug.Assert(slotDeleted >= 0);

            // Take care of the non-visible loaded rows
            for (int index = 0; index < _loadedRows.Count;)
            {
                DataGridRow dataGridRow = _loadedRows[index];
                if (IsSlotVisible(dataGridRow.Slot))
                {
                    index++;
                }
                else
                {
                    if (dataGridRow.Slot > slotDeleted)
                    {
                        CorrectRowAfterDeletion(dataGridRow, wasRow);
                        index++;
                    }
                    else if (dataGridRow.Slot == slotDeleted)
                    {
                        _loadedRows.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            // Take care of the non-visible edited row
            if (EditingRow != null &&
                !IsSlotVisible(EditingRow.Slot) &&
                EditingRow.Slot > slotDeleted)
            {
                CorrectRowAfterDeletion(EditingRow, wasRow);
            }

            // Take care of the non-visible focused row
            if (_focusedRow != null &&
                _focusedRow != EditingRow &&
                !IsSlotVisible(_focusedRow.Slot) &&
                _focusedRow.Slot > slotDeleted)
            {
                CorrectRowAfterDeletion(_focusedRow, wasRow);
            }

            // Take care of the visible rows
            foreach (DataGridRow row in DisplayData.GetScrollingRows())
            {
                if (row.Slot > slotDeleted)
                {
                    CorrectRowAfterDeletion(row, wasRow);
                    row.EnsureBackground();
                }
            }

            // Update the RowGroupHeaders
            foreach (int slot in RowGroupHeadersTable.GetIndexes())
            {
                DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (rowGroupInfo.Slot > slotDeleted)
                {
                    rowGroupInfo.Slot--;
                }
                if (rowGroupInfo.LastSubItemSlot >= slotDeleted)
                {
                    rowGroupInfo.LastSubItemSlot--;
                }
            }

            // Update which row we've calculated the RowHeightEstimate up to
            if (_lastEstimatedRow >= slotDeleted)
            {
                _lastEstimatedRow--;
            }
        }

        /// <summary>
        /// Adjusts the index of all displayed, loaded and edited rows after rows were deleted.
        /// </summary>
        private void CorrectSlotsAfterInsertion(int slotInserted, bool isCollapsed, bool rowInserted)
        {
            Debug.Assert(slotInserted >= 0);

            // Take care of the non-visible loaded rows
            foreach (DataGridRow dataGridRow in _loadedRows)
            {
                if (!IsSlotVisible(dataGridRow.Slot) && dataGridRow.Slot >= slotInserted)
                {
                    DataGrid.CorrectRowAfterInsertion(dataGridRow, rowInserted);
                }
            }

            // Take care of the non-visible focused row
            if (_focusedRow != null &&
                _focusedRow != EditingRow &&
                !(IsSlotVisible(_focusedRow.Slot) || ((_focusedRow.Slot == slotInserted) && isCollapsed)) &&
                _focusedRow.Slot >= slotInserted)
            {
                DataGrid.CorrectRowAfterInsertion(_focusedRow, rowInserted);
            }

            // Take care of the visible rows
            foreach (DataGridRow row in DisplayData.GetScrollingRows())
            {
                if (row.Slot >= slotInserted)
                {
                    DataGrid.CorrectRowAfterInsertion(row, rowInserted);
                    row.EnsureBackground();
                }
            }

            // Re-calculate the EditingRow's Slot and Index and ensure that it is still selected.
            if (EditingRow != null)
            {
                EditingRow.Index = DataConnection.IndexOf(EditingRow.DataContext);
                EditingRow.Slot = SlotFromRowIndex(EditingRow.Index);
            }

            // Update the RowGroupHeaders
            foreach (int slot in RowGroupHeadersTable.GetIndexes(slotInserted))
            {
                DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (rowGroupInfo.Slot >= slotInserted)
                {
                    rowGroupInfo.Slot++;
                }

                // We are purposefully checking GT and not GTE because the equality case is handled
                // by the CorrectLastSubItemSlotsAfterInsertion method
                if (rowGroupInfo.LastSubItemSlot > slotInserted)
                {
                    rowGroupInfo.LastSubItemSlot++;
                }
            }

            // Update which row we've calculated the RowHeightEstimate up to
            if (_lastEstimatedRow >= slotInserted)
            {
                _lastEstimatedRow++;
            }
        }

        private IEnumerable<DataGridRow> GetAllRows()
        {
            if (_rowsPresenter != null)
            {
                foreach (Control element in _rowsPresenter.Children)
                {
                    if (element is DataGridRow row)
                    {
                        yield return row;
                    }
                }
            }
        }

        // Expands slots from startSlot to endSlot inclusive and adds the amount expanded in this suboperation to
        // the given totalHeightChanged of the entire operation
        private void ExpandSlots(int startSlot, int endSlot, bool isDisplayed, ref int slotsExpanded, ref double totalHeightChange)
        {
            double heightAboveStartSlot = 0;
            if (isDisplayed)
            {
                int slot = DisplayData.FirstScrollingSlot;
                while (slot < startSlot)
                {
                    heightAboveStartSlot += GetExactSlotElementHeight(slot);
                    slot = GetNextVisibleSlot(slot);
                }

                // First make the bottom rows available for recycling so we minimize element creation when expanding
                for (int i = 0; (i < endSlot - startSlot + 1) && (DisplayData.LastScrollingSlot > endSlot); i++)
                {
                    RemoveDisplayedElement(DisplayData.LastScrollingSlot, wasDeleted: false, updateSlotInformation: true);
                }
            }

            // Figure out which slots actually need to be expanded since some might already be collapsed
            double currentHeightChange = 0;
            int firstSlot = startSlot;
            int lastSlot = endSlot;
            while (firstSlot <= endSlot)
            {
                firstSlot = _collapsedSlotsTable.GetNextIndex(firstSlot - 1);
                if (firstSlot == -1)
                {
                    break;
                }
                lastSlot = Math.Min(endSlot, _collapsedSlotsTable.GetNextGap(firstSlot) - 1);

                if (firstSlot <= lastSlot)
                {
                    if (!isDisplayed)
                    {
                        // Estimate the height change if the slots aren't displayed.  If they are displayed, we can add real values
                        double rowCount = lastSlot - firstSlot - GetRowGroupHeaderCount(firstSlot, lastSlot, false, out double headerHeight) + 1;
                        double detailsCount = GetDetailsCountInclusive(firstSlot, lastSlot);
                        currentHeightChange += headerHeight + (detailsCount * RowDetailsHeightEstimate) + (rowCount * RowHeightEstimate);
                    }
                    slotsExpanded += lastSlot - firstSlot + 1;
                    firstSlot = lastSlot + 1;
                }
            }

            // Update _collapsedSlotsTable in one bulk operation
            _collapsedSlotsTable.RemoveValues(startSlot, endSlot - startSlot + 1);

            if (isDisplayed)
            {
                double availableHeight = CellsHeight - heightAboveStartSlot;
                // Actually expand the displayed slots up to what we can display
                for (int i = startSlot; (i <= endSlot) && (currentHeightChange < availableHeight); i++)
                {
                    Control insertedElement = InsertDisplayedElement(i, updateSlotInformation: false);
                    currentHeightChange += insertedElement.DesiredSize.Height;
                    if (i > DisplayData.LastScrollingSlot)
                    {
                        DisplayData.LastScrollingSlot = i;
                    }
                }
            }

            // Update the total height for the entire Expand operation
            totalHeightChange += currentHeightChange;
        }

        /// <summary>
        /// Creates all the editing elements for the current editing row, so the bindings
        /// all exist during validation.
        /// </summary>
        private void GenerateEditingElements()
        {
            if (EditingRow != null && EditingRow.Cells != null)
            {
                Debug.Assert(EditingRow.Cells.Count == ColumnsItemsInternal.Count);
                foreach (DataGridColumn column in ColumnsInternal.GetDisplayedColumns(c => c.IsVisible && !c.IsReadOnly))
                {
                    column.GenerateEditingElementInternal(EditingRow.Cells[column.Index], EditingRow.DataContext);
                }
            }
        }

        /// <summary>
        /// Returns a row for the provided index. The row gets first loaded through the LoadingRow event.
        /// </summary>
        private DataGridRow GenerateRow(int rowIndex, int slot)
        {
            return GenerateRow(rowIndex, slot, DataConnection.GetDataItem(rowIndex));
        }

        /// <summary>
        /// Returns a row for the provided index. The row gets first loaded through the LoadingRow event.
        /// </summary>
        private DataGridRow GenerateRow(int rowIndex, int slot, object dataContext)
        {
            Debug.Assert(rowIndex > -1);
            DataGridRow dataGridRow = GetGeneratedRow(dataContext);
            if (dataGridRow == null)
            {
                dataGridRow = DisplayData.GetUsedRow() ?? new DataGridRow();
                dataGridRow.Index = rowIndex;
                dataGridRow.Slot = slot;
                dataGridRow.OwningGrid = this;
                dataGridRow.DataContext = dataContext;
                CompleteCellsCollection(dataGridRow);

                OnLoadingRow(new DataGridRowEventArgs(dataGridRow));
            }
            return dataGridRow;
        }

        /// <summary>
        /// Returns the exact row height, whether it is currently displayed or not.
        /// The row is generated and added to the displayed rows in case it is not already displayed.
        /// The horizontal gridlines thickness are added.
        /// </summary>
        private double GetExactSlotElementHeight(int slot)
        {
            Debug.Assert((slot >= 0) && slot < SlotCount);

            if (IsSlotVisible(slot))
            {
                Debug.Assert(DisplayData.GetDisplayedElement(slot) != null);
                return DisplayData.GetDisplayedElement(slot).DesiredSize.Height;
            }

            Control slotElement = InsertDisplayedElement(slot, true /*updateSlotInformation*/);
            Debug.Assert(slotElement != null);
            return slotElement.DesiredSize.Height;
        }

        // Returns an estimate for the height of the slots between fromSlot and toSlot
        private double GetHeightEstimate(int fromSlot, int toSlot)
        {
            double rowCount = toSlot - fromSlot - GetRowGroupHeaderCount(fromSlot, toSlot, true, out double headerHeight) + 1;
            double detailsCount = GetDetailsCountInclusive(fromSlot, toSlot);

            return headerHeight + (detailsCount * RowDetailsHeightEstimate) + (rowCount * RowHeightEstimate);
        }

        /// <summary>
        /// If the provided slot is displayed, returns the exact height.
        /// If the slot is not displayed, returns a default height.
        /// </summary>
        private double GetSlotElementHeight(int slot)
        {
            Debug.Assert(slot >= 0 && slot < SlotCount);
            if (IsSlotVisible(slot))
            {
                Debug.Assert(DisplayData.GetDisplayedElement(slot) != null);
                return DisplayData.GetDisplayedElement(slot).DesiredSize.Height;
            }
            else
            {
                DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (rowGroupInfo != null)
                {
                    return _rowGroupHeightsByLevel[rowGroupInfo.Level];
                }

                // Assume it's a row since we're either not grouping or it wasn't a RowGroupHeader
                return RowHeightEstimate + (GetRowDetailsVisibility(slot) ? RowDetailsHeightEstimate : 0);
            }
        }

        /// <summary>
        /// Cumulates the approximate height of the rows from fromRowIndex to toRowIndex included.
        /// Including the potential gridline thickness.
        /// </summary>
        private double GetSlotElementsHeight(int fromSlot, int toSlot)
        {
            Debug.Assert(toSlot >= fromSlot);

            double height = 0;
            for (int slot = fromSlot; slot <= toSlot; slot++)
            {
                height += GetSlotElementHeight(slot);
            }
            return height;
        }

        /// <summary>
        /// Checks if the row for the provided dataContext has been generated and is present
        /// in either the loaded rows, pre-fetched rows, or editing row. 
        /// The displayed rows are *not* searched. Returns null if the row does not belong to those 3 categories.
        /// </summary>
        private DataGridRow GetGeneratedRow(object dataContext)
        {
            // Check the list of rows being loaded via the LoadingRow event.
            DataGridRow dataGridRow = GetLoadedRow(dataContext);
            if (dataGridRow != null)
            {
                return dataGridRow;
            }

            // Check the potential editing row.
            if (EditingRow != null && dataContext == EditingRow.DataContext)
            {
                return EditingRow;
            }

            // Check the potential focused row.
            if (_focusedRow != null && dataContext == _focusedRow.DataContext)
            {
                return _focusedRow;
            }

            return null;
        }

        private DataGridRow GetLoadedRow(object dataContext)
        {
            foreach (DataGridRow dataGridRow in _loadedRows)
            {
                if (dataGridRow.DataContext == dataContext)
                {
                    return dataGridRow;
                }
            }
            return null;
        }

        private Control InsertDisplayedElement(int slot, bool updateSlotInformation)
        {
            Control slotElement;
            if (RowGroupHeadersTable.Contains(slot))
            {
                slotElement = GenerateRowGroupHeader(slot, rowGroupInfo: RowGroupHeadersTable.GetValueAt(slot));
            }
            else
            {
                // If we're grouping, the GroupLevel needs to be fixed later by methods calling this 
                // which end up inserting rows. We don't do it here because elements could be inserted 
                // from top to bottom or bottom to up so it's better to do in one pass
                slotElement = GenerateRow(RowIndexFromSlot(slot), slot);
            }
            InsertDisplayedElement(slot, slotElement, wasNewlyAdded: false, updateSlotInformation: updateSlotInformation);
            return slotElement;
        }

        //TODO Styles
        private void InsertDisplayedElement(int slot, Control element, bool wasNewlyAdded, bool updateSlotInformation)
        {
            // We can only support creating new rows that are adjacent to the currently visible rows
            // since they need to be added to the visual tree for us to Measure them.
            Debug.Assert(DisplayData.FirstScrollingSlot == -1 || slot >= GetPreviousVisibleSlot(DisplayData.FirstScrollingSlot) && slot <= GetNextVisibleSlot(DisplayData.LastScrollingSlot));
            Debug.Assert(element != null);

            if (_rowsPresenter != null)
            {
                DataGridRowGroupHeader groupHeader = null;
                DataGridRow row = element as DataGridRow;
                if (row != null)
                {
                    LoadRowVisualsForDisplay(row);

                    if (IsRowRecyclable(row))
                    {
                        if (!row.IsRecycled)
                        {
                            Debug.Assert(!_rowsPresenter.Children.Contains(element));
                            _rowsPresenter.Children.Add(row);
                        }
                    }
                    else
                    {
                        element.Clip = null;
                        Debug.Assert(row.Index == RowIndexFromSlot(slot));
                    }
                }
                else
                {
                    groupHeader = element as DataGridRowGroupHeader;
                    Debug.Assert(groupHeader != null);  // Nothing other and Rows and RowGroups now
                    if (groupHeader != null)
                    {
                        groupHeader.TotalIndent = (groupHeader.Level == 0) ? 0 : RowGroupSublevelIndents[groupHeader.Level - 1];
                        if (!groupHeader.IsRecycled)
                        {
                            _rowsPresenter.Children.Add(element);
                        }
                        groupHeader.LoadVisualsForDisplay();
                    }
                }

                // Measure the element and update AvailableRowRoom
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                AvailableSlotElementRoom -= element.DesiredSize.Height;

                if (groupHeader != null)
                {
                    _rowGroupHeightsByLevel[groupHeader.Level] = groupHeader.DesiredSize.Height;
                }

                if (row != null && RowHeightEstimate == DataGrid.DATAGRID_defaultRowHeight && double.IsNaN(row.Height))
                {
                    RowHeightEstimate = element.DesiredSize.Height;
                }
            }

            if (wasNewlyAdded)
            {
                DisplayData.CorrectSlotsAfterInsertion(slot, element, isCollapsed: false);
            }
            else
            {
                DisplayData.LoadScrollingSlot(slot, element, updateSlotInformation);
            }
        }

        private void InsertElement(int slot, Control element, bool updateVerticalScrollBarOnly, bool isCollapsed, bool isRow)
        {
            Debug.Assert(slot >= 0 && slot <= SlotCount);

            OnInsertingElement(slot, true /*firstInsertion*/, isCollapsed);   // will throw an exception if the insertion is illegal

            OnInsertedElement_Phase1(slot, element, isCollapsed, isRow);
            SlotCount++;
            if (!isCollapsed)
            {
                VisibleSlotCount++;
            }
            OnInsertedElement_Phase2(slot, updateVerticalScrollBarOnly, isCollapsed);
        }

        private void InvalidateRowHeightEstimate()
        {
            // Start from scratch and assume that we haven't estimated any rows
            _lastEstimatedRow = -1;
        }

        private void OnAddedElement_Phase1(int slot, Control element)
        {
            Debug.Assert(slot >= 0);

            // Row needs to be potentially added to the displayed rows
            if (SlotIsDisplayed(slot))
            {
                InsertDisplayedElement(slot, element, true /*wasNewlyAdded*/, true);
            }
        }

        private void OnAddedElement_Phase2(int slot, bool updateVerticalScrollBarOnly)
        {
            if (slot < DisplayData.FirstScrollingSlot - 1)
            {
                // The element was added above our viewport so it pushes the VerticalOffset down
                double elementHeight = RowGroupHeadersTable.Contains(slot) ? RowGroupHeaderHeightEstimate : RowHeightEstimate;

                SetVerticalOffset(_verticalOffset + elementHeight);
            }
            if (updateVerticalScrollBarOnly)
            {
                UpdateVerticalScrollBar();
            }
            else
            {
                ComputeScrollBarsLayout();
                // Reposition rows in case we use a recycled one
                InvalidateRowsArrange();
            }
        }

        private void OnElementsChanged(bool grew)
        {
            if (grew &&
                ColumnsItemsInternal.Count > 0 &&
                CurrentColumnIndex == -1)
            {
                MakeFirstDisplayedCellCurrentCell();
            }
        }

        private void OnInsertedElement_Phase1(int slot, Control element, bool isCollapsed, bool isRow)
        {
            Debug.Assert(slot >= 0);

            // Fix the Index of all following rows
            CorrectSlotsAfterInsertion(slot, isCollapsed, isRow);

            // Next, same effect as adding a row
            if (element != null)
            {
#if DEBUG
                if (element is DataGridRow dataGridRow)
                {
                    Debug.Assert(dataGridRow.Cells.Count == ColumnsItemsInternal.Count);

                    int columnIndex = 0;
                    foreach (DataGridCell dataGridCell in dataGridRow.Cells)
                    {
                        Debug.Assert(dataGridCell.OwningRow == dataGridRow);
                        Debug.Assert(dataGridCell.OwningColumn == ColumnsItemsInternal[columnIndex]);
                        columnIndex++;
                    }
                }
#endif
                Debug.Assert(!isCollapsed);
                OnAddedElement_Phase1(slot, element);
            }
            else if ((slot <= DisplayData.FirstScrollingSlot) || (isCollapsed && (slot <= DisplayData.LastScrollingSlot)))
            {
                DisplayData.CorrectSlotsAfterInsertion(slot, null /*row*/, isCollapsed);
            }
        }

        private void OnInsertedElement_Phase2(int slot, bool updateVerticalScrollBarOnly, bool isCollapsed)
        {
            Debug.Assert(slot >= 0);

            if (!isCollapsed)
            {
                // Same effect as adding a row
                OnAddedElement_Phase2(slot, updateVerticalScrollBarOnly);
            }
        }

        private void OnInsertingElement(int slotInserted,
                                    bool firstInsertion,
                                    bool isCollapsed)
        {
            // Reset the current cell's address if it's after the inserted row.
            if (firstInsertion)
            {
                if (CurrentSlot != -1 && slotInserted <= CurrentSlot)
                {
                    // The underlying data was already added, therefore we need to avoid accessing any back-end data since we might be off by 1 row.
                    _temporarilyResetCurrentCell = true;
                    bool success = SetCurrentCellCore(-1, -1);
                    Debug.Assert(success);
                }
            }

            _showDetailsTable.InsertIndex(slotInserted);
            // Update the slot ranges for the RowGroupHeaders before updating the _selectedItems table,
            // because it's dependent on the slots being correct with regards to grouping.
            RowGroupHeadersTable.InsertIndex(slotInserted);
            _selectedItems.InsertIndex(slotInserted);

            if (isCollapsed)
            {
                _collapsedSlotsTable.InsertIndexAndValue(slotInserted, false);
            }
            else
            {
                _collapsedSlotsTable.InsertIndex(slotInserted);
            }

            // If we've inserted rows before the current selected item, update its index
            if (slotInserted <= SelectedIndex)
            {
                SetValueNoCallback(SelectedIndexProperty, SelectedIndex + 1);
            }
        }

        private void OnRemovedElement(int slotDeleted, object itemDeleted)
        {
            SlotCount--;
            bool wasCollapsed = _collapsedSlotsTable.Contains(slotDeleted);
            if (!wasCollapsed)
            {
                VisibleSlotCount--;
            }

            // If we're deleting the focused row, we need to clear the cached value
            if (_focusedRow != null && _focusedRow.Slot == slotDeleted)
            {
                ResetFocusedRow();
            }

            // The element needs to be potentially removed from the displayed elements
            Control elementDeleted = null;
            if (slotDeleted <= DisplayData.LastScrollingSlot)
            {
                if ((slotDeleted >= DisplayData.FirstScrollingSlot) && !wasCollapsed)
                {
                    elementDeleted = DisplayData.GetDisplayedElement(slotDeleted);
                    // We need to retrieve the Element before updating the tables, but we need
                    // to update the tables before updating DisplayData in RemoveDisplayedElement
                    UpdateTablesForRemoval(slotDeleted, itemDeleted);

                    // Displayed row is removed
                    RemoveDisplayedElement(elementDeleted, slotDeleted, true /*wasDeleted*/, true /*updateSlotInformation*/);
                }
                else
                {
                    UpdateTablesForRemoval(slotDeleted, itemDeleted);

                    // Removed row is not in view, just update the DisplayData
                    DisplayData.CorrectSlotsAfterDeletion(slotDeleted, wasCollapsed);
                }
            }
            else
            {
                // The element was removed beyond the viewport so we just need to update the tables
                UpdateTablesForRemoval(slotDeleted, itemDeleted);
            }

            // If a row was removed before the currently selected row, update its index
            if (slotDeleted < SelectedIndex)
            {
                SetValueNoCallback(SelectedIndexProperty, SelectedIndex - 1);
            }

            if (!wasCollapsed)
            {
                if (slotDeleted >= DisplayData.LastScrollingSlot && elementDeleted == null)
                {
                    // Deleted Row is below our Viewport, we just need to adjust the scrollbar
                    UpdateVerticalScrollBar();
                }
                else
                {
                    if (elementDeleted != null)
                    {
                        // Deleted Row is within our Viewport, update the AvailableRowRoom
                        AvailableSlotElementRoom += elementDeleted.DesiredSize.Height;
                    }
                    else
                    {
                        // Deleted Row is above our Viewport, update the vertical offset
                        SetVerticalOffset(Math.Max(0, _verticalOffset - RowHeightEstimate));
                    }

                    ComputeScrollBarsLayout();
                    // Reposition rows in case we use a recycled one
                    InvalidateRowsArrange();
                }
            }
        }

        private void OnRemovingElement(int slotDeleted)
        {
            // Note that the row needs to be deleted no matter what. The underlying data row was already deleted.

            Debug.Assert(slotDeleted >= 0 && slotDeleted < SlotCount);
            _temporarilyResetCurrentCell = false;

            // Reset the current cell's address if it's on the deleted row, or after it.
            if (CurrentSlot != -1 && slotDeleted <= CurrentSlot)
            {
                _desiredCurrentColumnIndex = CurrentColumnIndex;
                if (slotDeleted == CurrentSlot)
                {
                    // No editing is committed since the underlying entity was already deleted.
                    bool success = SetCurrentCellCore(-1, -1, false /*commitEdit*/, false /*endRowEdit*/);
                    Debug.Assert(success);
                }
                else
                {
                    // Underlying data of deleted row is gone. It cannot be accessed anymore. Skip the commit of the editing.
                    _temporarilyResetCurrentCell = true;
                    bool success = SetCurrentCellCore(-1, -1);
                    Debug.Assert(success);
                }
            }
        }

        //TODO Styles
        // Makes sure the row shows the proper visuals for selection, currency, details, etc.
        private void LoadRowVisualsForDisplay(DataGridRow row)
        {
            // If the row has been recycled, reapply the BackgroundBrush
            if (row.IsRecycled)
            {
                row.EnsureBackground();
                row.ApplyCellsState();
            }
            else if (row == EditingRow)
            {
                row.ApplyCellsState();
            }

            // Set the Row's Style if we one's defined at the DataGrid level and the user didn't
            // set one at the row level
            //EnsureElementStyle(row, null, RowStyle);
            row.EnsureHeaderStyleAndVisibility(null);

            // Check to see if the row contains the CurrentCell, apply its state.
            if (CurrentColumnIndex != -1 &&
                CurrentSlot != -1 &&
                row.Index == CurrentSlot)
            {
                row.Cells[CurrentColumnIndex].UpdatePseudoClasses();
            }

            if (row.IsSelected || row.IsRecycled)
            {
                row.UpdatePseudoClasses();
            }

            // Show or hide RowDetails based on DataGrid settings
            EnsureRowDetailsVisibility(row, raiseNotification: false, animate: false);
        }

        private void RemoveDisplayedElement(int slot, bool wasDeleted, bool updateSlotInformation)
        {
            Debug.Assert(slot >= DisplayData.FirstScrollingSlot &&
                         slot <= DisplayData.LastScrollingSlot);

            RemoveDisplayedElement(DisplayData.GetDisplayedElement(slot), slot, wasDeleted, updateSlotInformation);
        }

        // Removes an element from display either because it was deleted or it was scrolled out of view.
        // If the element was provided, it will be the element removed; otherwise, the element will be
        // retrieved from the slot information
        private void RemoveDisplayedElement(Control element, int slot, bool wasDeleted, bool updateSlotInformation)
        {
            if (element is DataGridRow dataGridRow)
            {
                if (IsRowRecyclable(dataGridRow))
                {
                    UnloadRow(dataGridRow);
                }
                else
                {
                    dataGridRow.Clip = new RectangleGeometry();
                }
            }
            else if (element is DataGridRowGroupHeader groupHeader)
            {
                OnUnloadingRowGroup(new DataGridRowGroupHeaderEventArgs(groupHeader));
                DisplayData.AddRecylableRowGroupHeader(groupHeader);
            }
            else if (_rowsPresenter != null)
            {
                _rowsPresenter.Children.Remove(element);
            }

            // Update DisplayData
            if (wasDeleted)
            {
                DisplayData.CorrectSlotsAfterDeletion(slot, wasCollapsed: false);
            }
            else
            {
                DisplayData.UnloadScrollingElement(slot, updateSlotInformation, wasDeleted: false);
            }
        }

        /// <summary>
        /// Removes all of the editing elements for the row that is just leaving editing mode.
        /// </summary>
        private void RemoveEditingElements()
        {
            if (EditingRow != null && EditingRow.Cells != null)
            {
                Debug.Assert(EditingRow.Cells.Count == ColumnsItemsInternal.Count);
                foreach (DataGridColumn column in Columns)
                {
                    column.RemoveEditingElement();
                }
            }
        }

        private void RemoveElementAt(int slot, object item, bool isRow)
        {
            Debug.Assert(slot >= 0 && slot < SlotCount);

            OnRemovingElement(slot);

            CorrectSlotsAfterDeletion(slot, isRow);

            OnRemovedElement(slot, item);
        }

        private void RemoveNonDisplayedRows(int newFirstDisplayedSlot, int newLastDisplayedSlot)
        {
            while (DisplayData.FirstScrollingSlot < newFirstDisplayedSlot)
            {
                // Need to add rows above the lastDisplayedScrollingRow
                RemoveDisplayedElement(DisplayData.FirstScrollingSlot, false /*wasDeleted*/, true /*updateSlotInformation*/);
            }
            while (DisplayData.LastScrollingSlot > newLastDisplayedSlot)
            {
                // Need to remove rows below the lastDisplayedScrollingRow
                RemoveDisplayedElement(DisplayData.LastScrollingSlot, false /*wasDeleted*/, true /*updateSlotInformation*/);
            }
        }

        private void ResetDisplayedRows()
        {
            if (UnloadingRow != null || UnloadingRowGroup != null)
            {
                foreach (Control element in DisplayData.GetScrollingElements())
                {
                    // Raise Unloading Row for all the rows we're displaying
                    if (element is DataGridRow row)
                    {
                        if (IsRowRecyclable(row))
                        {
                            OnUnloadingRow(new DataGridRowEventArgs(row));
                        }
                    }
                    // Raise Unloading Row for all the RowGroupHeaders we're displaying
                    else if (element is DataGridRowGroupHeader groupHeader)
                    {
                        OnUnloadingRowGroup(new DataGridRowGroupHeaderEventArgs(groupHeader));
                    }
                }
            }

            DisplayData.ClearElements(recycle: true);
            AvailableSlotElementRoom = CellsHeight;
        }

        /// <summary>
        /// Determines whether the row at the provided index must be displayed or not.
        /// </summary>
        private bool SlotIsDisplayed(int slot)
        {
            Debug.Assert(slot >= 0);

            if (slot >= DisplayData.FirstScrollingSlot &&
                slot <= DisplayData.LastScrollingSlot)
            {
                // Additional row takes the spot of a displayed row - it is necessarily displayed
                return true;
            }
            else if (DisplayData.FirstScrollingSlot == -1 &&
                     CellsHeight > 0 &&
                     CellsWidth > 0)
            {
                return true;
            }
            else if (slot == GetNextVisibleSlot(DisplayData.LastScrollingSlot))
            {
                if (AvailableSlotElementRoom > 0)
                {
                    // There is room for this additional row
                    return true;
                }
            }
            return false;
        }

        // Updates display information and displayed rows after scrolling the given number of pixels
        private void ScrollSlotsByHeight(double height)
        {
            Debug.Assert(DisplayData.FirstScrollingSlot >= 0);
            Debug.Assert(!MathUtilities.IsZero(height));

            _scrollingByHeight = true;
            try
            {
                double deltaY = 0;
                int newFirstScrollingSlot = DisplayData.FirstScrollingSlot;
                double newVerticalOffset = _verticalOffset + height;
                if (height > 0)
                {
                    // Scrolling Down
                    int lastVisibleSlot = GetPreviousVisibleSlot(SlotCount);
                    if (_vScrollBar != null && MathUtilities.AreClose(_vScrollBar.Maximum, newVerticalOffset))
                    {
                        // We've scrolled to the bottom of the ScrollBar, automatically place the user at the very bottom
                        // of the DataGrid.  If this produces very odd behavior, evaluate the coping strategy used by
                        // OnRowMeasure(Size).  For most data, this should be unnoticeable.
                        ResetDisplayedRows();
                        UpdateDisplayedRowsFromBottom(lastVisibleSlot);
                        newFirstScrollingSlot = DisplayData.FirstScrollingSlot;
                    }
                    else
                    {
                        deltaY = GetSlotElementHeight(newFirstScrollingSlot) - NegVerticalOffset;
                        if (MathUtilities.LessThan(height, deltaY))
                        {
                            // We've merely covered up more of the same row we're on
                            NegVerticalOffset += height;
                        }
                        else
                        {
                            // Figure out what row we've scrolled down to and update the value for NegVerticalOffset
                            NegVerticalOffset = 0;
                            // 
                            if (height > 2 * CellsHeight &&
                                (RowDetailsVisibilityMode != DataGridRowDetailsVisibilityMode.VisibleWhenSelected || RowDetailsTemplate == null))
                            {
                                // Very large scroll occurred. Instead of determining the exact number of scrolled off rows,
                                // let's estimate the number based on RowHeight.
                                ResetDisplayedRows();
                                double singleRowHeightEstimate = RowHeightEstimate + (RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Visible ? RowDetailsHeightEstimate : 0);
                                int scrolledToSlot = newFirstScrollingSlot + (int)(height / singleRowHeightEstimate);
                                scrolledToSlot += _collapsedSlotsTable.GetIndexCount(newFirstScrollingSlot, newFirstScrollingSlot + scrolledToSlot);
                                newFirstScrollingSlot = Math.Min(GetNextVisibleSlot(scrolledToSlot), lastVisibleSlot);
                            }
                            else
                            {
                                while (MathUtilities.LessThanOrClose(deltaY, height))
                                {
                                    if (newFirstScrollingSlot < lastVisibleSlot)
                                    {
                                        if (IsSlotVisible(newFirstScrollingSlot))
                                        {
                                            // Make the top row available for reuse
                                            RemoveDisplayedElement(newFirstScrollingSlot, false /*wasDeleted*/, true /*updateSlotInformation*/);
                                        }
                                        newFirstScrollingSlot = GetNextVisibleSlot(newFirstScrollingSlot);
                                    }
                                    else
                                    {
                                        // We're being told to scroll beyond the last row, ignore the extra
                                        NegVerticalOffset = 0;
                                        break;
                                    }

                                    double rowHeight = GetExactSlotElementHeight(newFirstScrollingSlot);
                                    double remainingHeight = height - deltaY;
                                    if (MathUtilities.LessThanOrClose(rowHeight, remainingHeight))
                                    {
                                        deltaY += rowHeight;
                                    }
                                    else
                                    {
                                        NegVerticalOffset = remainingHeight;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Scrolling Up
                    if (MathUtilities.GreaterThanOrClose(height + NegVerticalOffset, 0))
                    {
                        // We've merely exposing more of the row we're on
                        NegVerticalOffset += height;
                    }
                    else
                    {
                        // Figure out what row we've scrolled up to and update the value for NegVerticalOffset
                        deltaY = -NegVerticalOffset;
                        NegVerticalOffset = 0;
                        // 

                        if (height < -2 * CellsHeight &&
                            (RowDetailsVisibilityMode != DataGridRowDetailsVisibilityMode.VisibleWhenSelected || RowDetailsTemplate == null))
                        {
                            // Very large scroll occurred. Instead of determining the exact number of scrolled off rows,
                            // let's estimate the number based on RowHeight.
                            if (newVerticalOffset == 0)
                            {
                                newFirstScrollingSlot = 0;
                            }
                            else
                            {
                                double singleRowHeightEstimate = RowHeightEstimate + (RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Visible ? RowDetailsHeightEstimate : 0);
                                int scrolledToSlot = newFirstScrollingSlot + (int)(height / singleRowHeightEstimate);
                                scrolledToSlot -= _collapsedSlotsTable.GetIndexCount(scrolledToSlot, newFirstScrollingSlot);

                                newFirstScrollingSlot = Math.Max(0, GetPreviousVisibleSlot(scrolledToSlot + 1));
                            }
                            ResetDisplayedRows();
                        }
                        else
                        {
                            int lastScrollingSlot = DisplayData.LastScrollingSlot;
                            while (MathUtilities.GreaterThan(deltaY, height))
                            {
                                if (newFirstScrollingSlot > 0)
                                {
                                    if (IsSlotVisible(lastScrollingSlot))
                                    {
                                        // Make the bottom row available for reuse
                                        RemoveDisplayedElement(lastScrollingSlot, wasDeleted: false, updateSlotInformation: true);
                                        lastScrollingSlot = GetPreviousVisibleSlot(lastScrollingSlot);
                                    }
                                    newFirstScrollingSlot = GetPreviousVisibleSlot(newFirstScrollingSlot);
                                }
                                else
                                {
                                    NegVerticalOffset = 0;
                                    break;
                                }
                                double rowHeight = GetExactSlotElementHeight(newFirstScrollingSlot);
                                double remainingHeight = height - deltaY;
                                if (MathUtilities.LessThanOrClose(rowHeight + remainingHeight, 0))
                                {
                                    deltaY -= rowHeight;
                                }
                                else
                                {
                                    NegVerticalOffset = rowHeight + remainingHeight;
                                    break;
                                }
                            }
                        }
                    }
                    if (MathUtilities.GreaterThanOrClose(0, newVerticalOffset) && newFirstScrollingSlot != 0)
                    {
                        // We've scrolled to the top of the ScrollBar, automatically place the user at the very top
                        // of the DataGrid.  If this produces very odd behavior, evaluate the RowHeight estimate.  
                        // strategy. For most data, this should be unnoticeable.
                        ResetDisplayedRows();
                        NegVerticalOffset = 0;
                        UpdateDisplayedRows(0, CellsHeight);
                        newFirstScrollingSlot = 0;
                    }
                }

                double firstRowHeight = GetExactSlotElementHeight(newFirstScrollingSlot);
                if (MathUtilities.LessThan(firstRowHeight, NegVerticalOffset))
                {
                    // We've scrolled off more of the first row than what's possible.  This can happen
                    // if the first row got shorter (Ex: Collapsing RowDetails) or if the user has a recycling
                    // cleanup issue.  In this case, simply try to display the next row as the first row instead
                    if (newFirstScrollingSlot < SlotCount - 1)
                    {
                        newFirstScrollingSlot = GetNextVisibleSlot(newFirstScrollingSlot);
                        Debug.Assert(newFirstScrollingSlot != -1);
                    }
                    NegVerticalOffset = 0;
                }

                UpdateDisplayedRows(newFirstScrollingSlot, CellsHeight);

                double firstElementHeight = GetExactSlotElementHeight(DisplayData.FirstScrollingSlot);
                if (MathUtilities.GreaterThan(NegVerticalOffset, firstElementHeight))
                {
                    int firstElementSlot = DisplayData.FirstScrollingSlot;
                    // We filled in some rows at the top and now we have a NegVerticalOffset that's greater than the first element
                    while (newFirstScrollingSlot > 0 && MathUtilities.GreaterThan(NegVerticalOffset, firstElementHeight))
                    {
                        int previousSlot = GetPreviousVisibleSlot(firstElementSlot);
                        if (previousSlot == -1)
                        {
                            NegVerticalOffset = 0;
                            _verticalOffset = 0;
                        }
                        else
                        {
                            NegVerticalOffset -= firstElementHeight;
                            _verticalOffset = Math.Max(0, _verticalOffset - firstElementHeight);
                            firstElementSlot = previousSlot;
                            firstElementHeight = GetExactSlotElementHeight(firstElementSlot);
                        }
                    }
                    // We could be smarter about this, but it's not common so we wouldn't gain much from optimizing here
                    if (firstElementSlot != DisplayData.FirstScrollingSlot)
                    {
                        UpdateDisplayedRows(firstElementSlot, CellsHeight);
                    }
                }

                Debug.Assert(DisplayData.FirstScrollingSlot >= 0);
                Debug.Assert(GetExactSlotElementHeight(DisplayData.FirstScrollingSlot) > NegVerticalOffset);

                if (DisplayData.FirstScrollingSlot == 0)
                {
                    _verticalOffset = NegVerticalOffset;
                }
                else if (MathUtilities.GreaterThan(NegVerticalOffset, newVerticalOffset))
                {
                    // The scrolled-in row was larger than anticipated. Adjust the DataGrid so the ScrollBar thumb
                    // can stay in the same place
                    NegVerticalOffset = newVerticalOffset;
                    _verticalOffset = newVerticalOffset;
                }
                else
                {
                    _verticalOffset = newVerticalOffset;
                }

                Debug.Assert(!(_verticalOffset == 0 && NegVerticalOffset == 0 && DisplayData.FirstScrollingSlot > 0));

                SetVerticalOffset(_verticalOffset);

                DisplayData.FullyRecycleElements();

                Debug.Assert(MathUtilities.GreaterThanOrClose(NegVerticalOffset, 0));
                Debug.Assert(MathUtilities.GreaterThanOrClose(_verticalOffset, NegVerticalOffset));
            }
            finally
            {
                _scrollingByHeight = false;
            }
        }

        private void SelectDisplayedElement(int slot)
        {
            Debug.Assert(IsSlotVisible(slot));
            Control element = DisplayData.GetDisplayedElement(slot);
            if (element is DataGridRow row)
            {
                row.UpdatePseudoClasses();
                EnsureRowDetailsVisibility(row, raiseNotification: true, animate: true);
            }
            else
            {
                // Assume it's a RowGroupHeader
                DataGridRowGroupHeader groupHeader = element as DataGridRowGroupHeader;
                groupHeader.UpdatePseudoClasses();
            }
        }

        private void SelectSlot(int slot, bool isSelected)
        {
            _selectedItems.SelectSlot(slot, isSelected);
            if (IsSlotVisible(slot))
            {
                SelectDisplayedElement(slot);
            }
        }

        private void SelectSlots(int startSlot, int endSlot, bool isSelected)
        {
            _selectedItems.SelectSlots(startSlot, endSlot, isSelected);

            // Apply the correct row state for display rows and also expand or collapse detail accordingly
            int firstSlot = Math.Max(DisplayData.FirstScrollingSlot, startSlot);
            int lastSlot = Math.Min(DisplayData.LastScrollingSlot, endSlot);

            for (int slot = firstSlot; slot <= lastSlot; slot++)
            {
                if (IsSlotVisible(slot))
                {
                    SelectDisplayedElement(slot);
                }
            }
        }

        private void UnloadElements(bool recycle)
        {
            // Since we're unloading all the elements, we can't be in editing mode anymore,
            // so commit if we can, otherwise force cancel.
            if (!CommitEdit())
            {
                CancelEdit(DataGridEditingUnit.Row, false);
            }
            ResetEditingRow();

            // Make sure to clear the focused row (because it's no longer relevant).
            if (_focusedRow != null)
            {
                ResetFocusedRow();
                Focus();
            }

            if (_rowsPresenter != null)
            {
                foreach (Control element in _rowsPresenter.Children)
                {
                    if (element is DataGridRow row)
                    {
                        // Raise UnloadingRow for any row that was visible
                        if (IsSlotVisible(row.Slot))
                        {
                            OnUnloadingRow(new DataGridRowEventArgs(row));
                        }
                        row.DetachFromDataGrid(recycle && row.IsRecyclable /*recycle*/);
                    }
                    else if (element is DataGridRowGroupHeader groupHeader)
                    {
                        if (IsSlotVisible(groupHeader.RowGroupInfo.Slot))
                        {
                            OnUnloadingRowGroup(new DataGridRowGroupHeaderEventArgs(groupHeader));
                        }
                    }
                }

                if (!recycle)
                {
                    _rowsPresenter.Children.Clear();
                }
            }
            DisplayData.ClearElements(recycle);

            // Update the AvailableRowRoom since we're displaying 0 rows now
            AvailableSlotElementRoom = CellsHeight;
            VisibleSlotCount = 0;
        }

        //TODO Styles
        private void UnloadRow(DataGridRow dataGridRow)
        {
            Debug.Assert(dataGridRow != null);
            Debug.Assert(_rowsPresenter != null);
            Debug.Assert(_rowsPresenter.Children.Contains(dataGridRow));

            if (_loadedRows.Contains(dataGridRow))
            {
                return; // The row is still referenced, we can't release it.
            }

            // Raise UnloadingRow regardless of whether the row will be recycled
            OnUnloadingRow(new DataGridRowEventArgs(dataGridRow));
            bool recycleRow = CurrentSlot != dataGridRow.Index;

            // Don't recycle if the row has a custom Style set
            //recycleRow &= (dataGridRow.Style == null || dataGridRow.Style == RowStyle);

            if (recycleRow)
            {
                DisplayData.AddRecyclableRow(dataGridRow);
            }
            else
            {
                // 
                _rowsPresenter.Children.Remove(dataGridRow);
                dataGridRow.DetachFromDataGrid(false);
            }
        }

        private void UpdateDisplayedRows(int newFirstDisplayedSlot, double displayHeight)
        {
            Debug.Assert(!_collapsedSlotsTable.Contains(newFirstDisplayedSlot));
            int firstDisplayedScrollingSlot = newFirstDisplayedSlot;
            int lastDisplayedScrollingSlot = -1;
            double deltaY = -NegVerticalOffset;
            int visibleScrollingRows = 0;

            if (MathUtilities.LessThanOrClose(displayHeight, 0) || SlotCount == 0 || ColumnsItemsInternal.Count == 0)
            {
                return;
            }

            if (firstDisplayedScrollingSlot == -1)
            {
                // 0 is fine because the element in the first slot cannot be collapsed
                firstDisplayedScrollingSlot = 0;
            }

            int slot = firstDisplayedScrollingSlot;
            while (slot < SlotCount && !MathUtilities.GreaterThanOrClose(deltaY, displayHeight))
            {
                deltaY += GetExactSlotElementHeight(slot);
                visibleScrollingRows++;
                lastDisplayedScrollingSlot = slot;
                slot = GetNextVisibleSlot(slot);
            }

            while (MathUtilities.LessThan(deltaY, displayHeight) && slot >= 0)
            {
                slot = GetPreviousVisibleSlot(firstDisplayedScrollingSlot);
                if (slot >= 0)
                {
                    deltaY += GetExactSlotElementHeight(slot);
                    firstDisplayedScrollingSlot = slot;
                    visibleScrollingRows++;
                }
            }
            // If we're up to the first row, and we still have room left, uncover as much of the first row as we can
            if (firstDisplayedScrollingSlot == 0 && MathUtilities.LessThan(deltaY, displayHeight))
            {
                double newNegVerticalOffset = Math.Max(0, NegVerticalOffset - displayHeight + deltaY);
                deltaY += NegVerticalOffset - newNegVerticalOffset;
                NegVerticalOffset = newNegVerticalOffset;
            }

            if (MathUtilities.GreaterThan(deltaY, displayHeight) || (MathUtilities.AreClose(deltaY, displayHeight) && MathUtilities.GreaterThan(NegVerticalOffset, 0)))
            {
                DisplayData.NumTotallyDisplayedScrollingElements = visibleScrollingRows - 1;
            }
            else
            {
                DisplayData.NumTotallyDisplayedScrollingElements = visibleScrollingRows;
            }
            if (visibleScrollingRows == 0)
            {
                firstDisplayedScrollingSlot = -1;
                Debug.Assert(lastDisplayedScrollingSlot == -1);
            }

            Debug.Assert(lastDisplayedScrollingSlot < SlotCount, "lastDisplayedScrollingRow larger than number of rows");

            RemoveNonDisplayedRows(firstDisplayedScrollingSlot, lastDisplayedScrollingSlot);

            Debug.Assert(DisplayData.NumDisplayedScrollingElements >= 0, "the number of visible scrolling rows can't be negative");
            Debug.Assert(DisplayData.NumTotallyDisplayedScrollingElements >= 0, "the number of totally visible scrolling rows can't be negative");
            Debug.Assert(DisplayData.FirstScrollingSlot < SlotCount, "firstDisplayedScrollingRow larger than number of rows");
            Debug.Assert(DisplayData.FirstScrollingSlot == firstDisplayedScrollingSlot);
            Debug.Assert(DisplayData.LastScrollingSlot == lastDisplayedScrollingSlot);
        }

        // Similar to UpdateDisplayedRows except that it starts with the LastDisplayedScrollingRow
        // and computes the FirstDisplayScrollingRow instead of doing it the other way around.  We use this
        // when scrolling down to a full row
        private void UpdateDisplayedRowsFromBottom(int newLastDisplayedScrollingRow)
        {
            //Debug.Assert(!_collapsedSlotsTable.Contains(newLastDisplayedScrollingRow));

            int lastDisplayedScrollingRow = newLastDisplayedScrollingRow;
            int firstDisplayedScrollingRow = -1;
            double displayHeight = CellsHeight;
            double deltaY = 0;
            int visibleScrollingRows = 0;

            if (MathUtilities.LessThanOrClose(displayHeight, 0) || SlotCount == 0 || ColumnsItemsInternal.Count == 0)
            {
                ResetDisplayedRows();
                return;
            }

            if (lastDisplayedScrollingRow == -1)
            {
                lastDisplayedScrollingRow = 0;
            }

            int slot = lastDisplayedScrollingRow;
            while (MathUtilities.LessThan(deltaY, displayHeight) && slot >= 0)
            {
                deltaY += GetExactSlotElementHeight(slot);
                visibleScrollingRows++;
                firstDisplayedScrollingRow = slot;
                slot = GetPreviousVisibleSlot(slot);
            }

            DisplayData.NumTotallyDisplayedScrollingElements = deltaY > displayHeight ? visibleScrollingRows - 1 : visibleScrollingRows;

            Debug.Assert(DisplayData.NumTotallyDisplayedScrollingElements >= 0);
            Debug.Assert(lastDisplayedScrollingRow < SlotCount, "lastDisplayedScrollingRow larger than number of rows");

            NegVerticalOffset = Math.Max(0, deltaY - displayHeight);

            RemoveNonDisplayedRows(firstDisplayedScrollingRow, lastDisplayedScrollingRow);

            Debug.Assert(DisplayData.NumDisplayedScrollingElements >= 0, "the number of visible scrolling rows can't be negative");
            Debug.Assert(DisplayData.NumTotallyDisplayedScrollingElements >= 0, "the number of totally visible scrolling rows can't be negative");
            Debug.Assert(DisplayData.FirstScrollingSlot < SlotCount, "firstDisplayedScrollingRow larger than number of rows");
        }

        private void UpdateTablesForRemoval(int slotDeleted, object itemDeleted)
        {
            if (RowGroupHeadersTable.Contains(slotDeleted))
            {
                // A RowGroupHeader was removed
                RowGroupHeadersTable.RemoveIndexAndValue(slotDeleted);
                _collapsedSlotsTable.RemoveIndexAndValue(slotDeleted);
                _selectedItems.DeleteSlot(slotDeleted);
            }
            else
            {
                // Update the ranges of selected rows
                if (_selectedItems.ContainsSlot(slotDeleted))
                {
                    SelectionHasChanged = true;
                }
                _selectedItems.Delete(slotDeleted, itemDeleted);
                RowGroupHeadersTable.RemoveIndex(slotDeleted);
                _collapsedSlotsTable.RemoveIndex(slotDeleted);
            }
        }

        private void CollectionViewGroup_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If we receive this event when the number of GroupDescriptions is different than what we have already
            // accounted for, that means the ICollectionView is still in the process of updating its groups.  It will
            // send a reset notification when it's done, at which point we can update our visuals.

            if (_rowGroupHeightsByLevel != null &&
                DataConnection.CollectionView != null &&
                DataConnection.CollectionView.IsGrouping &&
                DataConnection.CollectionView.GroupingDepth == _rowGroupHeightsByLevel.Length)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        CollectionViewGroup_CollectionChanged_Add(sender, e);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        CollectionViewGroup_CollectionChanged_Remove(sender, e);
                        break;
                }
            }
        }

        private void CollectionViewGroup_CollectionChanged_Add(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                // We need to figure out the CollectionViewGroup that the sender belongs to.  We could cache
                // it by tagging the collections ahead of time, but I think the extra storage might not be worth
                // it since this lookup should be performant enough
                int insertSlot = -1;
                DataGridRowGroupInfo parentGroupInfo = GetParentGroupInfo(sender);
                DataGridCollectionViewGroup group = e.NewItems[0] as DataGridCollectionViewGroup;

                if (parentGroupInfo != null)
                {
                    if (group != null || parentGroupInfo.Level == -1)
                    {
                        insertSlot = parentGroupInfo.Slot + 1;
                        // For groups, we need to skip over subgroups to find the correct slot
                        DataGridRowGroupInfo groupInfo;
                        for (int i = 0; i < e.NewStartingIndex; i++)
                        {
                            do
                            {
                                insertSlot = RowGroupHeadersTable.GetNextIndex(insertSlot);
                                groupInfo = RowGroupHeadersTable.GetValueAt(insertSlot);
                            }
                            while (groupInfo != null && groupInfo.Level > parentGroupInfo.Level + 1);
                            if (groupInfo == null)
                            {
                                // We couldn't find the subchild so this should go at the end
                                insertSlot = SlotCount;
                            }
                        }

                    }
                    else
                    {
                        // For items the slot is a simple calculation
                        insertSlot = parentGroupInfo.Slot + e.NewStartingIndex + 1;
                    }
                }

                // This could not be found when new GroupDescriptions are added to the PagedCollectionView
                if (insertSlot != -1)
                {
                    bool isCollapsed = (parentGroupInfo != null) && (!parentGroupInfo.IsVisible || _collapsedSlotsTable.Contains(parentGroupInfo.Slot));
                    if (group != null)
                    {
                        if (group.Items != null)
                        {
                            group.Items.CollectionChanged += CollectionViewGroup_CollectionChanged;
                        }
                        var newGroupInfo = new DataGridRowGroupInfo(group, true, parentGroupInfo.Level + 1, insertSlot, insertSlot);
                        InsertElementAt(insertSlot, 
                            rowIndex: -1, 
                            item: null, 
                            groupInfo: newGroupInfo, 
                            isCollapsed: isCollapsed);
                        RowGroupHeadersTable.AddValue(insertSlot, newGroupInfo);
                    }
                    else
                    {
                        // Assume we're adding a new row
                        int rowIndex = DataConnection.IndexOf(e.NewItems[0]);
                        Debug.Assert(rowIndex != -1);
                        if (SlotCount == 0 && DataConnection.ShouldAutoGenerateColumns)
                        {
                            AutoGenerateColumnsPrivate();
                        }
                        InsertElementAt(insertSlot, rowIndex, 
                            item: e.NewItems[0],
                            groupInfo: null, 
                            isCollapsed: isCollapsed);
                    }

                    CorrectLastSubItemSlotsAfterInsertion(parentGroupInfo);
                    if (parentGroupInfo.LastSubItemSlot - parentGroupInfo.Slot == 1)
                    {
                        // We just added the first item to a RowGroup so the header should transition from Empty to either Expanded or Collapsed
                        EnsureAncestorsExpanderButtonChecked(parentGroupInfo);
                    }
                }
            }
        }

        private void CollectionViewGroup_CollectionChanged_Remove(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(e.OldItems.Count == 1);
            if (e.OldItems != null && e.OldItems.Count > 0)
            {
                if (e.OldItems[0] is DataGridCollectionViewGroup removedGroup)
                {
                    if (removedGroup.Items != null)
                    {
                        removedGroup.Items.CollectionChanged -= CollectionViewGroup_CollectionChanged;
                    }
                    DataGridRowGroupInfo groupInfo = RowGroupInfoFromCollectionViewGroup(removedGroup);
                    Debug.Assert(groupInfo != null);
                    if ((groupInfo.Level == _rowGroupHeightsByLevel.Length - 1) && (removedGroup.Items != null) && (removedGroup.Items.Count > 0))
                    {
                        Debug.Assert((groupInfo.LastSubItemSlot - groupInfo.Slot) == removedGroup.Items.Count);
                        // If we're removing a leaf Group then remove all of its items before removing the Group
                        for (int i = 0; i < removedGroup.Items.Count; i++)
                        {
                            RemoveElementAt(groupInfo.Slot + 1, item: removedGroup.Items[i], isRow: true);
                        }
                    }
                    RemoveElementAt(groupInfo.Slot, item: null, isRow: false);
                }
                else
                {
                    // A single item was removed from a leaf group
                    DataGridRowGroupInfo parentGroupInfo = GetParentGroupInfo(sender);
                    if (parentGroupInfo != null)
                    {
                        int slot;
                        if (parentGroupInfo.CollectionViewGroup == null && RowGroupHeadersTable.IndexCount > 0)
                        {
                            // In this case, we're removing from the root group.  If there are other groups, then this must
                            // be the new item row that doesn't belong to any group because if there are other groups then
                            // this item cannot be a child of the root group.
                            slot = SlotCount - 1;
                        }
                        else
                        {
                            slot = parentGroupInfo.Slot + e.OldStartingIndex + 1;
                        }
                        RemoveElementAt(slot, e.OldItems[0], isRow: true);
                    }
                }
            }
        }

        private void ClearRowGroupHeadersTable()
        {
            // Detach existing handlers on CollectionViewGroup.Items.CollectionChanged
            foreach (int slot in RowGroupHeadersTable.GetIndexes())
            {
                DataGridRowGroupInfo groupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (groupInfo.CollectionViewGroup.Items != null)
                {
                    groupInfo.CollectionViewGroup.Items.CollectionChanged -= CollectionViewGroup_CollectionChanged;
                }
            }
            if (_topLevelGroup != null)
            {
                // The PagedCollectionView reuses the top level group so we need to detach any existing or else we'll get duplicate handers here
                _topLevelGroup.CollectionChanged -= CollectionViewGroup_CollectionChanged;
                _topLevelGroup = null;
            }

            RowGroupHeadersTable.Clear();
            // Unfortunately PagedCollectionView does not allow us to preserve expanded or collapsed states for RowGroups since
            // the CollectionViewGroups are recreated when a Reset happens.  This is true in both SL and WPF
            _collapsedSlotsTable.Clear();

            _rowGroupHeightsByLevel = null;
            RowGroupSublevelIndents = null;
        }

        private void CollectionViewGroup_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ItemCount")
            {
                DataGridRowGroupInfo rowGroupInfo = RowGroupInfoFromCollectionViewGroup(sender as DataGridCollectionViewGroup);
                if (rowGroupInfo != null && IsSlotVisible(rowGroupInfo.Slot))
                {
                    if (DisplayData.GetDisplayedElement(rowGroupInfo.Slot) is DataGridRowGroupHeader rowGroupHeader)
                    {
                        rowGroupHeader.UpdateTitleElements();
                    }
                }
            }
        }

        // This method is necessary for incrementing the LastSubItemSlot property of the group ancestors
        // because CorrectSlotsAfterInsertion only increments those that come after the specified group
        private void CorrectLastSubItemSlotsAfterInsertion(DataGridRowGroupInfo subGroupInfo)
        {
            int subGroupSlot;
            int subGroupLevel;
            while (subGroupInfo != null)
            {
                subGroupLevel = subGroupInfo.Level;
                subGroupInfo.LastSubItemSlot++;

                while (subGroupInfo != null && subGroupInfo.Level >= subGroupLevel)
                {
                    subGroupSlot = RowGroupHeadersTable.GetPreviousIndex(subGroupInfo.Slot);
                    subGroupInfo = RowGroupHeadersTable.GetValueAt(subGroupSlot);
                }
            }
        }

        private int CountAndPopulateGroupHeaders(object group, int rootSlot, int level)
        {
            int treeCount = 1;

            if (group is DataGridCollectionViewGroup collectionViewGroup)
            {
                if (collectionViewGroup.Items != null && collectionViewGroup.Items.Count > 0)
                {
                    collectionViewGroup.Items.CollectionChanged += CollectionViewGroup_CollectionChanged;
                    if (collectionViewGroup.Items[0] is DataGridCollectionViewGroup)
                    {
                        foreach (object subGroup in collectionViewGroup.Items)
                        {
                            treeCount += CountAndPopulateGroupHeaders(subGroup, rootSlot + treeCount, level + 1);
                        }
                    }
                    else
                    {
                        // Optimization: don't walk to the bottom level nodes
                        treeCount += collectionViewGroup.Items.Count;
                    }
                }
                RowGroupHeadersTable.AddValue(rootSlot, new DataGridRowGroupInfo(collectionViewGroup, true, level, rootSlot, rootSlot + treeCount - 1));
            }
            return treeCount;
        }

        private void EnsureAncestorsExpanderButtonChecked(DataGridRowGroupInfo parentGroupInfo)
        {
            if (IsSlotVisible(parentGroupInfo.Slot))
            {
                DataGridRowGroupHeader ancestorGroupHeader = DisplayData.GetDisplayedElement(parentGroupInfo.Slot) as DataGridRowGroupHeader;
                while (ancestorGroupHeader != null)
                {
                    ancestorGroupHeader.EnsureExpanderButtonIsChecked();
                    if (ancestorGroupHeader.Level > 0)
                    {
                        int slot = RowGroupHeadersTable.GetPreviousIndex(ancestorGroupHeader.RowGroupInfo.Slot);
                        if (IsSlotVisible(slot))
                        {
                            ancestorGroupHeader = DisplayData.GetDisplayedElement(slot) as DataGridRowGroupHeader;
                            continue;
                        }
                    }
                    break;
                }
            }
        }

        private void PopulateRowGroupHeadersTable()
        {
            if (DataConnection.CollectionView != null
                && DataConnection.CollectionView.CanGroup
                && DataConnection.CollectionView.Groups != null)
            {
                int totalSlots = 0;
                _topLevelGroup = (INotifyCollectionChanged)DataConnection.CollectionView.Groups;
                _topLevelGroup.CollectionChanged += CollectionViewGroup_CollectionChanged;
                foreach (object group in DataConnection.CollectionView.Groups)
                {
                    totalSlots += CountAndPopulateGroupHeaders(group, totalSlots, 0);
                }
            }
            SlotCount = DataConnection.Count + RowGroupHeadersTable.IndexCount;
            VisibleSlotCount = SlotCount;
        }

        //TODO Styles
        private void RefreshRowGroupHeaders()
        {
            if (DataConnection.CollectionView != null
                && DataConnection.CollectionView.CanGroup
                && DataConnection.CollectionView.Groups != null
                && DataConnection.CollectionView.IsGrouping 
                && DataConnection.CollectionView.GroupingDepth > 0)
            {
                // Initialize our array for the height of the RowGroupHeaders by Level.
                // If the Length is the same, we can reuse the old array
                int groupLevelCount = DataConnection.CollectionView.GroupingDepth;
                if (_rowGroupHeightsByLevel == null || _rowGroupHeightsByLevel.Length != groupLevelCount)
                {
                    _rowGroupHeightsByLevel = new double[groupLevelCount];
                    for (int i = 0; i < groupLevelCount; i++)
                    {
                        // Default height for now, the actual heights are updated as the RowGroupHeaders
                        // are added and measured
                        _rowGroupHeightsByLevel[i] = DATAGRID_defaultRowHeight;
                    }
                }
                if (RowGroupSublevelIndents == null || RowGroupSublevelIndents.Length != groupLevelCount)
                {
                    RowGroupSublevelIndents = new double[groupLevelCount];
                    double indent;
                    for (int i = 0; i < groupLevelCount; i++)
                    {
                        indent = DATAGRID_defaultRowGroupSublevelIndent; 
                        RowGroupSublevelIndents[i] = indent;
                        if (i > 0)
                        {
                            RowGroupSublevelIndents[i] += RowGroupSublevelIndents[i - 1];
                        }
                    }
                }
                EnsureRowGroupSpacerColumnWidth(groupLevelCount);
            }
        }

        private void EnsureRowGroupSpacerColumn()
        {
            bool spacerColumnChanged = ColumnsInternal.EnsureRowGrouping(!RowGroupHeadersTable.IsEmpty);
            if (spacerColumnChanged)
            {
                if (ColumnsInternal.RowGroupSpacerColumn.IsRepresented && CurrentColumnIndex == 0)
                {
                    CurrentColumn = ColumnsInternal.FirstVisibleNonFillerColumn;
                }

                ProcessFrozenColumnCount();
            }
        }

        private void EnsureRowGroupSpacerColumnWidth(int groupLevelCount)
        {
            if (groupLevelCount == 0)
            {
                ColumnsInternal.RowGroupSpacerColumn.Width = new DataGridLength(0);
            }
            else
            {
                ColumnsInternal.RowGroupSpacerColumn.Width = new DataGridLength(RowGroupSublevelIndents[groupLevelCount - 1]);
            }
        }

        private void EnsureRowGroupVisibility(DataGridRowGroupInfo rowGroupInfo, bool isVisible, bool setCurrent)
        {
            if (rowGroupInfo == null)
            {
                return;
            }
            if (rowGroupInfo.IsVisible != isVisible)
            {
                if (IsSlotVisible(rowGroupInfo.Slot))
                {
                    DataGridRowGroupHeader rowGroupHeader = DisplayData.GetDisplayedElement(rowGroupInfo.Slot) as DataGridRowGroupHeader;
                    Debug.Assert(rowGroupHeader != null);
                    rowGroupHeader.ToggleExpandCollapse(isVisible, setCurrent);
                }
                else
                {
                    if (_collapsedSlotsTable.Contains(rowGroupInfo.Slot))
                    {
                        // Somewhere up the parent chain, there's a collapsed header so all the slots remain the same and
                        // we just need to mark this header with the new visibility
                        rowGroupInfo.IsVisible = isVisible;
                    }
                    else
                    {
                        if (rowGroupInfo.Slot < DisplayData.FirstScrollingSlot)
                        {
                            double heightChange = UpdateRowGroupVisibility(rowGroupInfo, isVisible, isDisplayed: false);
                            // Use epsilon instead of 0 here so that in the off chance that our estimates put the vertical offset negative
                            // the user can still scroll to the top since the offset is non-zero
                            SetVerticalOffset(Math.Max(MathUtilities.DoubleEpsilon, _verticalOffset + heightChange));
                        }
                        else
                        {
                            UpdateRowGroupVisibility(rowGroupInfo, isVisible, isDisplayed: false);
                        }
                        UpdateVerticalScrollBar();
                    }
                }
            }
        }

        // Returns the inclusive count of expanded RowGroupHeaders from startSlot to endSlot
        private int GetRowGroupHeaderCount(int startSlot, int endSlot, bool? isVisible, out double headersHeight)
        {
            int count = 0;
            headersHeight = 0;
            foreach (int slot in RowGroupHeadersTable.GetIndexes(startSlot))
            {
                if (slot > endSlot)
                {
                    return count;
                }
                DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (!isVisible.HasValue ||
                    (isVisible.Value && !_collapsedSlotsTable.Contains(slot)) ||
                    (!isVisible.Value && _collapsedSlotsTable.Contains(slot)))
                {
                    count++;
                    headersHeight += _rowGroupHeightsByLevel[rowGroupInfo.Level];
                }
            }
            return count;
        }

        // This method does not check the state of the parent RowGroupHeaders, it assumes they're ready for this newVisibility to
        // be applied this header
        // Returns the number of pixels that were expanded or (collapsed); however, if we're expanding displayed rows, we only expand up
        // to what we can display
        private double UpdateRowGroupVisibility(DataGridRowGroupInfo targetRowGroupInfo, bool newIsVisible, bool isDisplayed)
        {
            double heightChange = 0;
            int slotsExpanded = 0;
            int startSlot = targetRowGroupInfo.Slot + 1;
            int endSlot;

            targetRowGroupInfo.IsVisible = newIsVisible;
            if (newIsVisible)
            {
                // Expand
                foreach (int slot in RowGroupHeadersTable.GetIndexes(targetRowGroupInfo.Slot + 1))
                {
                    if (slot >= startSlot)
                    {
                        DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                        if (rowGroupInfo.Level <= targetRowGroupInfo.Level)
                        {
                            break;
                        }
                        if (!rowGroupInfo.IsVisible)
                        {
                            // Skip over the items in collapsed subgroups
                            endSlot = rowGroupInfo.Slot;
                            ExpandSlots(startSlot, endSlot, isDisplayed, ref slotsExpanded, ref heightChange);
                            startSlot = rowGroupInfo.LastSubItemSlot + 1;
                        }
                    }
                }
                if (targetRowGroupInfo.LastSubItemSlot >= startSlot)
                {
                    ExpandSlots(startSlot, targetRowGroupInfo.LastSubItemSlot, isDisplayed, ref slotsExpanded, ref heightChange);
                }
                if (isDisplayed)
                {
                    UpdateDisplayedRows(DisplayData.FirstScrollingSlot, CellsHeight);
                }
            }
            else
            {
                // Collapse
                endSlot = SlotCount - 1;
                foreach (int slot in RowGroupHeadersTable.GetIndexes(targetRowGroupInfo.Slot + 1))
                {
                    DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                    if (rowGroupInfo.Level <= targetRowGroupInfo.Level)
                    {
                        endSlot = slot - 1;
                        break;
                    }
                }

                int oldLastDisplayedSlot = DisplayData.LastScrollingSlot;
                int endDisplayedSlot = Math.Min(endSlot, DisplayData.LastScrollingSlot);
                if (isDisplayed)
                {
                    // We need to remove all the displayed slots that aren't already collapsed
                    int elementsToRemove = endDisplayedSlot - startSlot + 1 - _collapsedSlotsTable.GetIndexCount(startSlot, endDisplayedSlot);

                    if (_focusedRow != null && _focusedRow.Slot >= startSlot && _focusedRow.Slot <= endSlot)
                    {
                        Debug.Assert(EditingRow == null);
                        // Don't call ResetFocusedRow here because we're already cleaning it up below, and we don't want to FullyRecycle yet
                        _focusedRow = null;
                    }

                    for (int i = 0; i < elementsToRemove; i++)
                    {
                        RemoveDisplayedElement(startSlot, wasDeleted: false , updateSlotInformation: false);
                    }
                }

                double heightChangeBelowLastDisplayedSlot = 0;
                if (DisplayData.FirstScrollingSlot >= startSlot && DisplayData.FirstScrollingSlot <= endSlot)
                {
                    // Our first visible slot was collapsed, find the replacement
                    int collapsedSlotsAbove = DisplayData.FirstScrollingSlot - startSlot - _collapsedSlotsTable.GetIndexCount(startSlot, DisplayData.FirstScrollingSlot);
                    Debug.Assert(collapsedSlotsAbove > 0);
                    int newFirstScrollingSlot = GetNextVisibleSlot(DisplayData.FirstScrollingSlot);
                    while (collapsedSlotsAbove > 1 && newFirstScrollingSlot < SlotCount)
                    {
                        collapsedSlotsAbove--;
                        newFirstScrollingSlot = GetNextVisibleSlot(newFirstScrollingSlot);
                    }
                    heightChange += CollapseSlotsInTable(startSlot, endSlot, ref slotsExpanded, oldLastDisplayedSlot, ref heightChangeBelowLastDisplayedSlot);
                    if (isDisplayed)
                    {
                        if (newFirstScrollingSlot >= SlotCount)
                        {
                            // No visible slots below, look up
                            UpdateDisplayedRowsFromBottom(targetRowGroupInfo.Slot);
                        }
                        else
                        {
                            UpdateDisplayedRows(newFirstScrollingSlot, CellsHeight);
                        }
                    }
                }
                else
                {
                    heightChange += CollapseSlotsInTable(startSlot, endSlot, ref slotsExpanded, oldLastDisplayedSlot, ref heightChangeBelowLastDisplayedSlot);
                }

                if (DisplayData.LastScrollingSlot >= startSlot && DisplayData.LastScrollingSlot <= endSlot)
                {
                    // Collapsed the last scrolling row, we need to update it
                    DisplayData.LastScrollingSlot = GetPreviousVisibleSlot(DisplayData.LastScrollingSlot);
                }

                // Collapsing could cause the vertical offset to move up if we collapsed a lot of slots
                // near the bottom of the DataGrid.  To do this, we compare the height we collapsed to
                // the distance to the last visible row and adjust the scrollbar if we collapsed more
                if (isDisplayed && _verticalOffset > 0)
                {
                    int lastVisibleSlot = GetPreviousVisibleSlot(SlotCount);
                    int slot = GetNextVisibleSlot(oldLastDisplayedSlot);
                    // AvailableSlotElementRoom ends up being the amount of the last slot that is partially scrolled off
                    // as a negative value, heightChangeBelowLastDisplayed slot is also a negative value since we're collapsing
                    double heightToLastVisibleSlot = AvailableSlotElementRoom + heightChangeBelowLastDisplayedSlot;
                    while ((heightToLastVisibleSlot > heightChange) && (slot < lastVisibleSlot))
                    {
                        heightToLastVisibleSlot -= GetSlotElementHeight(slot);
                        slot = GetNextVisibleSlot(slot);
                    }
                    if (heightToLastVisibleSlot > heightChange)
                    {
                        double newVerticalOffset = _verticalOffset + heightChange - heightToLastVisibleSlot;
                        if (newVerticalOffset > 0)
                        {
                            SetVerticalOffset(newVerticalOffset);
                        }
                        else
                        {
                            // Collapsing causes the vertical offset to go to 0 so we should go back to the first row.
                            ResetDisplayedRows();
                            NegVerticalOffset = 0;
                            SetVerticalOffset(0);
                            int firstDisplayedRow = GetNextVisibleSlot(-1);
                            UpdateDisplayedRows(firstDisplayedRow, CellsHeight);
                        }
                    }
                }
            }

            // Update VisibleSlotCount
            VisibleSlotCount += slotsExpanded;

            return heightChange;
        }

        private DataGridRowGroupHeader GenerateRowGroupHeader(int slot, DataGridRowGroupInfo rowGroupInfo)
        {
            Debug.Assert(slot > -1);
            Debug.Assert(rowGroupInfo != null);

            DataGridRowGroupHeader groupHeader = DisplayData.GetUsedGroupHeader() ?? new DataGridRowGroupHeader();
            groupHeader.OwningGrid = this;
            groupHeader.RowGroupInfo = rowGroupInfo;
            groupHeader.DataContext = rowGroupInfo.CollectionViewGroup;
            groupHeader.Level = rowGroupInfo.Level;

            // Set the RowGroupHeader's PropertyName. Unfortunately, CollectionViewGroup doesn't have this
            // so we have to set it manually
            Debug.Assert(DataConnection.CollectionView != null && groupHeader.Level < DataConnection.CollectionView.GroupingDepth);
            string propertyName = DataConnection.CollectionView.GetGroupingPropertyNameAtDepth(groupHeader.Level);

            if(string.IsNullOrWhiteSpace(propertyName))
            {
                groupHeader.PropertyName = null;
            }
            else
            {
                groupHeader.PropertyName = DataConnection.DataType?.GetDisplayName(propertyName) ?? propertyName;
            }

            if (rowGroupInfo.CollectionViewGroup is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= new PropertyChangedEventHandler(CollectionViewGroup_PropertyChanged);
                inpc.PropertyChanged += new PropertyChangedEventHandler(CollectionViewGroup_PropertyChanged);
            }
            groupHeader.UpdateTitleElements();

            OnLoadingRowGroup(new DataGridRowGroupHeaderEventArgs(groupHeader));

            return groupHeader;
        }

        private DataGridRowGroupInfo GetParentGroupInfo(object collection)
        {
            if (collection == DataConnection.CollectionView.Groups)
            {
                // If the new item is a root level element, it has no parent group, so create an empty RowGroupInfo
                return new DataGridRowGroupInfo(null, true, -1, -1, -1);
            }
            else
            {
                foreach (int slot in RowGroupHeadersTable.GetIndexes())
                {
                    DataGridRowGroupInfo groupInfo = RowGroupHeadersTable.GetValueAt(slot);
                    if (groupInfo.CollectionViewGroup.Items == collection)
                    {
                        return groupInfo;
                    }
                }
            }
            return null;
        }

        internal void OnRowGroupHeaderToggled(DataGridRowGroupHeader groupHeader, bool newIsVisible, bool setCurrent)
        {
            Debug.Assert(groupHeader.RowGroupInfo.CollectionViewGroup.ItemCount > 0);

            if (WaitForLostFocus(delegate { OnRowGroupHeaderToggled(groupHeader, newIsVisible, setCurrent); }) || !CommitEdit())
            {
                return;
            }

            if (setCurrent && CurrentSlot != groupHeader.RowGroupInfo.Slot)
            {
                // Most of the time this is set by the MouseLeftButtonDown handler but validation could cause that code path to fail
                UpdateSelectionAndCurrency(CurrentColumnIndex, groupHeader.RowGroupInfo.Slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false);
            }

            UpdateRowGroupVisibility(groupHeader.RowGroupInfo, newIsVisible, isDisplayed: true);

            ComputeScrollBarsLayout();
            // We need force arrange since our Scrollings Rows could update without automatically triggering layout
            InvalidateRowsArrange();
        }

        internal void OnSublevelIndentUpdated(DataGridRowGroupHeader groupHeader, double newValue)
        {
            Debug.Assert(DataConnection.CollectionView != null);
            Debug.Assert(RowGroupSublevelIndents != null);

            int groupLevelCount = DataConnection.CollectionView.GroupingDepth;
            Debug.Assert(groupHeader.Level >= 0 && groupHeader.Level < groupLevelCount);

            double oldValue = RowGroupSublevelIndents[groupHeader.Level];
            if (groupHeader.Level > 0)
            {
                oldValue -= RowGroupSublevelIndents[groupHeader.Level - 1];
            }
            // Update the affected values in our table by the amount affected
            double change = newValue - oldValue;
            for (int i = groupHeader.Level; i < groupLevelCount; i++)
            {
                RowGroupSublevelIndents[i] += change;
                Debug.Assert(RowGroupSublevelIndents[i] >= 0);
            }

            EnsureRowGroupSpacerColumnWidth(groupLevelCount);
        }

        internal DataGridRowGroupInfo RowGroupInfoFromCollectionViewGroup(DataGridCollectionViewGroup collectionViewGroup)
        {
            foreach (int slot in RowGroupHeadersTable.GetIndexes())
            {
                DataGridRowGroupInfo rowGroupInfo = RowGroupHeadersTable.GetValueAt(slot);
                if (rowGroupInfo.CollectionViewGroup == collectionViewGroup)
                {
                    return rowGroupInfo;
                }
            }
            return null;
        }

        /// <summary>
        /// Collapses the DataGridRowGroupHeader that represents a given CollectionViewGroup
        /// </summary>
        /// <param name="collectionViewGroup">CollectionViewGroup</param>
        /// <param name="collapseAllSubgroups">Set to true to collapse all Subgroups</param>
        public void CollapseRowGroup(DataGridCollectionViewGroup collectionViewGroup, bool collapseAllSubgroups)
        {
            if (WaitForLostFocus(delegate { CollapseRowGroup(collectionViewGroup, collapseAllSubgroups); }) ||
                collectionViewGroup == null || !CommitEdit())
            {
                return;
            }

            EnsureRowGroupVisibility(RowGroupInfoFromCollectionViewGroup(collectionViewGroup), false, true);

            if (collapseAllSubgroups)
            {
                foreach (object groupObj in collectionViewGroup.Items)
                {
                    if (groupObj is DataGridCollectionViewGroup subGroup)
                    {
                        CollapseRowGroup(subGroup, collapseAllSubgroups);
                    }
                }
            }
        }

        /// <summary>
        /// Expands the DataGridRowGroupHeader that represents a given CollectionViewGroup
        /// </summary>
        /// <param name="collectionViewGroup">CollectionViewGroup</param>
        /// <param name="expandAllSubgroups">Set to true to expand all Subgroups</param>
        public void ExpandRowGroup(DataGridCollectionViewGroup collectionViewGroup, bool expandAllSubgroups)
        {
            if (WaitForLostFocus(delegate { ExpandRowGroup(collectionViewGroup, expandAllSubgroups); }) ||
                collectionViewGroup == null || !CommitEdit())
                if (collectionViewGroup == null || !CommitEdit())
                {
                    return;
                }

            EnsureRowGroupVisibility(RowGroupInfoFromCollectionViewGroup(collectionViewGroup), true, true);

            if (expandAllSubgroups)
            {
                foreach (object groupObj in collectionViewGroup.Items)
                {
                    if (groupObj is DataGridCollectionViewGroup subGroup)
                    {
                        ExpandRowGroup(subGroup, expandAllSubgroups);
                    }
                }
            }
        }

        // Returns the number of rows with details visible between lowerBound and upperBound exclusive.
        // As of now, the caller needs to account for Collapsed slots.  This method assumes everything
        // is visible
        private int GetDetailsCountInclusive(int lowerBound, int upperBound)
        {
            int indexCount = upperBound - lowerBound + 1;
            if (indexCount <= 0)
            {
                return 0;
            }
            if (RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Visible)
            {
                // Total rows minus ones which explicity turned details off minus the RowGroupHeaders
                return indexCount - _showDetailsTable.GetIndexCount(lowerBound, upperBound, false) - RowGroupHeadersTable.GetIndexCount(lowerBound, upperBound);
            }
            else if (RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                // Total rows with details explicitly turned on
                return _showDetailsTable.GetIndexCount(lowerBound, upperBound, true);
            }
            else if (RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.VisibleWhenSelected)
            {
                // Total number of remaining rows that are selected
                return _selectedItems.GetIndexCount(lowerBound, upperBound);
            }
            Debug.Assert(false); // Shouldn't ever happen
            return 0;
        }

        private void EnsureRowDetailsVisibility(DataGridRow row, bool raiseNotification, bool animate)
        {
            // Show or hide RowDetails based on DataGrid settings
            row.SetDetailsVisibilityInternal(GetRowDetailsVisibility(row.Index), raiseNotification, animate);
        }

        private void UpdateRowDetailsHeightEstimate()
        {
            if (_rowsPresenter != null && _measured && RowDetailsTemplate != null)
            {
                object dataItem = null;
                if(VisibleSlotCount > 0)
                    dataItem = DataConnection.GetDataItem(0);
                var detailsContent = RowDetailsTemplate.Build(dataItem);
                if (detailsContent != null)
                {
                    _rowsPresenter.Children.Add(detailsContent);
                    if (dataItem != null)
                    {
                        detailsContent.DataContext = dataItem;
                    }
                    detailsContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    RowDetailsHeightEstimate = detailsContent.DesiredSize.Height;
                    _rowsPresenter.Children.Remove(detailsContent);
                }
            }
        }

        // detailsElement is the FrameworkElement created by the DetailsTemplate
        internal void OnUnloadingRowDetails(DataGridRow row, IControl detailsElement)
        {
            OnUnloadingRowDetails(new DataGridRowDetailsEventArgs(row, detailsElement));
        }

        // detailsElement is the FrameworkElement created by the DetailsTemplate
        internal void OnLoadingRowDetails(DataGridRow row, IControl detailsElement)
        {
            OnLoadingRowDetails(new DataGridRowDetailsEventArgs(row, detailsElement));
        }

        internal void OnRowDetailsVisibilityPropertyChanged(int rowIndex, bool isVisible)
        {
            Debug.Assert(rowIndex >= 0 && rowIndex < SlotCount);

            _showDetailsTable.AddValue(rowIndex, isVisible);
        }

        internal bool GetRowDetailsVisibility(int rowIndex)
        {
            return GetRowDetailsVisibility(rowIndex, RowDetailsVisibilityMode);
        }

        internal bool GetRowDetailsVisibility(int rowIndex, DataGridRowDetailsVisibilityMode gridLevelRowDetailsVisibility)
        {
            Debug.Assert(rowIndex != -1);
            if (_showDetailsTable.Contains(rowIndex))
            {
                // The user explicity set DetailsVisibility on a row so we should respect that
                return _showDetailsTable.GetValueAt(rowIndex);
            }
            else
            {
                return
                    gridLevelRowDetailsVisibility == DataGridRowDetailsVisibilityMode.Visible ||
                    (gridLevelRowDetailsVisibility == DataGridRowDetailsVisibilityMode.VisibleWhenSelected &&
                     _selectedItems.ContainsSlot(SlotFromRowIndex(rowIndex)));
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Avalonia.Controls.DataGrid.RowDetailsVisibilityChanged" /> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected internal virtual void OnRowDetailsVisibilityChanged(DataGridRowDetailsEventArgs e)
        {
            RowDetailsVisibilityChanged?.Invoke(this, e);
        }

#if DEBUG
        internal void PrintRowGroupInfo()
        {
            Debug.WriteLine("-----------------------------------------------RowGroupHeaders");
            foreach (int slot in RowGroupHeadersTable.GetIndexes())
            {
                DataGridRowGroupInfo info = RowGroupHeadersTable.GetValueAt(slot);
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1} Slot:{2} Last:{3} Level:{4}", info.CollectionViewGroup.Key, info.IsVisible.ToString(), slot, info.LastSubItemSlot, info.Level));
            }
            Debug.WriteLine("-----------------------------------------------CollapsedSlots");
            _collapsedSlotsTable.PrintIndexes();
        }
#endif
    }
}
