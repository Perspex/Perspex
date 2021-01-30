using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Mono.Reflection;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal abstract class HotReloadAction : IHotReloadAction
    {
        private readonly TypeBuilder _typeBuilder;
        private readonly IXamlMethodBuilder<IXamlILEmitter> _methodBuilder;
        private MethodInfo _method;

        protected KnownTypes KnownTypes { get; }

        protected Dictionary<object, IXamlLocal> Locals { get; }
        protected Dictionary<object, IXamlLabel> Labels { get; }
        
        protected IXamlTypeSystem TypeSystem { get; }
        protected IXamlILEmitter IlEmitter { get; }
        
        public HotReloadAction(IXamlTypeSystem typeSystem)
        {
            Locals = new Dictionary<object, IXamlLocal>();
            Labels = new Dictionary<object, IXamlLabel>();

            TypeSystem = typeSystem;
            KnownTypes = new KnownTypes(TypeSystem);

            _typeBuilder = AssemblyBuilder
                .DefineDynamicAssembly(
                    new AssemblyName(Guid.NewGuid().ToString()),
                    AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule(Guid.NewGuid().ToString())
                .DefineType(
                    Guid.NewGuid().ToString(),
                    TypeAttributes.Class | TypeAttributes.Public);
            
            var xamlTypeBuilder = ((SreTypeSystem)TypeSystem)
                .CreateTypeBuilder(_typeBuilder);

            _methodBuilder = xamlTypeBuilder.DefineMethod(
                TypeSystem.FindType("System.Void"),
                new[] { TypeSystem.FindType("System.IServiceProvider"), TypeSystem.FindType("System.Object") },
                Guid.NewGuid().ToString(),
                true,
                true,
                false);

            IlEmitter = new RecordingIlEmitter(_methodBuilder.Generator);
        }

        protected void EmitContext(IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInstructions)
        {
            foreach (var instruction in contextInstructions)
            {
                EmitInstruction(instruction);
            }
            
            // Set the root object of the context to the target object.
            var contextLocal = GetContextLocal();
            var contextRootObject = GetContextType()?.Fields.First(x => x.Name == "RootObject");
            
            if (contextInstructions.Count > 0)
            {
                IlEmitter.Emit(OpCodes.Ldloc, contextLocal);
                IlEmitter.Emit(OpCodes.Ldarg_1);
                IlEmitter.Emit(OpCodes.Stfld, contextRootObject);
            }
        }

        protected void EmitPropertyChain(
            IReadOnlyList<PropertyBlock> propertyChain,
            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInstructions,
            bool printObjects = false)
        {
            // Load the object that is sent to the method as the second parameter.
            // (First one is the service provider.)
            IlEmitter.Emit(OpCodes.Ldarg_1);

            foreach (var property in propertyChain)
            {
                EmitPropertyCall(property, contextInstructions, printObject: printObjects);
            }
        }

        protected void EmitPropertyCall(
            PropertyBlock property,
            IReadOnlyList<RecordingIlEmitter.RecordedInstruction> contextInstructions,
            bool emitGetItemIfList = true,
            bool printObject = false)
        {
            var type = TypeSystem.GetType(property.Type);

            if (printObject)
            {
                // Emit Debug.WriteLine(obj.ToString());
                IlEmitter.Emit(OpCodes.Dup);
                IlEmitter.Emit(OpCodes.Callvirt, KnownTypes.ObjectToString);
                IlEmitter.Emit(OpCodes.Call, KnownTypes.DebugWriteLine);
            }

            // Cast the object to its actual type.
            IlEmitter.Emit(OpCodes.Castclass, type);

            // If we have context, then push each parent to it.
            // TODO: Delete? Object initialization already have a push parent call.
            if (/*i < propertyChain.Count - 1 && */contextInstructions.Count > 0)
            {
                var contextLocal = GetContextLocal();
                var contextPushParent = GetContextType()?.FindMethod(x => x.Name == "PushParent");

                var local = IlEmitter.DefineLocal(type);

                // Store the current object in a local to be able to push the context local to the stack
                // before it.
                IlEmitter.Emit(OpCodes.Dup);
                IlEmitter.Emit(OpCodes.Stloc, local);
                IlEmitter.Emit(OpCodes.Ldloc, contextLocal);
                IlEmitter.Emit(OpCodes.Ldloc, local);
                IlEmitter.Emit(OpCodes.Callvirt, contextPushParent);
            }

            // Emit a call to the property getter.
            var getter = type.Properties.Single(x => x.Name == property.Name).Getter;
            IlEmitter.Emit(OpCodes.Callvirt, getter);

            if (property.IsList && emitGetItemIfList)
            {
                // If the property is a list, then get the item at the requested index.
                IlEmitter.Emit(OpCodes.Ldc_I4, property.Index);
                IlEmitter.Emit(OpCodes.Callvirt, KnownTypes.ListGetItem);
            }
        }

        protected void EmitWriteLine(string message)
        {
            IlEmitter.Emit(OpCodes.Ldstr, message);
            IlEmitter.Emit(OpCodes.Call, KnownTypes.DebugWriteLine);
        }

        private IXamlType GetContextType()
        {
            var contextLocal = GetContextLocal();
            
            // TODO: Find a better way to get the context type.
            return contextLocal
                ?.GetType()
                .GetProperty("XamlType", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(contextLocal) as IXamlType;
        }

        private IXamlLocal GetContextLocal()
        {
            // TODO: Find a better way to get the context local.
            return Locals.Values.FirstOrDefault();
        }

        public void Apply(object target)
        {
            if (_method == null)
            {
                _method = _typeBuilder.CreateTypeInfo().AsType().GetMethod(_methodBuilder.Name);

                var logger = Logger.TryGet(LogEventLevel.Debug, "HotReload");

                if (logger != null)
                {
                    foreach (var instruction in _method.GetInstructions())
                    {
                        logger.Value.Log(null, "{Instruction}", instruction);
                    }
                }
            }

            _method.Invoke(null, new[]
            {
                XamlIlRuntimeHelpers.CreateRootServiceProviderV2(),
                target
            });
        }

        protected void EmitInstruction(RecordingIlEmitter.RecordedInstruction instruction)
        {
            if (instruction.Operand == null)
            {
                IlEmitter.Emit(instruction.OpCode);
                return;
            }

            switch (instruction.Operand)
            {
                case double x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case int x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case long x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case float x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case string x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlField x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlMethod x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlConstructor x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlType x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlLabel x:
                    IlEmitter.Emit(instruction.OpCode, x);
                    break;
                case IXamlLocal x:
                    if (instruction.OpCode == OpCodes.Stloc)
                    {
                        var xamlType = (IXamlType)x.GetType().GetProperty("XamlType").GetValue(x);

                        var local = IlEmitter.DefineLocal(xamlType);
                        Locals[x] = local;
                        IlEmitter.Emit(instruction.OpCode, local);
                    }
                    else if (instruction.OpCode == OpCodes.Ldloc)
                    {
                        var local = Locals[x];
                        IlEmitter.Emit(instruction.OpCode, local);
                    }
                    break;
                case RecordingIlEmitter.LocalInfo x:
                    if (instruction.OpCode == OpCodes.Stloc)
                    {
                        var local = IlEmitter.DefineLocal(x.Type);
                        Locals[x] = local;
                        IlEmitter.Emit(instruction.OpCode, local);
                    }
                    else if (instruction.OpCode == OpCodes.Ldloc)
                    {
                        var local = Locals[x];
                        IlEmitter.Emit(instruction.OpCode, local);
                    }
                    break;
                case RecordingIlEmitter.LabelInfo x:
                    if (!Labels.TryGetValue(x, out var label))
                    {
                        label = IlEmitter.DefineLabel();
                        Labels[x] = label;
                    }
                    
                    IlEmitter.Emit(instruction.OpCode, label);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}
