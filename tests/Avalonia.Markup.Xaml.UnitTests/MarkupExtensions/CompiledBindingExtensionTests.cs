using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Markup.Data;
using Avalonia.Media;
using Avalonia.UnitTests;
using XamlX;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class CompiledBindingExtensionTests
    {
        [Fact]
        public void ResolvesClrPropertyBasedOnDataContextType()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesClrPropertyBasedOnDataContextType_InterfaceInheritance()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:IHasPropertyDerived'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesPathPassedByProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding Path=StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesStreamTaskBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding TaskProperty^}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    TaskProperty = Task.FromResult("foobar")
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.TaskProperty.Result, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesStreamObservableBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableProperty^}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                DelayedBinding.ApplyBindings(textBlock);

                var subject = new Subject<string>();
                var dataContext = new TestDataContext
                {
                    ObservableProperty = subject
                };

                window.DataContext = dataContext;

                subject.OnNext("foobar");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ListProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ListProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesArrayIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ArrayProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ArrayProperty = new[] { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ArrayProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesObservableIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableCollectionProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ObservableCollectionProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ObservableCollectionProperty[3], textBlock.Text);

                dataContext.ObservableCollectionProperty[3] = "New Value";

                Assert.Equal(dataContext.ObservableCollectionProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void InfersCompiledBindingDataContextFromDataContextBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock DataContext='{CompiledBinding StringProperty}' Text='{CompiledBinding}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                var dataContext = new TestDataContext
                {
                    StringProperty = "A"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesNonIntegerIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerProperty[Test]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();

                dataContext.NonIntegerIndexerProperty["Test"] = "Initial Value";

                window.DataContext = dataContext;

                Assert.Equal(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);

                dataContext.NonIntegerIndexerProperty["Test"] = "New Value";

                Assert.Equal(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesNonIntegerIndexerBindingFromParentInterfaceCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerInterfaceProperty[Test]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();

                dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "Initial Value";

                window.DataContext = dataContext;

                Assert.Equal(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);

                dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "New Value";

                Assert.Equal(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);
            }
        }

        [Fact]
        public void InfersDataTemplateTypeFromDataTypeProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type x:String}'>
            <TextBlock Text='{CompiledBinding}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding StringProperty}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child).Text);
            }
        }


        [Fact]
        public void ThrowsOnUninferrableLooseDataTemplateNoDataTypeWithCompiledBindingPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding}' />
</Window>";
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void ThrowsOnUninferrableDataTypeFromNonCompiledDataContextBindingWithCompiledBindingPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ContentControl Name='target' DataContext='{Binding}'>
        <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
    </ContentControl>
</Window>";
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void InfersDataTemplateTypeFromParentCollectionItemsType()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Items='{CompiledBinding ListProperty}' Name='target'>
        <ItemsControl.DataTemplates>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.DataTemplates>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ItemsControl>("target");

                var dataContext = new TestDataContext();

                dataContext.ListProperty.Add("Test");

                window.DataContext = dataContext;

                window.LayoutManager.ExecuteInitialLayoutPass();

                var presenter = (ContentPresenter)target.Presenter.RealizedElements.First();
                Assert.Equal(dataContext.ListProperty[0], presenter.Content);
            }
        }

        [Fact]
        public void ThrowsOnUninferrableDataTemplateInItemsControlWithoutItemsBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Name='target'>
        <ItemsControl.DataTemplates>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding Property}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.DataTemplates>
    </ItemsControl>
</Window>";
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void ResolvesElementNameBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding #text.Text}' x:Name='text2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("text2");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesElementNameBindingFromLongForm()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding Text, ElementName=text}' x:Name='text2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("text2");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesRelativeSourceBindingLongForm()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Title, RelativeSource={RelativeSource AncestorType=Window}}' x:Name='text'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("test", target.Text);
            }
        }

        [Fact]
        public void Binds_To_Source()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Length, Source=Test}' x:Name='text'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("Test".Length.ToString(), target.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        <x:String x:Key='otherObjectKey'>test</x:String>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource_In_ResourceDictionary()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource_In_ResourceDictionary1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
            <x:String x:Key='otherObjectKey'>test</x:String>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_xStatic()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <ContentControl Name='contentControl' Content='{Binding Color, Source={x:Static Brushes.Red}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                Assert.Equal(Brushes.Red.Color, contentControl.Content);
            }
        }

        [Fact]
        public void CompilesBindingWhenRequested()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ThrowsOnInvalidBindingPathOnCompiledBindingEnabledViaDirective()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding InvalidPath}' Name='textBlock' />
</Window>";
                ThrowsXamlParseException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void SupportParentInPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        Title='foo'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.Title}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                Assert.Equal("foo", contentControl.Content);
            }
        }

        [Fact]
        public void SupportConverterWithParameter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterParameter=Bar}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = new TestDataContext() { StringProperty = "Foo" };

                Assert.Equal("Foo+Bar", textBlock.Text);
            }
        }

        [Fact]
        public void ThrowsOnInvalidCompileBindingsDirective()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='notabool'>
</Window>";
                ThrowsXamlParseException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void SupportCastToTypeInExpression()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext();

                window.DataContext = dataContext;

                Assert.Equal(dataContext, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpression_DifferentTypeEvaluatesToNull()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var dataContext = "foo";

                window.DataContext = dataContext;

                Assert.Equal(null, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.DataContext(local:TestDataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithPropertyIndexer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding ((local:TestData)ObjectsArrayProperty[0]).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var data = new TestData()
                {
                    StringProperty = "Foo"
                };
                var dataContext = new TestDataContext
                {
                    ObjectsArrayProperty = new object[] { data }
                };

                window.DataContext = dataContext;

                Assert.Equal(data.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty_DifferentTypeEvaluatesToNull()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);

                window.DataContext = "foo";

                Assert.Equal(null, contentControl.Content);
            }
        }

        void Throws(string type, Action cb)
        {
            try
            {
                cb();
            }
            catch (Exception e) when (e.GetType().Name == type)
            {
                return;
            }

            throw new Exception("Expected " + type);
        }

        void ThrowsXamlParseException(Action cb) => Throws("XamlParseException", cb);
        void ThrowsXamlTransformException(Action cb) => Throws("XamlTransformException", cb);
    }
    
    public interface INonIntegerIndexer
    {
        string this[string key] { get; set; }
    }

    public interface INonIntegerIndexerDerived : INonIntegerIndexer
    { }

    public interface IHasProperty
    {
        string StringProperty { get; set; }
    }

    public interface IHasPropertyDerived : IHasProperty
    { }

    public class AppendConverter : IValueConverter
    {
        public static IValueConverter Instance { get; } = new AppendConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Format("{0}+{1}", value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

    }

    public class TestData
    {
        public string StringProperty { get; set; }
    }

    public class TestDataContext : IHasPropertyDerived
    {
        public string StringProperty { get; set; }

        public Task<string> TaskProperty { get; set; }

        public IObservable<string> ObservableProperty { get; set; }

        public ObservableCollection<string> ObservableCollectionProperty { get; set; } = new ObservableCollection<string>();

        public string[] ArrayProperty { get; set; }

        public object[] ObjectsArrayProperty { get; set; }

        public List<string> ListProperty { get; set; } = new List<string>();

        public NonIntegerIndexer NonIntegerIndexerProperty { get; set; } = new NonIntegerIndexer();

        public INonIntegerIndexerDerived NonIntegerIndexerInterfaceProperty => NonIntegerIndexerProperty;

        public class NonIntegerIndexer : NotifyingBase, INonIntegerIndexerDerived
        {
            private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

            public string this[string key]
            {
                get
                {
                    return _storage[key];
                }
                set
                {
                    _storage[key] = value;
                    RaisePropertyChanged(CommonPropertyNames.IndexerName);
                }
            }
        }
    }
}
