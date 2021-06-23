using System;
using System.Globalization;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SetterTests
    {
        [Fact]
        public void Cannot_Assign_Control_To_Value()
        {
            var target = new Setter();

            Assert.Throws<InvalidOperationException>(() => target.Value = new Border());
        }

        [Fact]
        public void Setter_Should_Apply_Binding_To_Property()
        {
            var control = new TextBlock();
            var subject = new BehaviorSubject<object>("foo");
            var descriptor = InstancedBinding.OneWay(subject);
            var binding = Mock.Of<IBinding>(x => x.Initiate(control, TextBlock.TextProperty, null, false) == descriptor);
            var style = Mock.Of<IStyle>();
            var setter = new Setter(TextBlock.TextProperty, binding);

            setter.Instance(control).Start(false);

            Assert.Equal("foo", control.Text);
        }

        [Fact]
        public void Setter_Should_Handle_Binding_Producing_UnsetValue()
        {
            var control = new TextBlock();
            var subject = new BehaviorSubject<object>(AvaloniaProperty.UnsetValue);
            var descriptor = InstancedBinding.OneWay(subject);
            var binding = Mock.Of<IBinding>(x => x.Initiate(control, TextBlock.TagProperty, null, false) == descriptor);
            var style = Mock.Of<IStyle>();
            var setter = new Setter(TextBlock.TagProperty, binding);

            setter.Instance(control).Start(false);

            Assert.Equal("", control.Text);
        }

        [Fact]
        public void Setter_Should_Materialize_Template_To_Property()
        {
            var control = new Decorator();
            var template = new FuncTemplate<Canvas>(() => new Canvas());
            var style = Mock.Of<IStyle>();
            var setter = new Setter(Decorator.ChildProperty, template);

            setter.Instance(control).Start(false);

            Assert.IsType<Canvas>(control.Child);
        }

        [Fact]
        public void Does_Not_Call_Converter_ConvertBack_On_OneWay_Binding()
        {
            var control = new Decorator { Name = "foo" };
            var style = Mock.Of<IStyle>();
            var binding = new Binding("Name", BindingMode.OneWay)
            {
                Converter = new TestConverter(),
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            };
            var setter = new Setter(Decorator.TagProperty, binding);

            var instance = setter.Instance(control);
            instance.Start(true);
            instance.Activate();

            Assert.Equal("foobar", control.Tag);

            // Issue #1218 caused TestConverter.ConvertBack to throw here.
            instance.Deactivate();
            Assert.Null(control.Tag);
        }

        [Fact]
        public void Setter_Should_Apply_Value_Without_Activator_With_Style_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TagProperty, "foo");

            setter.Instance(control.Object).Start(false);

            control.Verify(x => x.SetValue(
                TextBlock.TagProperty,
                "foo",
                BindingPriority.Style));
        }

        [Fact]
        public void Setter_Should_Apply_Value_With_Activator_As_Binding_With_StyleTrigger_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TagProperty, "foo");
            var activator = new Subject<bool>();

            var instance = setter.Instance(control.Object);
            instance.Start(true);
            instance.Activate();

            control.Verify(x => x.Bind(
                TextBlock.TagProperty,
                It.IsAny<IObservable<BindingValue<object>>>(),
                BindingPriority.StyleTrigger));
        }

        [Fact]
        public void Setter_Should_Apply_Binding_Without_Activator_With_Style_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TagProperty, CreateMockBinding(TextBlock.TagProperty));

            setter.Instance(control.Object).Start(false);

            control.Verify(x => x.Bind(
                TextBlock.TagProperty,
                It.IsAny<PropertySetterBindingInstance<object>>(),
                BindingPriority.Style));
        }

        [Fact]
        public void Setter_Should_Apply_Binding_With_Activator_With_StyleTrigger_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TagProperty, CreateMockBinding(TextBlock.TagProperty));

            var instance = setter.Instance(control.Object);
            instance.Start(true);
            instance.Activate();

            control.Verify(x => x.Bind(
                TextBlock.TagProperty,
                It.IsAny<IObservable<BindingValue<object>>>(),
                BindingPriority.StyleTrigger));
        }

        private IBinding CreateMockBinding(AvaloniaProperty property)
        {
            var subject = new Subject<object>();
            var descriptor = InstancedBinding.OneWay(subject);
            var binding = Mock.Of<IBinding>(x => 
                x.Initiate(It.IsAny<IAvaloniaObject>(), property, null, false) == descriptor);
            return binding;
        }

        private class TestConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value.ToString() + "bar";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
