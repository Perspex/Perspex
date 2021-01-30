using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.HotReload.Actions;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class DiffToAction
    {
        private readonly IXamlTypeSystem _typeSystem;
        private readonly List<RecordingIlEmitter.RecordedInstruction> _newInstructions;
        private readonly List<RecordingIlEmitter.RecordedInstruction> _contextInitializationInstructions;
       
        public DiffToAction(
            IXamlTypeSystem typeSystem,
            List<RecordingIlEmitter.RecordedInstruction> newInstructions)
        {
            _typeSystem = typeSystem;
            _newInstructions = newInstructions;

            _contextInitializationInstructions = GetContextInitializationInstructions();
        }
        
        public List<IHotReloadAction> ToActions(Diff diff)
        {
            var actions = new List<IHotReloadAction>();
            
            foreach (var (_, property) in diff.PropertyMap)
            {
                var action = CreateSetPropertyAction(property);
                actions.Add(action);
            }

            foreach (var property in diff.AddedProperties)
            {
                var action = CreateSetPropertyAction(property);
                actions.Add(action);
            }

            foreach (var property in diff.RemovedProperties)
            {
                var action = new ClearPropertyAction(property.GetPropertyChain(), _typeSystem);
                actions.Add(action);
            }

            foreach (var block in diff.AddedBlocks)
            {
                var action = new AddChildAction(
                    block,
                    _typeSystem,
                    _newInstructions,
                    _contextInitializationInstructions);

                actions.Add(action);
            }

            foreach (var block in diff.RemovedBlocks)
            {
                var action = new RemoveChildAction(block, _typeSystem);
                actions.Add(action);
            }

            return actions;
        }

        private SetPropertyAction CreateSetPropertyAction(PropertyBlock property)
        {
            var instructions = _newInstructions
                .Skip(property.StartOffset)
                .Take(property.Length)
                .ToList();

            // TODO: Better detection of context usage.
            bool hasContext = instructions.Any(x =>
                x.OpCode == OpCodes.Ldloc &&
                (x.Operand is IXamlLocal || x.Operand is RecordingIlEmitter.LocalInfo));

            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInitializationInstructions = hasContext
                ? (IReadOnlyList<RecordingIlEmitter.RecordedInstruction>)_contextInitializationInstructions
                : Array.Empty<RecordingIlEmitter.RecordedInstruction>();

            return new SetPropertyAction(
                property.GetPropertyChain(),
                instructions,
                _typeSystem,
                contextInitializationInstructions);
        }
        
        private List<RecordingIlEmitter.RecordedInstruction> GetContextInitializationInstructions()
        {
            var result = new List<RecordingIlEmitter.RecordedInstruction>();
            var collectInstructions = false;

            foreach (var instruction in _newInstructions)
            {
                if (instruction.IsStartContextInitializationMarker())
                {
                    collectInstructions = true;
                    continue;
                }

                if (instruction.IsEndContextInitializationMarker())
                {
                    break;
                }

                if (collectInstructions)
                {
                    result.Add(instruction);
                }
            }

            return result;
        }
    }
}
