using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.UnitTests;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BasicTests : XamlTestBase
    {
        [Fact]
        public void Simple_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui' Content='Foo'/>";

            var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void Default_Content_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui'>Foo</ContentControl>";

            var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void Attached_Property_Is_Set()
        {
            var xaml =
        @"<ContentControl xmlns='https://github.com/avaloniaui' TextBlock.FontSize='21'/>";

            var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal(21.0, TextBlock.GetFontSize(target));
        }

        [Fact]
        public void Attached_Property_Is_Set_On_Control_Outside_Avalonia_Namspace()
        {
            // Test for issue #1548
            var xaml =
@"<UserControl xmlns='https://github.com/avaloniaui'
    xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
  <local:TestControl Grid.Column='2' />
</UserControl>";

            var target = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);

            Assert.Equal(2, Grid.GetColumn((TestControl)target.Content));
        }

        [Fact]
        public void Attached_Property_With_Namespace_Is_Set()
        {
            var xaml =
                @"<ContentControl xmlns='https://github.com/avaloniaui' 
                    xmlns:test='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
                    test:BasicTestsAttachedPropertyHolder.Foo='Bar'/>";

            var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal("Bar", BasicTestsAttachedPropertyHolder.GetFoo(target));
        }

        [Fact]
        public void Attached_Property_Supports_Binding()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml =
            @"<Window xmlns='https://github.com/avaloniaui' TextBlock.FontSize='{Binding}'/>";

                var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

                target.DataContext = 21.0;

                Assert.Equal(21.0, TextBlock.GetFontSize(target));
            }
        }

        [Fact]
        public void Attached_Property_In_Panel_Is_Set()
        {
            var xaml = @"
<Panel xmlns='https://github.com/avaloniaui'>
    <ToolTip.Tip>Foo</ToolTip.Tip>
</Panel>";

            var target = AvaloniaRuntimeXamlLoader.Parse<Panel>(xaml);

            Assert.Empty(target.Children);

            Assert.Equal("Foo", ToolTip.GetTip(target));
        }
        
        [Fact]
        public void NonExistent_Property_Throws()
        {
            var xaml =
        @"<ContentControl xmlns='https://github.com/avaloniaui' DoesntExist='foo'/>";

            XamlTestHelpers.AssertThrowsXamlException(() => AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml));
        }

        [Fact]
        public void ContentControl_ContentTemplate_Is_Functional()
        {
            var xaml =
@"<ContentControl xmlns='https://github.com/avaloniaui'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock Text='Foo' />
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

            var contentControl = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);
            var target = contentControl.ContentTemplate;

            Assert.NotNull(target);

            var txt = (TextBlock)target.Build(null);

            Assert.Equal("Foo", txt.Text);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'>
    <Button Name='button'>Foo</Button>
</UserControl>";

            var control = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Direct_Content_In_ItemsControl_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
     <ItemsControl Name='items'>
         <ContentControl>Foo</ContentControl>
         <ContentControl>Bar</ContentControl>
      </ItemsControl>
</Window>";

                var control = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);

                var itemsControl = control.FindControl<ItemsControl>("items");

                Assert.NotNull(itemsControl);

                var items = itemsControl.Items.Cast<ContentControl>().ToArray();

                Assert.Equal("Foo", items[0].Content);
                Assert.Equal("Bar", items[1].Content);
            }
        }

        [Fact]
        public void Panel_Children_Are_Added()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'>
    <Panel Name='panel'>
        <ContentControl Name='Foo' />
        <ContentControl Name='Bar' />
    </Panel>
</UserControl>";

            var control = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);

            var panel = control.FindControl<Panel>("panel");

            Assert.Equal(2, panel.Children.Count);

            var foo = control.FindControl<ContentControl>("Foo");
            var bar = control.FindControl<ContentControl>("Bar");

            Assert.Contains(foo, panel.Children);
            Assert.Contains(bar, panel.Children);
        }

        [Fact]
        public void Grid_Row_Col_Definitions_Are_Built()
        {
            var xaml = @"
<Grid xmlns='https://github.com/avaloniaui'>
    <Grid.ColumnDefinitions>
        <ColumnDefinitions>
            <ColumnDefinition Width='100' />
            <ColumnDefinition Width='Auto' />
            <ColumnDefinition Width='*' />
            <ColumnDefinition Width='100*' />
        </ColumnDefinitions>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinitions>
            <RowDefinition Height='100' />
            <RowDefinition Height='Auto' />
            <RowDefinition Height='*' />
            <RowDefinition Height='100*' />
        </RowDefinitions>
    </Grid.RowDefinitions>
</Grid>";

            var grid = AvaloniaRuntimeXamlLoader.Parse<Grid>(xaml);

            Assert.Equal(4, grid.ColumnDefinitions.Count);
            Assert.Equal(4, grid.RowDefinitions.Count);

            var expected1 = new GridLength(100);
            var expected2 = GridLength.Auto;
            var expected3 = new GridLength(1, GridUnitType.Star);
            var expected4 = new GridLength(100, GridUnitType.Star);

            Assert.Equal(expected1, grid.ColumnDefinitions[0].Width);
            Assert.Equal(expected2, grid.ColumnDefinitions[1].Width);
            Assert.Equal(expected3, grid.ColumnDefinitions[2].Width);
            Assert.Equal(expected4, grid.ColumnDefinitions[3].Width);

            Assert.Equal(expected1, grid.RowDefinitions[0].Height);
            Assert.Equal(expected2, grid.RowDefinitions[1].Height);
            Assert.Equal(expected3, grid.RowDefinitions[2].Height);
            Assert.Equal(expected4, grid.RowDefinitions[3].Height);
        }

        [Fact]
        public void Grid_Row_Col_Definitions_Are_Parsed()
        {
            var xaml = @"
<Grid xmlns='https://github.com/avaloniaui'
        ColumnDefinitions='100,Auto,*,100*'
        RowDefinitions='100,Auto,*,100*'>
</Grid>";

            var grid = AvaloniaRuntimeXamlLoader.Parse<Grid>(xaml);


            Assert.Equal(4, grid.ColumnDefinitions.Count);
            Assert.Equal(4, grid.RowDefinitions.Count);

            var expected1 = new GridLength(100);
            var expected2 = GridLength.Auto;
            var expected3 = new GridLength(1, GridUnitType.Star);
            var expected4 = new GridLength(100, GridUnitType.Star);

            Assert.Equal(expected1, grid.ColumnDefinitions[0].Width);
            Assert.Equal(expected2, grid.ColumnDefinitions[1].Width);
            Assert.Equal(expected3, grid.ColumnDefinitions[2].Width);
            Assert.Equal(expected4, grid.ColumnDefinitions[3].Width);

            Assert.Equal(expected1, grid.RowDefinitions[0].Height);
            Assert.Equal(expected2, grid.RowDefinitions[1].Height);
            Assert.Equal(expected3, grid.RowDefinitions[2].Height);
            Assert.Equal(expected4, grid.RowDefinitions[3].Height);
        }

        [Fact]
        public void ControlTemplate_With_Nested_Child_Is_Operational()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'>
    <ContentControl Name='parent'>
        <ContentControl Name='child' />
    </ContentControl>
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            var parent = (ContentControl)template.Build(new ContentControl()).Control;

            Assert.Equal("parent", parent.Name);

            var child = parent.Content as ContentControl;

            Assert.NotNull(child);

            Assert.Equal("child", child.Name);
        }

        [Fact]
        public void ControlTemplate_With_TargetType_Is_Operational()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='{x:Type ContentControl}'>
    <ContentPresenter Content='{TemplateBinding Content}' />
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            Assert.Equal(typeof(ContentControl), template.TargetType);

            Assert.IsType(typeof(ContentPresenter), template.Build(new ContentControl()).Control);
        }

        [Fact]
        public void ControlTemplate_With_Panel_Children_Are_Added()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'>
    <Panel Name='panel'>
        <ContentControl Name='Foo' />
        <ContentControl Name='Bar' />
    </Panel>
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            var panel = (Panel)template.Build(new ContentControl()).Control;

            Assert.Equal(2, panel.Children.Count);

            var foo = panel.Children[0];
            var bar = panel.Children[1];

            Assert.Equal("Foo", foo.Name);
            Assert.Equal("Bar", bar.Name);
        }

        [Fact]
        public void Named_x_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='button'>Foo</Button>
</UserControl>";

            var control = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Standart_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Width='200.5' />";

            var control = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);
            Assert.Equal(200.5, control.Width);
        }

        [Fact]
        public void Avalonia_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Background='White' />";

            var control = AvaloniaRuntimeXamlLoader.Parse<UserControl>(xaml);
            var bk = control.Background;
            Assert.IsType<ImmutableSolidColorBrush>(bk);
            Assert.Equal(Colors.White, (bk as ISolidColorBrush).Color);
        }

        [Fact]
        public void Simple_Style_Is_Parsed()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='TextBlock'>
        <Setter Property='Background' Value='White'/>
        <Setter Property='Width' Value='100'/>
    </Style>
</Styles>";

            var styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);

            Assert.Single(styles);

            var style = (Style)styles[0];

            var setters = style.Setters.Cast<Setter>().ToArray();

            Assert.Equal(2, setters.Length);

            Assert.Equal(TextBlock.BackgroundProperty, setters[0].Property);
            Assert.Equal(Brushes.White.Color, ((ISolidColorBrush)setters[0].Value).Color);

            Assert.Equal(TextBlock.WidthProperty, setters[1].Property);
            Assert.Equal(100.0, setters[1].Value);
        }

        [Fact]
        public void Style_Setter_With_AttachedProperty_Is_Parsed()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='ContentControl'>
        <Setter Property='TextBlock.FontSize' Value='21'/>
    </Style>
</Styles>";

            var styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);

            Assert.Single(styles);

            var style = (Style)styles[0];

            var setters = style.Setters.Cast<Setter>().ToArray();

            Assert.Single(setters);

            Assert.Equal(TextBlock.FontSizeProperty, setters[0].Property);
            Assert.Equal(21.0, setters[0].Value);
        }

        [Fact]
        public void Complex_Style_Is_Parsed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'>
  <Style Selector='CheckBox'>
    <Setter Property='BorderBrush' Value='{DynamicResource ThemeBorderMidBrush}'/>
    <Setter Property='BorderThickness' Value='{DynamicResource ThemeBorderThickness}'/>
    <Setter Property='Template'>
      <ControlTemplate>
        <Grid ColumnDefinitions='Auto,*'>
          <Border Name='border'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  Width='18'
                  Height='18'
                  VerticalAlignment='Center'>
            <Path Name='checkMark'
                  Fill='{StaticResource HighlightBrush}'
                  Width='11'
                  Height='10'
                  Stretch='Uniform'
                  HorizontalAlignment='Center'
                  VerticalAlignment='Center'
                  Data='M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z'/>
          </Border>
          <ContentPresenter Name='PART_ContentPresenter'
                            Content='{TemplateBinding Content}'
                            ContentTemplate='{TemplateBinding ContentTemplate}'
                            Margin='4,0,0,0'
                            VerticalAlignment='Center'
                            Grid.Column='1'/>
        </Grid>
      </ControlTemplate>
    </Setter>
  </Style>
</Styles>
";
                var styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);

                Assert.Single(styles);

                var style = (Style)styles[0];

                var setters = style.Setters.Cast<Setter>().ToArray();

                Assert.Equal(3, setters.Length);

                Assert.Equal(CheckBox.BorderBrushProperty, setters[0].Property);
                Assert.Equal(CheckBox.BorderThicknessProperty, setters[1].Property);
                Assert.Equal(CheckBox.TemplateProperty, setters[2].Property);

                Assert.IsType<ControlTemplate>(setters[2].Value);
            }
        }

        [Fact]
        public void Style_Resources_Are_Built()
        {
            var xaml = @"
<Style xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:sys='clr-namespace:System;assembly=netstandard'>
    <Style.Resources>
        <SolidColorBrush x:Key='Brush'>White</SolidColorBrush>
        <sys:Double x:Key='Double'>10</sys:Double>
    </Style.Resources>
</Style>";

            var style = AvaloniaRuntimeXamlLoader.Parse<Style>(xaml);

            Assert.True(style.Resources.Count > 0);

            style.TryGetResource("Brush", out var brush);

            Assert.NotNull(brush);
            Assert.IsType<SolidColorBrush>(brush);
            Assert.Equal(Colors.White, ((ISolidColorBrush)brush).Color);

            style.TryGetResource("Double", out var d);

            Assert.Equal(10.0, d);
        }

        [Fact]
        public void StyleInclude_Is_Built()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StyleInclude Source='resm:Avalonia.Themes.Default.ContextMenu.xaml?assembly=Avalonia.Themes.Default'/>
</Styles>";

                var styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);

                Assert.True(styles.Count == 1);

                var styleInclude = styles.First() as StyleInclude;

                Assert.NotNull(styleInclude);

                var style = styleInclude.Loaded;

                Assert.NotNull(style);
            }
        }

        [Fact]
        public void Simple_Xaml_Binding_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
                                    .With(windowingPlatform: new MockWindowingPlatform())))
            {
                var xaml =
@"<Window xmlns='https://github.com/avaloniaui' Content='{Binding}'/>";

                var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

                Assert.Null(target.Content);

                target.DataContext = "Foo";

                Assert.Equal("Foo", target.Content);
            }
        }

        [Fact]
        public void Double_Xaml_Binding_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
                                    .With(windowingPlatform: new MockWindowingPlatform())))
            {
                var xaml =
@"<Window xmlns='https://github.com/avaloniaui' Width='{Binding}'/>";

                var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

                Assert.Null(target.Content);

                target.DataContext = 55.0;

                Assert.Equal(55.0, target.Width);
            }
        }

        [Fact]
        public void Collection_Xaml_Binding_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
                                    .With(windowingPlatform: new MockWindowingPlatform())))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <ItemsControl Name='itemsControl' Items='{Binding}'>
    </ItemsControl>
</Window>
";

                var target = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);

                Assert.NotNull(target.Content);

                var itemsControl = target.FindControl<ItemsControl>("itemsControl");

                var items = new string[] { "Foo", "Bar" };

                //DelayedBinding.ApplyBindings(itemsControl);

                target.DataContext = items;

                Assert.Equal(items, itemsControl.Items);
            }
        }

        [Fact]
        public void Multi_Xaml_Binding_Is_Parsed()
        {
            var xaml =
@"<MultiBinding xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    Converter ='{x:Static BoolConverters.And}'>
     <Binding Path='Foo' />
     <Binding Path='Bar' />
</MultiBinding>";

            var target = AvaloniaRuntimeXamlLoader.Parse<MultiBinding>(xaml);

            Assert.Equal(2, target.Bindings.Count);

            Assert.Equal(BoolConverters.And, target.Converter);

            var bindings = target.Bindings.Cast<Binding>().ToArray();

            Assert.Equal("Foo", bindings[0].Path);
            Assert.Equal("Bar", bindings[1].Path);
        }

        [Fact]
        public void Control_Template_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
                                    .With(windowingPlatform: new MockWindowingPlatform())))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Template>
        <ControlTemplate TargetType='Window'>
            <ContentPresenter Name='PART_ContentPresenter'
                        Content='{TemplateBinding Content}'/>
        </ControlTemplate>
    </Window.Template>
</Window>";

                var target = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

                Assert.NotNull(target.Template);

                Assert.Null(target.Presenter);

                target.ApplyTemplate();

                Assert.NotNull(target.Presenter);

                target.Content = "Foo";

                Assert.Equal("Foo", target.Presenter.Content);
            }
        }

        [Fact]
        public void Style_ControlTemplate_Is_Built()
        {
            var xaml = @"
<Style xmlns='https://github.com/avaloniaui' Selector='ContentControl'>
  <Setter Property='Template'>
     <ControlTemplate>
        <ContentPresenter Name='PART_ContentPresenter'
                       Content='{TemplateBinding Content}'
                       ContentTemplate='{TemplateBinding ContentTemplate}' />
      </ControlTemplate>
  </Setter>
</Style> ";

            var style = AvaloniaRuntimeXamlLoader.Parse<Style>(xaml);

            Assert.Single(style.Setters);

            var setter = (Setter)style.Setters.First();

            Assert.Equal(ContentControl.TemplateProperty, setter.Property);

            Assert.IsType<ControlTemplate>(setter.Value);

            var template = (ControlTemplate)setter.Value;

            var control = new ContentControl();

            var result = (ContentPresenter)template.Build(control).Control;

            Assert.NotNull(result);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Name='button'>Foo</Button>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var button = window.FindControl<Button>("button");

                Assert.Equal("Foo", button.Content);
            }
        }

        [Fact]
        public void Control_Is_Added_To_Parent_Before_Properties_Are_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
                var widthChanged = tracker.Order.IndexOf("Property Width Changed");

                Assert.NotEqual(-1, attached);
                Assert.NotEqual(-1, widthChanged);
                Assert.True(attached < widthChanged);
            }
        }

        [Fact]
        public void Control_Is_Added_To_Parent_Before_Final_EndInit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
                var endInit = tracker.Order.IndexOf("EndInit 0");

                Assert.NotEqual(-1, attached);
                Assert.NotEqual(-1, endInit);
                Assert.True(attached < endInit);
            }
        }

        [Fact]
        public void All_Properties_Are_Set_Before_Final_EndInit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker Width='100' Height='100'
        Tag='{Binding Height, RelativeSource={RelativeSource Self}}' />
</Window>";


                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                //ensure binding is set and operational first
                Assert.Equal(100.0, tracker.Tag);

                Assert.Equal("EndInit 0", tracker.Order.Last());
            }
        }

        [Fact]
        public void BeginInit_Matches_EndInit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker />
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                Assert.Equal(0, tracker.InitState);
            }
        }

        [Fact]
        public void DeferedXamlLoader_Should_Preserve_NamespacesContext()
        {
            var xaml =
@"<ContentControl xmlns='https://github.com/avaloniaui'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock  Tag='{x:Static local:NonControl.StringProperty}'/>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

            var contentControl = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);
            var template = contentControl.ContentTemplate;

            Assert.NotNull(template);

            var txt = (TextBlock)template.Build(null);

            Assert.Equal((object)NonControl.StringProperty, txt.Tag);
        }

        [Fact]
        public void Binding_To_List_AvaloniaProperty_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <ListBox Items='{Binding Items}' SelectedItems='{Binding SelectedItems}'/>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var listBox = (ListBox)window.Content;

                var vm = new SelectedItemsViewModel()
                {
                    Items = new string[] { "foo", "bar", "baz" }
                };

                window.DataContext = vm;

                Assert.Equal(vm.Items, listBox.Items);

                Assert.Equal(vm.SelectedItems, listBox.SelectedItems);
            }
        }

        [Fact]
        public void Element_Whitespace_Should_Be_Trimmed()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <TextBlock>
        Hello World!
    </TextBlock>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var textBlock = (TextBlock)window.Content;

                Assert.Equal("Hello World!", textBlock.Text);
            }
        }

        [Fact]
        public void Design_Mode_Properties_Should_Be_Ignored_At_Runtime_And_Set_In_Design_Mode()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui' 
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
    mc:Ignorable='d'
    d:DataContext='data-context'
    d:DesignWidth='123'
    d:DesignHeight='321'
>

</Window>";
                foreach (var designMode in new[] {true, false})
                {
                    var obj = (Window)AvaloniaRuntimeXamlLoader.Load(xaml, designMode: designMode);
                    var context = Design.GetDataContext(obj);
                    var width = Design.GetWidth(obj);
                    var height = Design.GetHeight(obj);
                    if (designMode)
                    {
                        Assert.Equal("data-context", context);
                        Assert.Equal(123, width);
                        Assert.Equal(321, height);
                    }
                    else
                    {
                        Assert.False(obj.IsSet(Design.DataContextProperty));
                        Assert.False(obj.IsSet(Design.WidthProperty));
                        Assert.False(obj.IsSet(Design.HeightProperty));
                    }
                }
            }
        }

        [Fact]
        public void Slider_Properties_Can_Be_Set_In_Any_Order()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <Slider Width='400' Value='500' Minimum='0' Maximum='1000'/>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var slider = (Slider)window.Content;

                Assert.Equal(0, slider.Minimum);
                Assert.Equal(1000, slider.Maximum);
                Assert.Equal(500, slider.Value);
            }
        }

        private class SelectedItemsViewModel : INotifyPropertyChanged
        {
            public string[] Items { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            private IList _selectedItems = new AvaloniaList<string>();

            public IList SelectedItems
            {
                get { return _selectedItems; }
                set
                {
                    _selectedItems = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItems)));
                }
            }
        }
    }
    public class BasicTestsAttachedPropertyHolder
    {
        public static AvaloniaProperty<string> FooProperty =
            AvaloniaProperty.RegisterAttached<BasicTestsAttachedPropertyHolder, AvaloniaObject, string>("Foo");

        public static void SetFoo(AvaloniaObject target, string value) => target.SetValue(FooProperty, value);
        public static string GetFoo(AvaloniaObject target) => (string)target.GetValue(FooProperty);

    }
}
