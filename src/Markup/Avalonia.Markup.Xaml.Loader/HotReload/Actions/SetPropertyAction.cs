using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal class SetPropertyAction : HotReloadAction
    {
        public SetPropertyAction(
            IReadOnlyCollection<PropertyBlock> propertyChain,
            IEnumerable<RecordingIlEmitter.RecordedInstruction> instructions,
            IXamlTypeSystem typeSystem,
            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInstructions)
            :
            base(typeSystem)
        {
            EmitContext(contextInstructions);
            
            // Exclude the last property. We do not want to call its getter. The instructions below
            // will call its setter.
            EmitPropertyChain(
                propertyChain.Take(propertyChain.Count - 1).ToList(),
                contextInstructions);

            foreach (var instruction in instructions)
            {
                EmitInstruction(instruction);
            }

            IlEmitter.Emit(OpCodes.Ret);
        }
    }
}
