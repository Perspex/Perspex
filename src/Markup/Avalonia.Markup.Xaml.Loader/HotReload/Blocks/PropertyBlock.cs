using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Avalonia.Markup.Xaml.HotReload.Blocks
{
    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    internal class PropertyBlock
    {
        public string Type { get; }
        public string Name { get; }

        public int StartOffset { get; }
        public int EndOffset { get; set; }
        public int Length => EndOffset - StartOffset + 1;

        public bool IsList { get; }
        public int Index { get; }

        public ObjectBlock Parent { get; }

        public PropertyBlock(
            ObjectBlock parent,
            string type,
            string property,
            int startOffset,
            bool isList = false,
            int index = 0)
        {
            Parent = parent;
            Type = type;
            Name = property;
            StartOffset = startOffset;
            IsList = isList;
            Index = index;
        }
        
        public List<PropertyBlock> GetPropertyChain()
        {
            var properties = new List<PropertyBlock>
            {
               this
            };

            var parent = Parent;

            while (parent != null)
            {
                if (parent.ParentProperty != null)
                {
                    properties.Add(parent.ParentProperty);
                }
                parent = parent.Parent;
            }

            properties.Reverse();

            return properties;
        }

        public override string ToString()
        {
            var properties = GetPropertyChain()
                .Select(x =>
                {
                    string property = $"{x.Name}";

                    if (x.IsList)
                    {
                        property += $"[{x.Index}]";
                    }
                    
                    return property;
                });
            
            return string.Join(".", properties);
        }

        private string GetDebuggerDisplay()
        {
            var result = $"{Type}.{Name}";

            if (IsList)
            {
                result += $"[{Index}]";
            }

            return result;
        }
    }
}
