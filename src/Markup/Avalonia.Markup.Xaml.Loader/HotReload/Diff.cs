using System.Collections.Generic;
using Avalonia.Markup.Xaml.HotReload.Blocks;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class Diff
    {
        public Dictionary<ObjectBlock, ObjectBlock> BlockMap { get; }
        public List<ObjectBlock> AddedBlocks { get; }
        public List<ObjectBlock> RemovedBlocks { get; }

        public List<(PropertyBlock OldProperty, PropertyBlock NewProperty)> PropertyMap { get; }
        public List<PropertyBlock> AddedProperties { get; }
        public List<PropertyBlock> RemovedProperties { get; }

        public Diff()
        {
            BlockMap = new Dictionary<ObjectBlock, ObjectBlock>();
            AddedBlocks = new List<ObjectBlock>();
            RemovedBlocks = new List<ObjectBlock>();

            PropertyMap = new List<(PropertyBlock, PropertyBlock)>();
            AddedProperties = new List<PropertyBlock>();
            RemovedProperties = new List<PropertyBlock>();
        }
    }
}
