using System.Linq;

using Avalonia.Controls.Primitives;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class UniformGridTests
    {
        [Fact]
        public void UniformGrid_Columns_Equals_Rows_For_Auto_Columns_And_Rows()
        {
            var target = new UniformGrid()
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_Expands_Vertically_For_Columns_With_Auto_Rows()
        {
            var target = new UniformGrid()
            {
                Columns = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 3 grid
            Assert.Equal(new Size(2 * 80, 3 * 90), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_Extends_For_Columns_And_First_Column_With_Auto_Rows()
        {
            var target = new UniformGrid()
            {
                Columns = 3,
                FirstColumn = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 3 grid
            Assert.Equal(new Size(3 * 80, 3 * 90), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_Expands_Horizontally_For_Rows_With_Auto_Columns()
        {
            var target = new UniformGrid()
            {
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 2 grid
            Assert.Equal(new Size(3 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_Size_Is_Limited_By_Rows_And_Columns()
        {
            var target = new UniformGrid()
            {
                Columns = 2,
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_Not_Visible_Children_Are_Ignored()
        {
            var target = new UniformGrid()
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90, IsVisible = false },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 50, 2 * 70), target.Bounds.Size);
        }

        [Fact]
        public void UniformGrid_GetDimensions_NoElements()
        {
            var grid = new UniformGrid()
            {
                Children = { }
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(0, grid.Children.Count());

            var (rows, columns) = UniformGrid.GetDimensions(children, 0, 0, 0);

            Assert.Equal(1, rows);
            Assert.Equal(1, columns);
        }

        [Fact]
        public void UniformGrid_GetDimensions_AllVisible()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(8, grid.Children.Count());

            var (rows, columns) = UniformGrid.GetDimensions(children, 0, 0, 0);

            Assert.Equal(3, rows);
            Assert.Equal(3, columns);
        }

        [Fact]
        public void UniformGrid_GetDimensions_SomeVisible()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border() { IsVisible = false },
                    new Border(),
                    new Border() { IsVisible = false },
                    new Border(),
                    new Border() { IsVisible = false },
                    new Border(),
                    new Border() { IsVisible = false },
                }
            };

            var children = grid.Children.OfType<Control>();

            Assert.Equal(8, grid.Children.Count());

            // TODO: We don't expose this piece of the UniformGrid, but want to test this here for now.
            var visible = grid.Children.Where(item => item.IsVisible).OfType<Control>().ToArray();

            Assert.Equal(4, visible.Count());

            var (rows, columns) = UniformGrid.GetDimensions(visible, 0, 0, 0);

            Assert.Equal(2, rows);
            Assert.Equal(2, columns);
        }

        [Fact]
        public void UniformGrid_GetDimensions_FirstColumn()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(8, grid.Children.Count());

            var (rows, columns) = UniformGrid.GetDimensions(children, 0, 0, 2);

            Assert.Equal(4, rows);
            Assert.Equal(4, columns);
        }

        [Fact]
        public void UniformGrid_GetDimensions_ElementLarger()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border() { [Grid.RowSpanProperty] = 3, [Grid.ColumnSpanProperty] = 2 },
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            Assert.NotNull(grid);

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(8, grid.Children.Count());

            var (rows, columns) = UniformGrid.GetDimensions(children, 0, 0, 0);

            Assert.Equal(4, rows);
            Assert.Equal(4, columns);
        }

        [Fact]
        public void UniformGrid_GetDimensions_FirstColumnEqualsColumns()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(7, grid.Children.Count());

            // columns == first column
            // In WPF, First Column is ignored and we have a 1x7 layout.
            var (rows, columns) = UniformGrid.GetDimensions(children, 0, 7, 7);

            Assert.Equal(1, rows);
            Assert.Equal(7, columns);
        }

        [Fact]
        public void UniformGrid_SetupRowDefinitions_AllAutomatic()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // Normal Grid's don't have any definitions to start
            Assert.Equal(0, grid.RowDefinitions.Count);

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have three rows in this setup
            grid.SetupRowDefinitions(3);

            // We should now have our rows created
            Assert.Equal(3, grid.RowDefinitions.Count);

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }

            // We'll have three rows in this setup
            grid.SetupRowDefinitions(3);

            // We should now have our rows created
            Assert.Equal(3, grid.RowDefinitions.Count);

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupRowDefinitions_FirstFixed()
        {
            var grid = new UniformGrid()
            {
                RowDefinitions =
                {
                    new RowDefinition(48, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            Assert.NotNull(grid);

            // We should find our first definition
            Assert.Equal(1, grid.RowDefinitions.Count);
            Assert.Equal(48, grid.RowDefinitions[0].Height.Value);

            // We'll have three rows in this setup
            grid.SetupRowDefinitions(3);

            // We should now have our rows created
            Assert.Equal(3, grid.RowDefinitions.Count);

            var rdo = grid.RowDefinitions[0];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have three rows in this setup
            grid.SetupRowDefinitions(3);

            // We should now have our rows created
            Assert.Equal(3, grid.RowDefinitions.Count);

            rdo = grid.RowDefinitions[0];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupRowDefinitions_MiddleFixed()
        {
            var grid = new UniformGrid()
            {
                Rows = 5,
                RowDefinitions =
                {
                    new RowDefinition(48, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // We should find our first definition
            Assert.Equal(1, grid.RowDefinitions.Count);
            Assert.Equal(48, grid.RowDefinitions[0].Height.Value);

            Assert.Equal(5, grid.Rows);

            // We'll have five rows in this setup
            grid.SetupRowDefinitions(5);

            // We should now have our rows created
            Assert.Equal(5, grid.RowDefinitions.Count);

            // Our original definition should be at index 2
            var rdo = grid.RowDefinitions[0];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have five rows in this setup
            grid.SetupRowDefinitions(5);

            // We should now have our rows created
            Assert.Equal(5, grid.RowDefinitions.Count);

            // Our original definition should be at index 2
            rdo = grid.RowDefinitions[0];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.RowDefinitions.Count; i++)
            {
                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupRowDefinitions_MiddleAndEndFixed()
        {
            var grid = new UniformGrid()
            {
                Rows = 5,
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition(48, GridUnitType.Pixel),
                    new RowDefinition(),
                    new RowDefinition(128, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // We should find our first definition
            Assert.Equal(5, grid.RowDefinitions.Count);
            Assert.Equal(48, grid.RowDefinitions[2].Height.Value);
            Assert.Equal(128, grid.RowDefinitions[4].Height.Value);

            Assert.Equal(5, grid.Rows);

            // We'll have five rows in this setup
            grid.SetupRowDefinitions(5);

            // We should now have our rows created
            Assert.Equal(5, grid.RowDefinitions.Count);

            // Our original definition should be at index 2
            var rdo = grid.RowDefinitions[2];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Our 2nd original definition should be at index 4
            var rdo2 = grid.RowDefinitions[4];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo2));

            Assert.NotEqual(GridUnitType.Star, rdo2.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo2.Height.Value);

            // Check that we filled in the other two.
            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (i == 2 || i == 4)
                {
                    continue;
                }

                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(false, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have five rows in this setup
            grid.SetupRowDefinitions(5);

            // We should now have our rows created
            Assert.Equal(5, grid.RowDefinitions.Count);

            // Our original definition should be at index 2
            rdo = grid.RowDefinitions[2];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo.Height.Value);

            // Our 2nd original definition should be at index 4
            rdo2 = grid.RowDefinitions[4];

            // Did we mark that our row is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo2));

            Assert.NotEqual(GridUnitType.Star, rdo2.Height.GridUnitType);

            Assert.NotEqual(1.0, rdo2.Height.Value);

            // Check that we filled in the other two.
            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (i == 2 || i == 4)
                {
                    continue;
                }

                var rd = grid.RowDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(false, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, rd.Height.GridUnitType);

                Assert.Equal(1.0, rd.Height.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupColumnDefinitions_AllAutomatic()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // Normal Grid's don't have any definitions to start
            Assert.Equal(0, grid.ColumnDefinitions.Count);

            // We'll have three columns in this setup
            grid.SetupColumnDefinitions(3);

            // We should now have our columns created
            Assert.Equal(3, grid.ColumnDefinitions.Count);

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                var cd = grid.ColumnDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(cd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, cd.Width.GridUnitType);

                Assert.Equal(1.0, cd.Width.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have three columns in this setup
            grid.SetupColumnDefinitions(3);

            // We should now have our columns created
            Assert.Equal(3, grid.ColumnDefinitions.Count);

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                var cd = grid.ColumnDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(cd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, cd.Width.GridUnitType);

                Assert.Equal(1.0, cd.Width.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupColumnDefinitions_FirstFixed()
        {
            var grid = new UniformGrid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(48, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // We should find our first definition
            Assert.Equal(1, grid.ColumnDefinitions.Count);
            Assert.Equal(48, grid.ColumnDefinitions[0].Width.Value);

            // We'll have three columns in this setup
            grid.SetupColumnDefinitions(3);

            // We should now have our columns created
            Assert.Equal(3, grid.ColumnDefinitions.Count);

            var cdo = grid.ColumnDefinitions[0];

            // Did we mark that our column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(cdo));

            Assert.NotEqual(GridUnitType.Star, cdo.Width.GridUnitType);

            Assert.NotEqual(1.0, cdo.Width.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.ColumnDefinitions.Count; i++)
            {
                var cd = grid.ColumnDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(cd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, cd.Width.GridUnitType);

                Assert.Equal(1.0, cd.Width.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have three columns in this setup
            grid.SetupColumnDefinitions(3);

            // We should now have our columns created
            Assert.Equal(3, grid.ColumnDefinitions.Count);

            cdo = grid.ColumnDefinitions[0];

            // Did we mark that our column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(cdo));

            Assert.NotEqual(GridUnitType.Star, cdo.Width.GridUnitType);

            Assert.NotEqual(1.0, cdo.Width.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.ColumnDefinitions.Count; i++)
            {
                var cd = grid.ColumnDefinitions[i];

                // Check if we've setup our row to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(cd));

                // We need to be using '*' layout for all our rows to be even.
                Assert.Equal(GridUnitType.Star, cd.Width.GridUnitType);

                Assert.Equal(1.0, cd.Width.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupColumnDefinitions_MiddleFixed()
        {
            var grid = new UniformGrid()
            {
                Columns = 5,
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition(48, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // We should find our first definition
            Assert.Equal(3, grid.ColumnDefinitions.Count);
            Assert.Equal(48, grid.ColumnDefinitions[2].Width.Value);

            Assert.Equal(5, grid.Columns);

            // We'll have five Columns in this setup
            grid.SetupColumnDefinitions(5);

            // We should now have our Columns created
            Assert.Equal(5, grid.ColumnDefinitions.Count);

            // Our original definition should be at index 2
            var rdo = grid.ColumnDefinitions[2];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo.Width.Value);

            // Check that we filled in the other two.
            for (int i = 3; i < grid.ColumnDefinitions.Count; i++)
            {
                var rd = grid.ColumnDefinitions[i];

                // Check if we've setup our Column to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our Columns to be even.
                Assert.Equal(GridUnitType.Star, rd.Width.GridUnitType);

                Assert.Equal(1.0, rd.Width.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have five Columns in this setup
            grid.SetupColumnDefinitions(5);

            // We should now have our Columns created
            Assert.Equal(5, grid.ColumnDefinitions.Count);

            // Our original definition should be at index 2
            rdo = grid.ColumnDefinitions[2];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo.Width.Value);

            // Check that we filled in the other two.
            for (int i = 3; i < grid.ColumnDefinitions.Count; i++)
            {
                var rd = grid.ColumnDefinitions[i];

                // Check if we've setup our Column to automatically layout
                Assert.Equal(true, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our Columns to be even.
                Assert.Equal(GridUnitType.Star, rd.Width.GridUnitType);

                Assert.Equal(1.0, rd.Width.Value);
            }
        }

        [Fact]
        public void UniformGrid_SetupColumnDefinitions_FirstAndEndFixed()
        {
            var grid = new UniformGrid()
            {
                Columns = 5,
                ColumnDefinitions =
                {
                    new ColumnDefinition(48, GridUnitType.Pixel),
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition(128, GridUnitType.Pixel)
                },
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            // We should find our first definition
            Assert.Equal(5, grid.ColumnDefinitions.Count);
            Assert.Equal(48, grid.ColumnDefinitions[0].Width.Value);
            Assert.Equal(128, grid.ColumnDefinitions[4].Width.Value);

            Assert.Equal(5, grid.Columns);

            // We'll have five Columns in this setup
            grid.SetupColumnDefinitions(5);

            // We should now have our Columns created
            Assert.Equal(5, grid.ColumnDefinitions.Count);

            // Our original definition should be at index 2
            var rdo = grid.ColumnDefinitions[0];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo.Width.Value);

            // Our 2nd original definition should be at index 4
            var rdo2 = grid.ColumnDefinitions[4];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo2));
            ////Assert.Equal(4, UniformGrid.GetColumn(rdo2));

            Assert.NotEqual(GridUnitType.Star, rdo2.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo2.Width.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.ColumnDefinitions.Count - 1; i++)
            {
                var rd = grid.ColumnDefinitions[i];

                // Check if we've setup our Column to automatically layout
                Assert.Equal(false, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our Columns to be even.
                Assert.Equal(GridUnitType.Star, rd.Width.GridUnitType);

                Assert.Equal(1.0, rd.Width.Value);
            }

            //// DO IT AGAIN
            //// This is so we can check that it will behave the same again on another pass.

            // We'll have five Columns in this setup
            grid.SetupColumnDefinitions(5);

            // We should now have our Columns created
            Assert.Equal(5, grid.ColumnDefinitions.Count);

            // Our original definition should be at index 0
            rdo = grid.ColumnDefinitions[0];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo));

            Assert.NotEqual(GridUnitType.Star, rdo.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo.Width.Value);

            // Our 2nd original definition should be at index 4
            rdo2 = grid.ColumnDefinitions[4];

            // Did we mark that our Column is special?
            Assert.Equal(false, UniformGrid.GetAutoLayout(rdo2));

            Assert.NotEqual(GridUnitType.Star, rdo2.Width.GridUnitType);

            Assert.NotEqual(1.0, rdo2.Width.Value);

            // Check that we filled in the other two.
            for (int i = 1; i < grid.ColumnDefinitions.Count - 1; i++)
            {
                var rd = grid.ColumnDefinitions[i];

                // Check if we've setup our Column to automatically layout
                Assert.Equal(false, UniformGrid.GetAutoLayout(rd));

                // We need to be using '*' layout for all our Columns to be even.
                Assert.Equal(GridUnitType.Star, rd.Width.GridUnitType);

                Assert.Equal(1.0, rd.Width.Value);
            }
        }

        [Fact]
        public void UniformGrid_AutoLayout_FixedElementSingle()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border() { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1 },
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            var expected = new (int row, int col)[]
            {
                (0, 0),
                (1, 1),
                (0, 1),
                (0, 2),
                (1, 0),
                (1, 2),
                (2, 0),
                (2, 1)
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(8, grid.Children.Count);

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            // Check all children are in expected places.
            for (int i = 0; i < children.Count(); i++)
            {
                if (expected[i].row == 1 && expected[i].col == 1)
                {
                    // Check our fixed item isn't set to auto-layout.
                    Assert.Equal(false, UniformGrid.GetAutoLayout(children[i]));
                }

                Assert.Equal(expected[i].row, Grid.GetRow(children[i]));
                Assert.Equal(expected[i].col, Grid.GetColumn(children[i]));
            }
        }

        [Fact]
        public void UniformGrid_AutoLayout_FixedElementZeroZeroSpecial()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border() { [Grid.RowProperty] = 0, [Grid.ColumnProperty] = 0, [UniformGrid.AutoLayoutProperty] = false },
                    new Border()
                }
            };

            var expected = new (int row, int col)[]
            {
                (0, 1),
                (0, 2),
                (1, 0),
                (1, 1),
                (1, 2),
                (2, 0),
                (0, 0),
                (2, 1)
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(8, grid.Children.Count);

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            // Check all children are in expected places.
            for (int i = 0; i < children.Count(); i++)
            {
                if (expected[i].row == 0 && expected[i].col == 0)
                {
                    // Check our fixed item isn't set to auto-layout.
                    Assert.Equal(false, UniformGrid.GetAutoLayout(children[i]));
                }

                Assert.Equal(expected[i].row, Grid.GetRow(children[i]));
                Assert.Equal(expected[i].col, Grid.GetColumn(children[i]));
            }
        }

        [Fact]
        public void UniformGrid_AutoLayout_FixedElementSquare()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border() { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1, [Grid.RowSpanProperty] = 2, [Grid.ColumnSpanProperty] = 2 },
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            var expected = new (int row, int col)[]
            {
                (0, 0),
                (1, 1),
                (0, 1),
                (0, 2),
                (0, 3),
                (1, 0),
                (1, 3),
                (2, 0),
                (2, 3)
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(9, grid.Children.Count);

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            // Check all children are in expected places.
            for (int i = 0; i < children.Count(); i++)
            {
                if (expected[i].row == 1 && expected[i].col == 1)
                {
                    // Check our fixed item isn't set to auto-layout.
                    Assert.Equal(false, UniformGrid.GetAutoLayout(children[i]));
                }

                Assert.Equal(expected[i].row, Grid.GetRow(children[i]));
                Assert.Equal(expected[i].col, Grid.GetColumn(children[i]));
            }
        }

        [Fact]
        public void UniformGrid_AutoLayout_VerticalElement_FixedPosition()
        {
            Border ourItem, shifted;
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    (ourItem = new Border() { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1, [Grid.RowSpanProperty] = 2 }),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    (shifted = new Border())
                }
            };

            Assert.Equal(8, grid.Children.Count());

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            Assert.Equal(1, Grid.GetRow(ourItem));
            Assert.Equal(1, Grid.GetColumn(ourItem));

            Assert.Equal(2, Grid.GetRow(shifted));
            Assert.Equal(2, Grid.GetColumn(shifted));
        }

        [Fact]
        public void UniformGrid_AutoLayout_VerticalElement()
        {
            Border ourItem, shifted;
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    (ourItem = new Border() { [Grid.RowSpanProperty] = 2 }),
                    new Border(),
                    new Border(),
                    (shifted = new Border())
                }
            };

            Assert.Equal(8, grid.Children.Count());

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            Assert.Equal(1, Grid.GetRow(ourItem));
            Assert.Equal(1, Grid.GetColumn(ourItem));

            Assert.Equal(2, Grid.GetRow(shifted));
            Assert.Equal(2, Grid.GetColumn(shifted));
        }

        [Fact]
        public void UniformGrid_AutoLayout_HorizontalElement()
        {
            Border ourItem, shifted;
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    (ourItem = new Border() { [Grid.ColumnSpanProperty] = 2 }),
                    (shifted = new Border()),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            Assert.Equal(8, grid.Children.Count());

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            Assert.Equal(0, Grid.GetRow(ourItem));
            Assert.Equal(1, Grid.GetColumn(ourItem));

            Assert.Equal(1, Grid.GetRow(shifted));
            Assert.Equal(0, Grid.GetColumn(shifted));
        }

        [Fact]
        public void UniformGrid_AutoLayout_LargeElement()
        {
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border() { [Grid.ColumnSpanProperty] = 2, [Grid.RowSpanProperty] = 2 },
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border()
                }
            };

            var expected = new (int row, int col)[]
            {
                (0, 0),
                (0, 2),
                (1, 2),
                (2, 0),
                (2, 1),
                (2, 2),
            };

            var children = grid.Children.OfType<Control>().ToArray();

            Assert.Equal(6, grid.Children.Count());

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            // Check all children are in expected places.
            for (int i = 0; i < children.Count(); i++)
            {
                Assert.Equal(expected[i].row, Grid.GetRow(children[i]));
                Assert.Equal(expected[i].col, Grid.GetColumn(children[i]));
            }
        }

        [Fact]
        public void UniformGrid_AutoLayout_HorizontalElement_FixedPosition()
        {
            Border ourItem, shifted;
            var grid = new UniformGrid()
            {
                Children =
                {
                    new Border(),
                    (ourItem = new Border() { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1, [Grid.ColumnSpanProperty] = 2 }),
                    new Border(),
                    new Border(),
                    new Border(),
                    (shifted = new Border()),
                    new Border(),
                    new Border(),
                }
            };

            Assert.Equal(8, grid.Children.Count());

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(grid.DesiredSize));

            Assert.Equal(1, Grid.GetRow(ourItem));
            Assert.Equal(1, Grid.GetColumn(ourItem));

            Assert.Equal(2, Grid.GetRow(shifted));
            Assert.Equal(0, Grid.GetColumn(shifted));
        }
    }
}
