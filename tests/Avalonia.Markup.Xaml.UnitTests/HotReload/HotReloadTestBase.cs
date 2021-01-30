using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.HotReload;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class TestControl : UserControl
    {
    }

    public class HotReloadTestBase : XamlTestBase
    {
        protected (T Original, T Modified) ParseAndApplyHotReload<T>(string xaml, string modifiedXaml)
        {
            var original = AvaloniaRuntimeXamlLoader.Parse<T>(xaml);
            var modified = AvaloniaRuntimeXamlLoader.Parse<T>(modifiedXaml);
            
            var actions = HotReloadDiffer.Diff<T>(xaml, modifiedXaml);

            foreach (var action in actions)
            {
                action.Apply(original);
            }

            return (original, modified);
        }

        protected void SetLogLevel(LogEventLevel level)
        {
            Logger.Sink = new TraceLogSink(level, new[] { "HotReload" });
        }

        protected void Compare<T>(string originalXaml, string modifiedXaml, string comparison)
        {
            var (original, modified) = ParseAndApplyHotReload<T>(originalXaml, modifiedXaml);
            
            var parentStack = new Stack<ObjectNode>();
            var lines = comparison.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            var propertyWithIndexRegex = new Regex(@"(.+)#(.+)\((.*)\)");
            var propertyWithTypeRegex = new Regex(@"(.+)\((.+)\)");

            var root = new ObjectNode(typeof(T).Name);
            var current = root;

            foreach (var line in lines)
            {
                var indentation = line
                    .TakeWhile(x => x == ' ')
                    .Count();

                var level = indentation / 2;

                while (level < parentStack.Count)
                {
                    current = parentStack.Pop();
                }

                string trimmedLine = line.Trim();

                var indexMatch = propertyWithIndexRegex.Match(trimmedLine);
                var typeMatch = propertyWithTypeRegex.Match(trimmedLine);

                if (indexMatch.Success)
                {
                    var property = indexMatch.Groups[1].Captures[0].Value;
                    var index = indexMatch.Groups[2].Captures[0].Value;
                    var type = indexMatch.Groups[3].Captures[0].Value;

                    var objectNode = new ObjectNode(type);
                    var propertyNode = new PropertyNode(property, objectNode, current, int.Parse(index));

                    current.Properties.Add(propertyNode);
                    parentStack.Push(current);

                    current = objectNode;
                }
                else if (typeMatch.Success)
                {
                    var property = typeMatch.Groups[1].Captures[0].Value;
                    var type = typeMatch.Groups[2].Captures[0].Value;

                    var objectNode = new ObjectNode(type);
                    var propertyNode = new PropertyNode(property, objectNode, current);

                    current.Properties.Add(propertyNode);
                    parentStack.Push(current);

                    current = objectNode;
                }
                else
                {
                    var property = trimmedLine;
                    var propertyNode = new PropertyNode(property, null, current);

                    current.Properties.Add(propertyNode);
                }
            }

            root.Print();
            root.Compare(original, modified);
        }

        private class ObjectNode
        {
            public string Type { get; }
            public List<PropertyNode> Properties { get; }
            public PropertyNode ParentProperty { get; set; }

            public ObjectNode(string type)
            {
                Type = type;
                Properties = new List<PropertyNode>();
            }

            public void Print(int indentation = 0)
            {
                foreach (var property in Properties)
                {
                    property.Print(indentation);
                }
            }

            public void Compare(object first, object second)
            {
                string message = $"Types do not match. {ToString()}. Expected: {Type}, Actual: ";

                Logger
                    .TryGet(LogEventLevel.Information, "HotReload")
                    ?.Log(null, "{Object}", this);

                Assert.True(Type.Equals(first.GetType().Name), $"{message}{first.GetType().Name}");
                Assert.True(Type.Equals(second.GetType().Name), $"{message}{second.GetType().Name}");

                foreach (var property in Properties)
                {
                    property.Compare(first, second);
                }
            }

            public override string ToString()
            {
                return ParentProperty?.ToString();
            }
        }

        private class PropertyNode
        {
            private readonly string _property;
            private readonly ObjectNode _objectNode;
            private readonly ObjectNode _ownerObjectNode;
            private readonly int _index;

            public PropertyNode(
                string property,
                ObjectNode objectNode,
                ObjectNode ownerObjectNode,
                int index = -1)
            {
                _property = property;
                _objectNode = objectNode;
                _ownerObjectNode = ownerObjectNode;
                _index = index;

                if (_objectNode != null)
                {
                    _objectNode.ParentProperty = this;
                }
            }

            public void Compare(object first, object second)
            {
                first = GetValue(first);
                second = GetValue(second);

                if (_objectNode == null)
                {
                    Logger
                        .TryGet(LogEventLevel.Information, "HotReload")
                        ?.Log(null, "{Property}", this);
                    
                    Assert.Equal(first, second);
                }
                else
                {
                    _objectNode.Compare(first, second);
                }
            }

            public void Print(int indentation)
            {
                var indentationString = new string(' ', indentation * 2);

                var indexString = _index < 0
                        ? ""
                        : $"[{_index}]";

                var typeString = _objectNode == null
                        ? ""
                        : $"({_objectNode?.Type})";
                
                Logger
                    .TryGet(LogEventLevel.Verbose, "HotReload")
                    ?.Log(null, "{Indentation}- {Property}{Index}{Type}", indentationString, _property, indexString, typeString);
                
                _objectNode?.Print(indentation + 1);
            }

            public override string ToString()
            {
                var indexString = _index < 0
                    ? ""
                    : $"[{_index}]";

                var typeString = _objectNode == null
                    ? ""
                    : $"({_objectNode?.Type})";

                var ownerString = _ownerObjectNode == null
                    ? ""
                    : $"{_ownerObjectNode}.";

                return $"{ownerString}{_property}{indexString}{typeString}";
            }

            private object GetValue(object obj)
            {
                var type = obj.GetType();

                var propertyChain = _property.Split('.');

                var value = obj;

                foreach (var property in propertyChain)
                {
                    var propertyInfo = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
                    Assert.True(propertyInfo != null, $"Cannot find property: {property} in {type}");
                    Assert.True(value != null, "Value is null.");

                    value = propertyInfo.GetValue(value);

                    if (value != null)
                    {
                        type = value.GetType();
                    }
                }

                if (_index >= 0)
                {
                    var countProperty = type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
                    var indexer = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
                    
                    Assert.True(countProperty != null, $"Cannot find Count property in {type}");
                    Assert.True(indexer != null, $"Cannot find get_Item method in {type}");

                    int count = (int) countProperty.GetValue(value);
                    
                    Assert.True(_index < count, $"Index out of range. Count: {count}, Index: {_index}");
                    
                    value = indexer.Invoke(value, new object[] { _index });
                }

                return value;
            }
        }
    }
}
