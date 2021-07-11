// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The UniformGrid control presents information within a Grid with even spacing.
    /// </summary>
    public partial class UniformGrid : Grid
    {
        // Provides the next spot in the boolean array with a 'false' value.
        internal static IEnumerable<(int row, int column)> GetFreeSpot(TakenSpotsReferenceHolder arrayref, int firstcolumn, bool topdown)
        {
            if (topdown)
            {
                var rows = arrayref.Height;

                // Layout spots from Top-Bottom, Left-Right (right-left handled automatically by Grid with Flow-Direction).
                // Effectively transpose the Grid Layout.
                for (int c = 0; c < arrayref.Width; c++)
                {
                    int start = (c == 0 && firstcolumn > 0 && firstcolumn < rows) ? firstcolumn : 0;
                    for (int r = start; r < rows; r++)
                    {
                        if (!arrayref[r, c])
                        {
                            yield return (r, c);
                        }
                    }
                }
            }
            else
            {
                var columns = arrayref.Width;

                // Layout spots as normal from Left-Right.
                // (right-left handled automatically by Grid with Flow-Direction
                // during its layout, internal model is always left-right).
                for (int r = 0; r < arrayref.Height; r++)
                {
                    int start = (r == 0 && firstcolumn > 0 && firstcolumn < columns) ? firstcolumn : 0;
                    for (int c = start; c < columns; c++)
                    {
                        if (!arrayref[r, c])
                        {
                            yield return (r, c);
                        }
                    }
                }
            }
        }

        // Based on the number of visible children,
        // returns the dimensions of the
        // grid we need to hold all elements.
        internal static (int rows, int columns) GetDimensions(Control[] visible, int rows, int cols, int firstColumn)
        {
            // If a dimension isn't specified, we need to figure out the other one (or both).
            if (rows == 0 || cols == 0)
            {
                // Calculate the size & area of all objects in the grid to know how much space we need.
                var count = Math.Max(1, visible.Sum(item => GetRowSpan(item) * GetColumnSpan(item)));

                if (rows == 0)
                {
                    if (cols > 0)
                    {
                        // Bound check
                        var first = (firstColumn >= cols || firstColumn < 0) ? 0 : firstColumn;

                        // If we have columns but no rows, calculate rows based on column offset and number of children.
                        rows = (count + first + (cols - 1)) / cols;
                        return (rows, cols);
                    }
                    else
                    {
                        // Otherwise, determine square layout if both are zero.
                        var size = (int)Math.Ceiling(Math.Sqrt(count));

                        // Figure out if firstColumn is in bounds
                        var first = (firstColumn >= size || firstColumn < 0) ? 0 : firstColumn;

                        rows = (int)Math.Ceiling(Math.Sqrt(count + first));
                        return (rows, rows);
                    }
                }
                else if (cols == 0)
                {
                    // If we have rows and no columns, then calculate columns needed based on rows
                    cols = (count + (rows - 1)) / rows;

                    // Now that we know a rough size of our shape, see if the FirstColumn effects that:
                    var first = (firstColumn >= cols || firstColumn < 0) ? 0 : firstColumn;

                    cols = (count + first + (rows - 1)) / rows;
                }
            }

            return (rows, cols);
        }

        // Used to interleave specified row dimensions with automatic rows added to use
        // underlying Grid layout for main arrange of UniformGrid.
        internal void SetupRowDefinitions(int rows)
        {
            // Mark initial definitions so we don't erase them.
            foreach (var rd in RowDefinitions)
            {
                if (GetAutoLayout(rd) == null)
                {
                    SetAutoLayout(rd, false);
                }
            }

            // Remove non-autolayout rows we've added and then add them in the right spots.
            if (rows != RowDefinitions.Count)
            {
                for (int r = RowDefinitions.Count - 1; r >= 0; r--)
                {
                    if (GetAutoLayout(RowDefinitions[r]) == true)
                    {
                        RowDefinitions.RemoveAt(r);
                    }
                }

                for (int r = RowDefinitions.Count; r < rows; r++)
                {
                    var rd = new RowDefinition();
                    SetAutoLayout(rd, true);
                    RowDefinitions.Insert(r, rd);
                }
            }
        }

        // Used to interleave specified column dimensions with automatic columns added to use
        // underlying Grid layout for main arrange of UniformGrid.
        internal void SetupColumnDefinitions(int columns)
        {
            // Mark initial definitions so we don't erase them.
            foreach (var cd in ColumnDefinitions)
            {
                if (GetAutoLayout(cd) == null)
                {
                    SetAutoLayout(cd, false);
                }
            }

            // Remove non-autolayout columns we've added and then add them in the right spots.
            if (columns != ColumnDefinitions.Count)
            {
                for (int c = ColumnDefinitions.Count - 1; c >= 0; c--)
                {
                    if (GetAutoLayout(ColumnDefinitions[c]) == true)
                    {
                        ColumnDefinitions.RemoveAt(c);
                    }
                }

                for (int c = ColumnDefinitions.Count; c < columns; c++)
                {
                    var cd = new ColumnDefinition();
                    SetAutoLayout(cd, true);
                    ColumnDefinitions.Insert(c, cd);
                }
            }
        }

        /// <summary>
        /// Referencable class object we can use to have a reference shared between
        /// our <see cref="UniformGrid.MeasureOverride"/> and
        /// <see cref="UniformGrid.GetFreeSpot"/> iterator.
        /// This is used so we can better isolate our logic and make it easier to test.
        /// </summary>
        internal sealed class TakenSpotsReferenceHolder
        {
            /// <summary>
            /// The <see cref="BitArray"/> instance used to efficiently track empty spots.
            /// </summary>
            private readonly BitArray spotsTaken;

            /// <summary>
            /// Initializes a new instance of the <see cref="TakenSpotsReferenceHolder"/> class.
            /// </summary>
            /// <param name="rows">The number of rows to track.</param>
            /// <param name="columns">The number of columns to track.</param>
            public TakenSpotsReferenceHolder(int rows, int columns)
            {
                if (rows < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(rows));
                }
                if (columns < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(columns));
                }

                Height = rows;
                Width = columns;

                spotsTaken = new BitArray(rows * columns);
            }

            /// <summary>
            /// Gets the height of the grid to monitor.
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// Gets the width of the grid to monitor.
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// Gets or sets the value of a specified grid cell.
            /// </summary>
            /// <param name="i">The vertical offset.</param>
            /// <param name="j">The horizontal offset.</param>
            public bool this[int i, int j]
            {
                get => spotsTaken[(i * Width) + j];
                set => spotsTaken[(i * Width) + j] = value;
            }

            /// <summary>
            /// Fills the specified area in the current grid with a given value.
            /// If invalid coordinates are given, they will simply be ignored and no exception will be thrown.
            /// </summary>
            /// <param name="value">The value to fill the target area with.</param>
            /// <param name="row">The row to start on (inclusive, 0-based index).</param>
            /// <param name="column">The column to start on (inclusive, 0-based index).</param>
            /// <param name="width">The positive width of area to fill.</param>
            /// <param name="height">The positive height of area to fill.</param>
            public void Fill(bool value, int row, int column, int width, int height)
            {
                var bounds = new Rectangle(0, 0, Width, Height);

                // Precompute bounds to skip branching in main loop
                bounds.Intersect(new Rectangle(column, row, width, height));

                for (int i = (int)bounds.Top; i < (int)bounds.Bottom; i++)
                {
                    for (int j = (int)bounds.Left; j < (int)bounds.Right; j++)
                    {
                        this[i, j] = value;
                    }
                }
            }

            /// <summary>
            /// Resets the current reference holder.
            /// </summary>
            public void Reset()
            {
                spotsTaken.SetAll(false);
            }
        }
    }
}
