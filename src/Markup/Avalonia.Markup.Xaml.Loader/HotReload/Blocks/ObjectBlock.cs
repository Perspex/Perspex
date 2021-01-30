using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Markup.Xaml.HotReload.Blocks
{
    [DebuggerDisplay("{" + nameof(Type) + "}")]
    internal class ObjectBlock
    {
        private readonly List<PropertyBlock> _properties;
        private readonly List<ObjectBlock> _children;

        public string Type { get; }
        
        public ObjectBlock Parent { get; private set; }
        public PropertyBlock ParentProperty { get; }
        public int ParentIndex { get; private set; }

        public IReadOnlyList<PropertyBlock> Properties => _properties;
        public IReadOnlyList<ObjectBlock> Children => _children;

        public int NewObjectStartOffset { get; set; }
        public int NewObjectEndOffset { get; set; }

        public int InitializationStartOffset { get; set; }
        public int InitializationEndOffset { get; set; }
        public int InitializationLength => InitializationEndOffset - InitializationStartOffset;

        public ObjectBlock(string type, PropertyBlock parentProperty)
        {
            Type = type;
            ParentProperty = parentProperty;

            _properties = new List<PropertyBlock>();
            _children = new List<ObjectBlock>();
        }

        public void AddProperty(PropertyBlock property)
        {
            _properties.Add(property);
        }

        public void AddChild(ObjectBlock child)
        {
            child.Parent = this;
            child.ParentIndex = _children.Count;
            
            _children.Add(child);
        }
    }
}
