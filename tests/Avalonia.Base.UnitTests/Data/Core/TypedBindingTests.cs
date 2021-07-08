// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Moq;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class TypedBindingTests
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = TypedBinding<Source>.OneWay(o => o.Foo);

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("bar", target.Text);
            target.Text = "baz";
            Assert.Equal("bar", source.Foo);
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = TypedBinding<Source>.TwoWay(o => o.Foo, (o, v) => o.Foo = v);

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("bar", target.Text);
            target.Text = "baz";
            Assert.Equal("baz", source.Foo);
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up_GC_Collect()
        {
            var source = new WeakRefSource { Foo = null };
            var target = new TestControl { DataContext = source };

            var binding = TypedBinding<WeakRefSource>.TwoWay(o => o.Foo, (o, v) => o.Foo = v);

            target.Bind(TestControl.ValueProperty, binding);

            var ref1 = AssignValue(target, "ref1");

            Assert.Equal(ref1.Target, source.Foo);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var ref2 = AssignValue(target, "ref2");

            GC.Collect();
            GC.WaitForPendingFinalizers();

            target.Value = null;

            Assert.Null(source.Foo);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Immediately_Write_Value_Back_To_Source()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = TypedBinding<Source>.TwoWay(o => o.Foo, (o, v) => o.Foo = v);
            var raised = 0;

            source.PropertyChanged += (s, e) => ++raised;

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal(0, raised);
        }

        private class DummyObject : ICloneable
        {
            private readonly string _val;

            public DummyObject(string val)
            {
                _val = val;
            }

            public object Clone()
            {
                return new DummyObject(_val);
            }

            protected bool Equals(DummyObject other)
            {
                return string.Equals(_val, other._val);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((DummyObject)obj);
            }

            public override int GetHashCode()
            {
                return (_val != null ? _val.GetHashCode() : 0);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WeakReference AssignValue(TestControl source, string val)
        {
            var obj = new DummyObject(val);

            source.Value = obj;

            return new WeakReference(obj);
        }

        [Fact]
        public void OneTime_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = TypedBinding<Source>.OneTime(o => o.Foo);

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("foo", target.Text);
            target.Text = "baz";
            Assert.Equal("bar", source.Foo);
        }

        ////[Fact]
        ////public void OneWayToSource_Binding_Should_Be_Set_Up()
        ////{
        ////    var source = new Source { Foo = "foo" };
        ////    var target = new TextBlock { DataContext = source, Text = "bar" };
        ////    var binding = new Binding
        ////    {
        ////        Path = "Foo",
        ////        Mode = BindingMode.OneWayToSource,
        ////    };

        ////    target.Bind(TextBox.TextProperty, binding);

        ////    Assert.Equal("bar", source.Foo);
        ////    target.Text = "baz";
        ////    Assert.Equal("baz", source.Foo);
        ////    source.Foo = "quz";
        ////    Assert.Equal("baz", target.Text);
        ////}

        ////[Fact]
        ////public void OneWayToSource_Binding_Should_React_To_DataContext_Changed()
        ////{
        ////    var target = new TextBlock { Text = "bar" };
        ////    var binding = new Binding
        ////    {
        ////        Path = "Foo",
        ////        Mode = BindingMode.OneWayToSource,
        ////    };

        ////    target.Bind(TextBox.TextProperty, binding);

        ////    var source = new Source { Foo = "foo" };
        ////    target.DataContext = source;

        ////    Assert.Equal("bar", source.Foo);
        ////    target.Text = "baz";
        ////    Assert.Equal("baz", source.Foo);
        ////    source.Foo = "quz";
        ////    Assert.Equal("baz", target.Text);
        ////}

        [Fact]
        public void Default_BindingMode_Should_Be_Used()
        {
            var source = new Source { Foo = "foo" };
            var target = new TwoWayBindingTest { DataContext = source };
            var binding = TypedBinding<Source>.Default(o => o.Foo, (o, v) => o.Foo = v);

            target.Bind(TwoWayBindingTest.TwoWayProperty, binding);

            Assert.Equal("foo", target.TwoWay);
            source.Foo = "bar";
            Assert.Equal("bar", target.TwoWay);
            target.TwoWay = "baz";
            Assert.Equal("baz", source.Foo);
        }

        [Fact]
        public void DataContext_Binding_Should_Use_Parent_DataContext()
        {
            var parentDataContext = Mock.Of<IHeadered>(x => x.Header == (object)"Foo");

            var parent = new Decorator
            {
                Child = new Control(),
                DataContext = parentDataContext,
            };

            var binding = TypedBinding
                <IHeadered>.OneWay(x => x.Header);

            parent.Child.Bind(Control.DataContextProperty, binding);

            Assert.Equal("Foo", parent.Child.DataContext);

            parentDataContext = Mock.Of<IHeadered>(x => x.Header == (object)"Bar");
            parent.DataContext = parentDataContext;
            Assert.Equal("Bar", parent.Child.DataContext);
        }

        [Fact]
        public void DataContext_Binding_Should_Track_Parent()
        {
            var parent = new Decorator
            {
                DataContext = new Source { Foo = "foo" },
            };

            var child = new Control();

            var binding = TypedBinding<Source>.OneWay(x => x.Foo);

            child.Bind(Control.DataContextProperty, binding);

            Assert.Null(child.DataContext);
            parent.Child = child;
            Assert.Equal("foo", child.DataContext);
        }

        [Fact]
        public void DataContext_Binding_Should_Produce_Correct_Results()
        {
            var viewModel = new Source { Foo = "bar" };
            var root = new Decorator
            {
                DataContext = viewModel,
            };

            var child = new Control();
            var values = new List<object>();
            var binding = TypedBinding<Source>.OneWay(x => x.Foo);

            child.GetObservable(Control.DataContextProperty).Subscribe(x => values.Add(x));
            child.Bind(Control.DataContextProperty, binding);

            // When binding to DataContext and the target isn't found, the binding should produce
            // null rather than UnsetValue in order to not propagate incorrect DataContexts from
            // parent controls while things are being set up. This logic is implemented in 
            // `Avalonia.Data.TypedBinding<TIn,TOut>.Initiate`.
            Assert.True(child.IsSet(Control.DataContextProperty));

            root.Child = child;

            Assert.Equal(new[] { null, "bar" }, values);
        }

        [Fact]
        public void Binding_With_Source_Should_Produce_Correct_Results()
        {
            var viewModel = new Source { Foo = "bar" };
            var target = new TextBlock();
            var binding = TypedBinding<Source>.OneWay(x => x.Foo);

            binding.Source = viewModel;

            target.Bind(TextBlock.TextProperty, binding);

            Assert.Equal("bar", target.Text);
        }

        ////[Fact]
        ////public void Should_Use_DefaultValueConverter_When_No_Converter_Specified()
        ////{
        ////    var target = new TextBlock(); ;
        ////    var binding = new Binding
        ////    {
        ////        Path = "Foo",
        ////    };

        ////    var result = binding.Initiate(target, TextBox.TextProperty).Subject;

        ////    Assert.IsType<DefaultValueConverter>(((BindingExpression)result).Converter);
        ////}

        ////[Fact]
        ////public void Should_Use_Supplied_Converter()
        ////{
        ////    var target = new TextBlock();
        ////    var converter = new Mock<IValueConverter>();
        ////    var binding = new Binding
        ////    {
        ////        Converter = converter.Object,
        ////        Path = "Foo",
        ////    };

        ////    var result = binding.Initiate(target, TextBox.TextProperty).Subject;

        ////    Assert.Same(converter.Object, ((BindingExpression)result).Converter);
        ////}

        ////[Fact]
        ////public void Should_Pass_ConverterParameter_To_Supplied_Converter()
        ////{
        ////    var target = new TextBlock();
        ////    var converter = new Mock<IValueConverter>();
        ////    var binding = new Binding
        ////    {
        ////        Converter = converter.Object,
        ////        ConverterParameter = "foo",
        ////        Path = "Bar",
        ////    };

        ////    var result = binding.Initiate(target, TextBox.TextProperty).Subject;

        ////    Assert.Same("foo", ((BindingExpression)result).ConverterParameter);
        ////}

        ////[Fact]
        ////public void Should_Return_FallbackValue_When_Path_Not_Resolved()
        ////{
        ////    var target = new TextBlock();
        ////    var source = new Source();
        ////    var binding = new Binding
        ////    {
        ////        Source = source,
        ////        Path = "BadPath",
        ////        FallbackValue = "foofallback",
        ////    };

        ////    target.Bind(TextBlock.TextProperty, binding);

        ////    Assert.Equal("foofallback", target.Text);
        ////}

        ////[Fact]
        ////public void Should_Return_FallbackValue_When_Invalid_Source_Type()
        ////{
        ////    var target = new ProgressBar();
        ////    var source = new Source { Foo = "foo" };
        ////    var binding = new Binding
        ////    {
        ////        Source = source,
        ////        Path = "Foo",
        ////        FallbackValue = 42,
        ////    };

        ////    target.Bind(ProgressBar.ValueProperty, binding);

        ////    Assert.Equal(42, target.Value);
        ////}

        ////[Fact]
        ////public void Null_Path_Should_Bind_To_DataContext()
        ////{
        ////    var target = new TextBlock { DataContext = "foo" };
        ////    var binding = new Binding();

        ////    target.Bind(TextBlock.TextProperty, binding);

        ////    Assert.Equal("foo", target.Text);
        ////}

        ////[Fact]
        ////public void Empty_Path_Should_Bind_To_DataContext()
        ////{
        ////    var target = new TextBlock { DataContext = "foo" };
        ////    var binding = new Binding { Path = string.Empty };

        ////    target.Bind(TextBlock.TextProperty, binding);

        ////    Assert.Equal("foo", target.Text);
        ////}

        ////[Fact]
        ////public void Dot_Path_Should_Bind_To_DataContext()
        ////{
        ////    var target = new TextBlock { DataContext = "foo" };
        ////    var binding = new Binding { Path = "." };

        ////    target.Bind(TextBlock.TextProperty, binding);

        ////    Assert.Equal("foo", target.Text);
        ////}

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the logical tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedIndex
        ///   binding and so the unselection is sent back to the ViewModel
        /// </remarks>
        [Fact]
        public void Should_Not_Write_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new OldDataContextTest();

            var fooBinding = TypedBinding<OldDataContextViewModel>.TwoWay(o => o.Foo, (o, v) => o.Foo = v);
            var barBinding = TypedBinding<OldDataContextViewModel>.TwoWay(o => o.Bar, (o, v) => o.Bar = v);

            // Bind Foo and Bar to the VM.
            target.Bind(OldDataContextTest.FooProperty, fooBinding);
            target.Bind(OldDataContextTest.BarProperty, barBinding);
            target.DataContext = vm;

            // Make sure the control's Foo and Bar properties are read from the VM
            Assert.Equal(1, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(2, target.GetValue(OldDataContextTest.BarProperty));

            // Set DataContext to null.
            target.DataContext = null;

            // Foo and Bar are no longer bound so they return 0, their default value.
            Assert.Equal(0, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(0, target.GetValue(OldDataContextTest.BarProperty));

            // The problem was here - DataContext is now null, setting Foo to 0. Bar is bound to 
            // Foo so Bar also gets set to 0. However the Bar binding still had a reference to
            // the VM and so vm.Bar was set to 0 erroneously.
            Assert.Equal(1, vm.Foo);
            Assert.Equal(2, vm.Bar);
        }

        [Fact]
        public void AvaloniaObject_this_Operator_Accepts_TypedBinding()
        {
            var target = new ContentControl
            {
                DataContext = new Source { Foo = "foo" }
            };

            target[!ContentControl.ContentProperty] = TypedBinding<Source>.OneWay(o => o.Foo);

            Assert.Equal("foo", target.Content);
        }

        ////[Fact]
        ////public void StyledProperty_SetValue_Should_Not_Cause_StackOverflow_And_Have_Correct_Values()
        ////{
        ////    var viewModel = new TestStackOverflowViewModel()
        ////    {
        ////        Value = 50
        ////    };

        ////    var target = new StyledPropertyClass();

        ////    target.Bind(StyledPropertyClass.DoubleValueProperty,
        ////        new Binding("Value") { Mode = BindingMode.TwoWay, Source = viewModel });

        ////    var child = new StyledPropertyClass();

        ////    child.Bind(StyledPropertyClass.DoubleValueProperty,
        ////        new Binding("DoubleValue")
        ////        {
        ////            Mode = BindingMode.TwoWay,
        ////            Source = target
        ////        });

        ////    Assert.Equal(1, viewModel.SetterInvokedCount);

        ////    //here in real life stack overflow exception is thrown issue #855 and #824
        ////    target.DoubleValue = 51.001;

        ////    Assert.Equal(2, viewModel.SetterInvokedCount);

        ////    double expected = 51;

        ////    Assert.Equal(expected, viewModel.Value);
        ////    Assert.Equal(expected, target.DoubleValue);
        ////    Assert.Equal(expected, child.DoubleValue);
        ////}

        ////[Fact]
        ////public void SetValue_Should_Not_Cause_StackOverflow_And_Have_Correct_Values()
        ////{
        ////    var viewModel = new TestStackOverflowViewModel()
        ////    {
        ////        Value = 50
        ////    };

        ////    var target = new DirectPropertyClass();

        ////    target.Bind(DirectPropertyClass.DoubleValueProperty, new Binding("Value")
        ////    {
        ////        Mode = BindingMode.TwoWay,
        ////        Source = viewModel
        ////    });

        ////    var child = new DirectPropertyClass();

        ////    child.Bind(DirectPropertyClass.DoubleValueProperty,
        ////        new Binding("DoubleValue")
        ////        {
        ////            Mode = BindingMode.TwoWay,
        ////            Source = target
        ////        });

        ////    Assert.Equal(1, viewModel.SetterInvokedCount);

        ////    //here in real life stack overflow exception is thrown issue #855 and #824
        ////    target.DoubleValue = 51.001;

        ////    Assert.Equal(2, viewModel.SetterInvokedCount);

        ////    double expected = 51;

        ////    Assert.Equal(expected, viewModel.Value);
        ////    Assert.Equal(expected, target.DoubleValue);
        ////    Assert.Equal(expected, child.DoubleValue);
        ////}

        ////private class StyledPropertyClass : AvaloniaObject
        ////{
        ////    public static readonly StyledProperty<double> DoubleValueProperty =
        ////                AvaloniaProperty.Register<StyledPropertyClass, double>(nameof(DoubleValue));

        ////    public double DoubleValue
        ////    {
        ////        get { return GetValue(DoubleValueProperty); }
        ////        set { SetValue(DoubleValueProperty, value); }
        ////    }
        ////}

        ////private class DirectPropertyClass : AvaloniaObject
        ////{
        ////    public static readonly DirectProperty<DirectPropertyClass, double> DoubleValueProperty =
        ////        AvaloniaProperty.RegisterDirect<DirectPropertyClass, double>(
        ////            nameof(DoubleValue),
        ////            o => o.DoubleValue,
        ////            (o, v) => o.DoubleValue = v);

        ////    private double _doubleValue;
        ////    public double DoubleValue
        ////    {
        ////        get { return _doubleValue; }
        ////        set { SetAndRaise(DoubleValueProperty, ref _doubleValue, value); }
        ////    }
        ////}

        ////private class TestStackOverflowViewModel : INotifyPropertyChanged
        ////{
        ////    public int SetterInvokedCount { get; private set; }

        ////    public const int MaxInvokedCount = 1000;

        ////    private double _value;

        ////    public event PropertyChangedEventHandler PropertyChanged;

        ////    public double Value
        ////    {
        ////        get { return _value; }
        ////        set
        ////        {
        ////            if (_value != value)
        ////            {
        ////                SetterInvokedCount++;
        ////                if (SetterInvokedCount < MaxInvokedCount)
        ////                {
        ////                    _value = (int)value;
        ////                    if (_value > 75) _value = 75;
        ////                    if (_value < 25) _value = 25;
        ////                }
        ////                else
        ////                {
        ////                    _value = value;
        ////                }

        ////                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        ////            }
        ////        }
        ////    }
        ////}

        private class TwoWayBindingTest : Control
        {
            public static readonly StyledProperty<string> TwoWayProperty =
                AvaloniaProperty.Register<TwoWayBindingTest, string>(
                    "TwoWay",
                    defaultBindingMode: BindingMode.TwoWay);

            public string TwoWay
            {
                get { return GetValue(TwoWayProperty); }
                set { SetValue(TwoWayProperty, value); }
            }
        }

        public class Source : INotifyPropertyChanged
        {
            private string _foo;

            public string Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string prop = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }

        public class WeakRefSource : INotifyPropertyChanged
        {
            private WeakReference<object> _foo;

            public object Foo
            {
                get
                {
                    if (_foo == null)
                    {
                        return null;
                    }

                    if (_foo.TryGetTarget(out object target))
                    {
                        if (target is ICloneable cloneable)
                        {
                            return cloneable.Clone();
                        }

                        return target;
                    }

                    return null;
                }
                set
                {
                    _foo = new WeakReference<object>(value);

                    RaisePropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string prop = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }

        private class OldDataContextViewModel
        {
            public int Foo { get; set; } = 1;
            public int Bar { get; set; } = 2;
        }

        private class TestControl : Control
        {
            public static readonly DirectProperty<TestControl, object> ValueProperty =
                AvaloniaProperty.RegisterDirect<TestControl, object>(
                    nameof(Value),
                    o => o.Value,
                    (o, v) => o.Value = v);

            private object _value;

            public object Value
            {
                get => _value;
                set => SetAndRaise(ValueProperty, ref _value, value);
            }
        }

        private class OldDataContextTest : Control
        {
            public static readonly StyledProperty<int> FooProperty =
                AvaloniaProperty.Register<OldDataContextTest, int>("Foo");

            public static readonly StyledProperty<int> BarProperty =
              AvaloniaProperty.Register<OldDataContextTest, int>("Bar");

            public OldDataContextTest()
            {
                Bind(BarProperty, this.GetBindingObservable(FooProperty));
            }
        }

        ////private class InheritanceTest : Decorator
        ////{
        ////    public static readonly StyledProperty<int> BazProperty =
        ////        AvaloniaProperty.Register<InheritanceTest, int>(nameof(Baz), defaultValue: 6, inherits: true);

        ////    public int Baz
        ////    {
        ////        get { return GetValue(BazProperty); }
        ////        set { SetValue(BazProperty, value); }
        ////    }
        ////}
    }
}
