using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class AddChildTests : HotReloadTestBase
    {
        [Fact]
        public void AddSingleChild()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'
             Padding='10' Margin='5'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20' Orientation='Vertical' />
  </Border>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'
             Padding='10' Margin='5'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20' Orientation='Vertical'>
      <TextBlock Text='TintOpacity' Foreground='Black' />
    </StackPanel>
  </Border>
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Padding
  HorizontalAlignment
  Child(StackPanel)
    Spacing
    Orientation
    Children.Count
    Children#0(TextBlock)
      Text
      Foreground
Padding
Margin");
            }
        }
        
        [Fact]
        public void AddChildToNonEmptyParentWithDifferentProperties()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'
             Padding='10' Margin='5'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='TintOpacity' Foreground='Black' />
    </StackPanel>
  </Border>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'
             Padding='10' Margin='5'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20'>
      <TextBlock Text='TintOpacity' Foreground='Black' />
      <TextBlock Text='TintOpacity' />
    </StackPanel>
  </Border>
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Padding
  HorizontalAlignment
  Child(StackPanel)
    Spacing
    Orientation
    Children.Count
    Children#0(TextBlock)
      Text
      Foreground
    Children#1(TextBlock)
      Text
Padding
Margin");
            }
        }
    }
}
