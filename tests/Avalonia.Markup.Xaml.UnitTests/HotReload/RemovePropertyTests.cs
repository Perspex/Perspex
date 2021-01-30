using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class RemovePropertyTests : HotReloadTestBase
    {
        [Fact]
        public void RemoveSingleProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Background='Yellow' />
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border />
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Background");
            }
        }
        
        [Fact]
        public void RemoveMultipleProperties()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Background='Yellow' Padding='20' />
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border />
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Background
  Padding");
            }
        }
        
        [Fact]
        public void RemovePropertyWithSameTypeSiblings()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock Text='Text' Foreground='Yellow' />
    <TextBlock Text='Text' Foreground='Yellow' />
  </StackPanel>
</UserControl>";

                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock Text='Text' />
    <TextBlock Text='Text' Foreground='Yellow' />
  </StackPanel>
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(StackPanel)
  Children#0(TextBlock)
    Text
    Foreground
  Children#1(TextBlock)
    Text
    Foreground");
            }
        }
    }
}
