using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class RemoveChildTests : HotReloadTestBase
    {
        [Fact]
        public void RemoveOnlyChildFromCollection()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='Text' />
    </StackPanel>
  </Border>
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20' />
  </Border>
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Child(StackPanel)
    Children.Count");
            }
        }

        [Fact]
        public void RemoveChildDirectProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20' />
  </Border>
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center' />
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Child");
            }
        }

        [Fact]
        public void RemoveFirstChildWithSameTypeSiblings()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='TintOpacity' Foreground='Black' />
      <TextBlock Text='Text' />
    </StackPanel>
  </Border>
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='Text' />
    </StackPanel>
  </Border>
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Child(StackPanel)
    Children.Count
    Children#0(TextBlock)
      Text");
            }
        }

        [Fact]
        public void RemoveChildThatHasChildren()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='Text' />
      <TextBlock Text='Text' />
    </StackPanel>
  </StackPanel>
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel HorizontalAlignment='Center' />
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(StackPanel)
  Children.Count");
            }
        }
    }
}
