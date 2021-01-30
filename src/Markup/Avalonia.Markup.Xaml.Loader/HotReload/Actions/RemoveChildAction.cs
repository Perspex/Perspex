using System;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal class RemoveChildAction : HotReloadAction
    {
        public RemoveChildAction(
            ObjectBlock block,
            IXamlTypeSystem typeSystem)
            :
            base(typeSystem)
        {
            var propertyChain = block.ParentProperty.GetPropertyChain();
            var contextInstructions = Array.Empty<RecordingIlEmitter.RecordedInstruction>();

            // Skip the last property. It will be a list property with an indexer, but we do not
            // want to call the getItem on it.
            EmitPropertyChain(
                propertyChain.Take(propertyChain.Count - 1).ToArray(),
                contextInstructions);

            // Call the last property call with emitGetItemIfList: false to load the collection
            // instead of getting the item in that collection.
            EmitPropertyCall(propertyChain.Last(), contextInstructions, emitGetItemIfList: false);
            
            IlEmitter.Emit(OpCodes.Ldc_I4, block.ParentIndex);
            IlEmitter.Emit(OpCodes.Callvirt, KnownTypes.ListRemoveAt);

            IlEmitter.Emit(OpCodes.Ret);
        }
    }
}
