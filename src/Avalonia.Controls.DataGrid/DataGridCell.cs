// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an individual <see cref="T:Avalonia.Controls.DataGrid" /> cell.
    /// </summary>
    [PseudoClasses(":selected", ":current", ":edited", ":invalid")]
    public class DataGridCell : ContentControl
    {
        private const string DATAGRIDCELL_elementRightGridLine = "PART_RightGridLine";

        private Rectangle _rightGridLine;
        private DataGridColumn _owningColumn;

        bool _isValid = true;

        public static readonly DirectProperty<DataGridCell, bool> IsValidProperty =
            AvaloniaProperty.RegisterDirect<DataGridCell, bool>(
                nameof(IsValid),
                o => o.IsValid);

        static DataGridCell()
        {
            PointerPressedEvent.AddClassHandler<DataGridCell>(
                (x,e) => x.DataGridCell_PointerPressed(e), handledEventsToo: true);
        }
        public DataGridCell()
        { }

        public bool IsValid
        {
            get { return _isValid; }
            internal set { SetAndRaise(IsValidProperty, ref _isValid, value); }
        }

        internal DataGridColumn OwningColumn
        {
            get => _owningColumn;
            set
            {
                if (_owningColumn != value)
                {
                    _owningColumn = value;
                    OnOwningColumnSet(value);
                }
            }
        }
        internal DataGridRow OwningRow
        {
            get;
            set;
        }

        internal DataGrid OwningGrid
        {
            get { return OwningRow?.OwningGrid ?? OwningColumn?.OwningGrid; }
        }

        internal double ActualRightGridLineWidth
        {
            get { return _rightGridLine?.Bounds.Width ?? 0; }
        }

        internal int ColumnIndex
        {
            get { return OwningColumn?.Index ?? -1; }
        }

        internal int RowIndex
        {
            get { return OwningRow?.Index ?? -1; }
        }

        internal bool IsCurrent
        {
            get
            {
                return OwningGrid.CurrentColumnIndex == OwningColumn.Index &&
                       OwningGrid.CurrentSlot == OwningRow.Slot;
            }
        }

        private bool IsEdited
        {
            get
            {
                return OwningGrid.EditingRow == OwningRow &&
                       OwningGrid.EditingColumnIndex == ColumnIndex;
            }
        }

        private bool IsMouseOver
        {
            get
            {
                return OwningRow != null && OwningRow.MouseOverColumnIndex == ColumnIndex;
            }
            set
            {
                if (value != IsMouseOver)
                {
                    if (value)
                    {
                        OwningRow.MouseOverColumnIndex = ColumnIndex;
                    }
                    else
                    {
                        OwningRow.MouseOverColumnIndex = null;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the visual tree for the cell control when a new template is applied.
        /// </summary>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            UpdatePseudoClasses();
            _rightGridLine = e.NameScope.Find<Rectangle>(DATAGRIDCELL_elementRightGridLine);
            if (_rightGridLine != null && OwningColumn == null)
            {
                // Turn off the right GridLine for filler cells
                _rightGridLine.IsVisible = false;
            }
            else
            {
                EnsureGridLine(null);
            }

        }
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            if (OwningRow != null)
            {
                IsMouseOver = true;
            }
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);

            if (OwningRow != null)
            {
                IsMouseOver = false;
            }
        }

        //TODO TabStop
        private void DataGridCell_PointerPressed(PointerPressedEventArgs e)
        {
            // OwningGrid is null for TopLeftHeaderCell and TopRightHeaderCell because they have no OwningRow
            if (OwningGrid != null)
            {
                OwningGrid.OnCellPointerPressed(new DataGridCellPointerPressedEventArgs(this, OwningRow, OwningColumn, e));
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    if (!e.Handled)
                    //if (!e.Handled && OwningGrid.IsTabStop)
                    {
                        OwningGrid.Focus();
                    }
                    if (OwningRow != null)
                    {
                        e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, ColumnIndex, OwningRow.Slot, !e.Handled);
                        OwningGrid.UpdatedStateOnMouseLeftButtonDown = true;
                    }
                }
            }
        }

        internal void UpdatePseudoClasses()
        {
            if (OwningGrid == null || OwningColumn == null || OwningRow == null || !OwningRow.IsVisible || OwningRow.Slot == -1)
            {
                return;
            }

            PseudoClasses.Set(":selected", OwningRow.IsSelected);

            PseudoClasses.Set(":current", IsCurrent);

            PseudoClasses.Set(":edited", IsEdited);

            PseudoClasses.Set(":invalid", !IsValid);
        }

        // Makes sure the right gridline has the proper stroke and visibility. If lastVisibleColumn is specified, the 
        // right gridline will be collapsed if this cell belongs to the lastVisibleColumn and there is no filler column
        internal void EnsureGridLine(DataGridColumn lastVisibleColumn)
        {
            if (OwningGrid != null && _rightGridLine != null)
            {
                if (OwningGrid.VerticalGridLinesBrush != null && OwningGrid.VerticalGridLinesBrush != _rightGridLine.Fill)
                {
                    _rightGridLine.Fill = OwningGrid.VerticalGridLinesBrush;
                }

                bool newVisibility =
                    (OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.Vertical || OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.All)
                        && (OwningGrid.ColumnsInternal.FillerColumn.IsActive || OwningColumn != lastVisibleColumn);

                if (newVisibility != _rightGridLine.IsVisible)
                {
                    _rightGridLine.IsVisible = newVisibility;
                }
            }
        }

        private void OnOwningColumnSet(DataGridColumn column)
        {
            if (column == null)
            {
                Classes.Clear();
            }
            else
            {
                Classes.Replace(column.CellStyleClasses);
            }
        }
    }
}
