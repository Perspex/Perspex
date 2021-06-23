using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Data
{
    public class DefaultValueConverterTests
    {
        [Fact]
        public void Can_Convert_String_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5", 
                typeof(int), 
                null, 
                CultureInfo.InvariantCulture);

            Assert.Equal(5, result);
        }

        [Fact]
        public void Can_Convert_String_To_Double()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5",
                typeof(double),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Do_Not_Throw_On_InvalidInput_For_NullableInt()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "<not-a-number>",
                typeof(int?),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsType(typeof(BindingNotification), result);
        }

        [Fact]
        public void Can_Convert_Decimal_To_NullableDouble()
        {
            var result = DefaultValueConverter.Instance.Convert(
                5m,
                typeof(double?),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Can_Convert_CustomType_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                new CustomType(123),
                typeof(int),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(123, result);
        }

        [Fact]
        public void Can_Convert_Int_To_CustomType()
        {
            var result = DefaultValueConverter.Instance.Convert(
                123,
                typeof(CustomType),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(new CustomType(123), result);
        }

        [Fact]
        public void Can_Convert_String_To_Enum()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "Bar",
                typeof(TestEnum),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(TestEnum.Bar, result);
        }

        [Fact]
        public void Can_Convert_String_To_TimeSpan()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "00:00:10",
                typeof(TimeSpan),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(TimeSpan.FromSeconds(10), result);
        }

        [Fact]
        public void Can_Convert_Int_To_Enum()
        {
            var result = DefaultValueConverter.Instance.Convert(
                1,
                typeof(TestEnum),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(TestEnum.Bar, result);
        }

        [Fact]
        public void Can_Convert_Double_To_String()
        {
            var result = DefaultValueConverter.Instance.Convert(
                5.0,
                typeof(string),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal("5", result);
        }

        [Fact]
        public void Can_Convert_Enum_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Bar,
                typeof(int),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Can_Convert_Enum_To_String()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Bar,
                typeof(string),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal("Bar", result);
        }

        [Fact]
        public void Can_Use_Explicit_Cast()
        {
            var result = DefaultValueConverter.Instance.Convert(
                new ExplicitDouble(5.0),
                typeof(double),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Cannot_Convert_Between_Different_Enum_Types()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Foo,
                typeof(Orientation),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsType<BindingNotification>(result);
        }

        [Fact]
        public void Can_Convert_From_Delegate_To_Command()
        {
            int commandResult = 0;

            var result = DefaultValueConverter.Instance.Convert(
                (Action<int>)((int i) => { commandResult = i; }),
                typeof(ICommand),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsAssignableFrom<ICommand>(result);

            (result as ICommand).Execute(5);

            Assert.Equal(5, commandResult);
        }

        [Fact]
        public void Can_Convert_From_Delegate_To_Command_No_Parameters()
        {
            int commandResult = 0;

            var result = DefaultValueConverter.Instance.Convert(
                (Action)(() => { commandResult = 1; }),
                typeof(ICommand),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsAssignableFrom<ICommand>(result);

            (result as ICommand).Execute(null);

            Assert.Equal(1, commandResult);
        }

        private enum TestEnum
        {
            Foo,
            Bar,
        }

        private class ExplicitDouble
        {
            public ExplicitDouble(double value)
            {
                Value = value;
            }

            public double Value { get; }

            public static explicit operator double (ExplicitDouble v)
            {
                return v.Value;
            }
        }

        [TypeConverter(typeof(CustomTypeConverter))]
        private class CustomType {

            public int Value { get; }

            public CustomType(int value)
            {
                Value = value;
            }

            public override bool Equals(object obj)
            {
                return obj is CustomType other && this.Value == other.Value;
            }

            public override int GetHashCode()
            {
                return 8399587^Value.GetHashCode();
            }
        }

        private class CustomTypeConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(int);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(int);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return ((CustomType)value).Value;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return new CustomType((int)value);
            }
        }
    }
}
