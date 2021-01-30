using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal static class InstructionExtensions
    {
        public static bool IsStartSetPropertyMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("StartSetPropertyMarker");
        }

        public static bool IsEndSetPropertyMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("EndSetPropertyMarker");
        }

        public static bool IsStartObjectInitializationMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("StartObjectInitializationMarker");
        }

        public static bool IsEndObjectInitializationMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("EndObjectInitializationMarker");
        }

        public static bool IsAddChildMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("AddChildMarker");
        }

        public static bool IsStartContextInitializationMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("StartContextInitializationMarker");
        }

        public static bool IsEndContextInitializationMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("EndContextInitializationMarker");
        }

        public static bool IsStartNewObjectMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("StartNewObjectMarker");
        }

        public static bool IsEndNewObjectMarker(this RecordingIlEmitter.RecordedInstruction instruction)
        {
            return instruction.IsXamlMarker("EndNewObjectMarker");
        }
        
        private static bool IsXamlMarker(this RecordingIlEmitter.RecordedInstruction instruction, string methodName)
        {
            if (!(instruction.Operand is IXamlMethod method))
            {
                return false;
            }

            if (method.DeclaringType?.ToString() != "Avalonia.Markup.Xaml.HotReload.XamlMarkers")
            {
                return false;
            }

            return method.Name == methodName;
        }
    }
}
