using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal class ClearPropertyAction : HotReloadAction
    {
        public ClearPropertyAction(
            List<PropertyBlock> propertyChain,
            IXamlTypeSystem typeSystem)
            :
            base(typeSystem)
        {
            // Exclude the last property. We do not want to call its getter. The instructions below
            // will call its setter.
            EmitPropertyChain(
                propertyChain.Take(propertyChain.Count - 1).ToList(),
                Array.Empty<RecordingIlEmitter.RecordedInstruction>());

            var property = propertyChain.Last();
            var xamlType = typeSystem.GetType(property.Type);

            // TODO: Can AvaloniaProperty have a different name?
            var avaloniaSetter = xamlType.Fields.SingleOrDefault(x => x.Name == property.Name + "Property");

            if (avaloniaSetter != null)
            {
                var unsetValue = TypeSystem
                    .GetType("Avalonia.AvaloniaProperty")
                    .Fields.First(f => f.Name == "UnsetValue");

                var setValue = TypeSystem
                    .GetType("Avalonia.AvaloniaObject")
                    .Methods.First(x => x.Name == "SetValue");

                IlEmitter
                    .Ldsfld(avaloniaSetter)
                    .Ldsfld(unsetValue)
                    .Ldc_I4(0)
                    .EmitCall(setValue, true);
            }
            else
            {
                // TODO: This setter assumes the type is a reference and default value is null.
                var setter = xamlType.Properties.Single(x => x.Name == property.Name).Setter;

                IlEmitter
                    .Emit(OpCodes.Ldnull)
                    .Emit(OpCodes.Callvirt, setter);
            }

            IlEmitter.Emit(OpCodes.Ret);
        }
    }
}
