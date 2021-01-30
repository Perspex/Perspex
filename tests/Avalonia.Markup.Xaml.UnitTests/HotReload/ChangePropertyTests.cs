using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class ChangePropertyTests : HotReloadTestBase
    {
        [Fact]
        public void ChangeSingleProperty()
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
  <Border Background='Green' />
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Background");
            }
        }
        
        [Fact]
        public void ChangeMultipleProperties()
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
  <Border Background='Green' Padding='30' />
</UserControl>";
                
                Compare<TestControl>(xaml, modifiedXaml, @"
Content(Border)
  Background
  Padding");
            }
        }
        
        [Fact]
        public void NestedProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <ExperimentalAcrylicBorder Width='660' CornerRadius='5'>
    <ExperimentalAcrylicBorder.Material>
      <ExperimentalAcrylicMaterial
        TintColor='White'
        BackgroundSource='Digger' />
    </ExperimentalAcrylicBorder.Material>
  </ExperimentalAcrylicBorder>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <ExperimentalAcrylicBorder Width='660' CornerRadius='5'>
    <ExperimentalAcrylicBorder.Material>
      <ExperimentalAcrylicMaterial
        TintColor='Green'
        BackgroundSource='None' />
    </ExperimentalAcrylicBorder.Material>
  </ExperimentalAcrylicBorder>
</UserControl>";

                Compare<TestControl>(xaml, modifiedXaml, @"
Content(ExperimentalAcrylicBorder)
  Material(ExperimentalAcrylicMaterial)
    TintColor
    BackgroundSource");
            }
        }
    }
}
