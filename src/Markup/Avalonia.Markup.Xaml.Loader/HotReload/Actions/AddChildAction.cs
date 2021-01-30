using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal class AddChildAction : HotReloadAction
    {
        public AddChildAction(
            ObjectBlock block,
            IXamlTypeSystem typeSystem,
            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> instructions,
            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInstructions)
            :
            base(typeSystem)
        {
            var propertyChain = block.ParentProperty.GetPropertyChain();
            
            EmitContext(contextInstructions);

            // Skip the last property. It will be a list property with an indexer, but we do not
            // want to call the getItem on it.
            EmitPropertyChain(
                propertyChain.Take(propertyChain.Count - 1).ToArray(),
                contextInstructions);

            // Call the last property call with emitGetItemIfList: false to load the collection
            // instead of getting the item in that collection.
            EmitPropertyCall(propertyChain.Last(), contextInstructions, emitGetItemIfList: false);

            // These instructions will create the new child.
            for (int i = block.NewObjectStartOffset; i < block.NewObjectEndOffset; i++)
            {
                var instruction = instructions[i];
                EmitInstruction(instruction);
            }

            // Store the created object to a new local. We should push the index to the stack
            // before the object.
            var local = IlEmitter.DefineLocal(TypeSystem.FindType(block.Type));
            IlEmitter.Emit(OpCodes.Stloc, local);

            IlEmitter.Emit(OpCodes.Ldc_I4, block.ParentIndex);
            IlEmitter.Emit(OpCodes.Ldloc, local);
            IlEmitter.Emit(OpCodes.Callvirt, KnownTypes.ListInsert);

            if (block.InitializationLength > 0)
            {
                // Load the object again to initialize it.
                IlEmitter.Emit(OpCodes.Ldloc, local);

                for (int i = block.InitializationStartOffset; i < block.InitializationEndOffset; i++)
                {
                    var instruction = instructions[i];
                    EmitInstruction(instruction);
                }
            }

            IlEmitter.Emit(OpCodes.Ret);
        }
    }
}
