// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Avalonia.Layout;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The UniformGrid spaces items evenly.
    /// </summary>
    public partial class UniformGrid
    {
        /// <summary>
        /// Determines if this element in the grid participates in the auto-layout algorithm.
        /// </summary>
        public static readonly AttachedProperty<bool?> AutoLayoutProperty =
            AvaloniaProperty.RegisterAttached<UniformGrid, Control, bool?>(
                "AutoLayout",
                defaultValue: null);

        /// <summary>
        /// Sets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// Though it its required to use this property to force an element to the 0, 0 position.
        /// </summary>
        /// <param name="element"><see cref="Control"/></param>
        /// <param name="value">A true value indicates this item should be automatically arranged.</param>
        public static void SetAutoLayout(Control element, bool? value)
        {
            element.SetValue(AutoLayoutProperty, value);
        }

        /// <summary>
        /// Gets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// </summary>
        /// <param name="element"><see cref="Control"/></param>
        /// <returns>A true value indicates this item should be automatically arranged.</returns>
        public static bool? GetAutoLayout(Control element)
        {
            return element.GetValue(AutoLayoutProperty);
        }

        /// <summary>
        /// Sets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// </summary>
        /// <param name="element"><see cref="ColumnDefinition"/></param>
        /// <param name="value">A true value indicates this item should be automatically arranged.</param>
        internal static void SetAutoLayout(ColumnDefinition element, bool? value)
        {
            element.SetValue(AutoLayoutProperty, value);
        }

        /// <summary>
        /// Gets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// </summary>
        /// <param name="element"><see cref="ColumnDefinition"/></param>
        /// <returns>A true value indicates this item should be automatically arranged.</returns>
        internal static bool? GetAutoLayout(ColumnDefinition element)
        {
            return element.GetValue(AutoLayoutProperty);
        }

        /// <summary>
        /// Sets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// </summary>
        /// <param name="element"><see cref="RowDefinition"/></param>
        /// <param name="value">A true value indicates this item should be automatically arranged.</param>
        internal static void SetAutoLayout(RowDefinition element, bool? value)
        {
            element.SetValue(AutoLayoutProperty, value);
        }

        /// <summary>
        /// Gets the AutoLayout Attached Property Value. Used internally to track items which need to be arranged vs. fixed in place.
        /// </summary>
        /// <param name="element"><see cref="RowDefinition"/></param>
        /// <returns>A true value indicates this item should be automatically arranged.</returns>
        internal static bool? GetAutoLayout(RowDefinition element)
        {
            return element.GetValue(AutoLayoutProperty);
        }

        /// <summary>
        /// Identifies the <see cref="Columns"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Columns), 0);

        /// <summary>
        /// Identifies the <see cref="FirstColumn"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> FirstColumnProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(FirstColumn), 0);

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<UniformGrid, Orientation>(nameof(Orientation), Orientation.Horizontal);

        /// <summary>
        /// Identifies the <see cref="Rows"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Rows), 0);

        /// <summary>
        /// Gets or sets the number of columns in the UniformGrid.
        /// </summary>
        public int Columns
        {
            get { return GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the starting column offset for the first row of the UniformGrid.
        /// </summary>
        public int FirstColumn
        {
            get { return GetValue(FirstColumnProperty); }
            set { SetValue(FirstColumnProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation of the grid. When <see cref="Orientation.Vertical"/>,
        /// will transpose the layout of automatically arranged items such that they start from
        /// top to bottom then based on <see cref="FlowDirection"/>.
        /// Defaults to <see cref="Orientation.Horizontal"/>.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of rows in the UniformGrid.
        /// </summary>
        public int Rows
        {
            get { return GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }
    }
}
