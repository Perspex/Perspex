using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Metadata;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class InlineContainerTest : XamlTestBase
    {
        [Fact]
        public void Inline_Objects_Are_Recognized()
        {
            var xaml = @"<local:InlineContainerControl xmlns='https://github.com/avaloniaui'
xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
><local:RecursiveNonControl>SomeText</local:RecursiveNonControl></local:InlineContainerControl>";
            var target = new InlineContainerControl();

            AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: target);

            Assert.Equal(0, target.Content.AddedStrings.Count);
            Assert.Equal(1, target.Content.AddedObjects.Count);
            Assert.IsType<RecursiveNonControl>(target.Content.AddedObjects[0]);
            var addedNonControl = (RecursiveNonControl)target.Content.AddedObjects[0];

            Assert.Equal(0, addedNonControl.Content.AddedObjects.Count);
            Assert.Equal(new[] { "SomeText" }, addedNonControl.Content.AddedStrings);
        }
    }

    public class RecursiveNonControl : StyledElement
    {
        [Content]
        public SpecializedContentList Content { get; } = new SpecializedContentList();
    }

    public class InlineContainerControl : Control
    {
        [Content]
        public SpecializedContentList Content { get; } = new SpecializedContentList();
    }

    public class SpecializedContentList : ContentList<RecursiveNonControl>
    {
        public List<object> AddedStrings { get; } = new();

        void Add(string text)
        {
            AddedStrings.Add(text);
        }
    }

    public class ContentList<T> : IList, ICollection<T>
    {
        public List<object> AddedObjects { get; } = new();

        public List<T> AddedTyped { get; } = new();

        public int Add(object value)
        {
            AddedObjects.Add(value);
            return 0;
        }

        public void Add(T item)
        {
            AddedTyped.Add(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int Count { get; set; }
        public object SyncRoot { get; set; }
        public bool IsSynchronized { get; set; }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public object this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsReadOnly { get; set; }
        public bool IsFixedSize { get; set; }
    }
}
