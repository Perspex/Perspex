using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class StyleTests
    {
        [Fact]
        public void Style_With_Only_Type_Selector_Should_Update_Value()
        {
            Style style = new Style(x => x.OfType<Class1>())
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, null);

            Assert.Equal("Foo", target.Foo);
        }

        [Fact]
        public void Style_With_Class_Selector_Should_Update_And_Restore_Value()
        {
            Style style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, null);
            Assert.Equal("foodefault", target.Foo);
            target.Classes.Add("foo");
            Assert.Equal("Foo", target.Foo);
            target.Classes.Remove("foo");
            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Style_With_No_Selector_Should_Apply_To_Containing_Control()
        {
            Style style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, target);

            Assert.Equal("Foo", target.Foo);
        }

        [Fact]
        public void Style_With_No_Selector_Should_Not_Apply_To_Other_Control()
        {
            Style style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();
            var other = new Class1();

            style.TryAttach(target, other);

            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void LocalValue_Should_Override_Style()
        {
            Style style = new Style(x => x.OfType<Class1>())
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1
            {
                Foo = "Original",
            };

            style.TryAttach(target, null);
            Assert.Equal("Original", target.Foo);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Foo"),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Bar"),
                    },
                }
            };

            var target = new Class1();

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Foo", "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier_2()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Foo"),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Bar"),
                    },
                }
            };

            var target = new Class1();

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("bar");
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier_3()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, new Binding("Foo")),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, new Binding("Bar")),
                    },
                }
            };

            var target = new Class1
            {
                DataContext = new
                {
                    Foo = "Foo",
                    Bar = "Bar",
                }
            };

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("bar");
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Attach()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var values = new List<string>();
            var target = new Class1();

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = target;

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Inactive_Bindings_Should_Not_Be_Made_Active_During_Style_Attach()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Foo")),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Bar")),
                        },
                    }
                }
            };

            var values = new List<string>();
            var target = new Class1
            {
                DataContext = new
                {
                    Foo = "Foo",
                    Bar = "Bar",
                }
            };

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = target;

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Detach()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var target = new Class1();
            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Detach_2()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var target = new Class1 { Classes = { "foo" } };
            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Foo", "foodefault" }, values);
        }

        [Fact]
        public void Inactive_Bindings_Should_Not_Be_Made_Active_During_Style_Detach()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Foo")),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Bar")),
                        },
                    }
                }
            };

            var target = new Class1
            {
                DataContext = new
                {
                    Foo = "Foo",
                    Bar = "Bar",
                }
            };

            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Template_In_Non_Matching_Style_Is_Not_Built()
        {
            var instantiationCount = 0;
            var template = new FuncTemplate<Class1>(() =>
            {
                ++instantiationCount;
                return new Class1();
            });

            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                },

                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                }
            };

            var target = new Class1();
            styles.TryAttach(target, null);

            Assert.NotNull(target.Child);
            Assert.Equal(1, instantiationCount);
        }

        [Fact]
        public void Template_In_Inactive_Style_Is_Not_Built()
        {
            var instantiationCount = 0;
            var template = new FuncTemplate<Class1>(() =>
            {
                ++instantiationCount;
                return new Class1();
            });

            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                },

                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                }
            };

            var target = new Class1();
            target.BeginBatchUpdate();
            styles.TryAttach(target, null);
            target.EndBatchUpdate();

            Assert.NotNull(target.Child);
            Assert.Equal(1, instantiationCount);
        }

        [Fact]
        public void Style_Should_Detach_When_Control_Removed_From_Logical_Tree()
        {
            Border border;

            var style = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                }
            };

            var root = new TestRoot
            {
                Child = border = new Border(),
            };

            style.TryAttach(border, null);

            Assert.Equal(new Thickness(4), border.BorderThickness);
            root.Child = null;
            Assert.Equal(new Thickness(0), border.BorderThickness);
        }

        [Fact]
        public void Removing_Style_Should_Detach_From_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var border = new Border();
                var root = new TestRoot
                {
                    Styles =
                    { 
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        }
                    },
                    Child = border,
                };

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(4), border.BorderThickness);

                root.Styles.RemoveAt(0);
                Assert.Equal(new Thickness(0), border.BorderThickness);
            }
        }

        [Fact]
        public void Adding_Style_Should_Attach_To_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var border = new Border();
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        }
                    },
                    Child = border,
                };

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(4), border.BorderThickness);

                root.Styles.Add(new Style(x => x.OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                    }
                });

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(6), border.BorderThickness);
            }
        }

        [Fact]
        public void Removing_Style_With_Nested_Style_Should_Detach_From_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var border = new Border();
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Styles
                        {
                            new Style(x => x.OfType<Border>())
                            {
                                Setters =
                                {
                                    new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                                }
                            }
                        }
                    },
                    Child = border,
                };

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(4), border.BorderThickness);

                root.Styles.RemoveAt(0);
                Assert.Equal(new Thickness(0), border.BorderThickness);
            }
        }
        
        [Fact]
        public void Adding_Nested_Style_Should_Attach_To_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var border = new Border();
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Styles
                        {
                            new Style(x => x.OfType<Border>())
                            {
                                Setters =
                                {
                                    new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                                }
                            }
                        }
                    },
                    Child = border,
                };

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(4), border.BorderThickness);

                ((Styles)root.Styles[0]).Add(new Style(x => x.OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                    }
                });

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(6), border.BorderThickness);
            }
        }

        [Fact]
        public void Removing_Nested_Style_Should_Detach_From_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var border = new Border();
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Styles
                        {
                            new Style(x => x.OfType<Border>())
                            {
                                Setters =
                                {
                                    new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                                }
                            },
                            new Style(x => x.OfType<Border>())
                            {
                                Setters =
                                {
                                    new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                                }
                            },
                        }
                    },
                    Child = border,
                };

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(6), border.BorderThickness);

                ((Styles)root.Styles[0]).RemoveAt(1);

                root.Measure(Size.Infinity);
                Assert.Equal(new Thickness(4), border.BorderThickness);
            }
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources()
        {
            var host = new Mock<IResourceHost>();
            var target = new Style();
            ((IResourceProvider)target).AddOwner(host.Object);

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources_2()
        {
            var host = new Mock<IResourceHost>();
            var target = new Style();

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            host.Invocations.Clear();
            ((IResourceProvider)target).AddOwner(host.Object);
            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        private class Class1 : Control
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");

            public static readonly StyledProperty<Class1> ChildProperty =
                AvaloniaProperty.Register<Class1, Class1>(nameof(Child));

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }

            public Class1 Child
            {
                get => GetValue(ChildProperty);
                set => SetValue(ChildProperty, value);
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                throw new NotImplementedException();
            }
        }
    }
}
