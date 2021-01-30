using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class AddPropertyTests : HotReloadTestBase
    {
        [Fact]
        public void AddSimpleProperties()
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
  <Border Padding='20' HorizontalAlignment='Center' Background='Yellow' />
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Padding
  HorizontalAlignment
  Background");
            }
        }
        
        [Fact]
        public void AddPropertiesWithSameTypeSiblings()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock Foreground='Yellow' />
    <TextBlock />
  </StackPanel>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock Foreground='Yellow' Text='Text' />
    <TextBlock Foreground='Green' />
  </StackPanel>
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(StackPanel)
  Children#0(TextBlock)
    Foreground
    Text
  Children#1(TextBlock)
    Foreground");
            }
        }
        
        [Fact]
        public void AddExpandedProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock />
  </StackPanel>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <StackPanel>
    <TextBlock>
      <TextBlock.Styles>
        <Style Selector='TextBlock.h1'>
          <Setter Property='Margin' Value='50' />
        </Style>
      </TextBlock.Styles>
    </TextBlock>
  </StackPanel>
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(StackPanel)
  Children#0(TextBlock)
    Styles.Count
    Styles#0
      Setters#0
        Property
        Value");
            }
        }
    }
}
