using System.Collections.Generic;
using Avalonia.Markup.Xaml.HotReload.Actions;
using XamlX.IL;

namespace Avalonia.Markup.Xaml.HotReload
{
    public class HotReloadDiffer
    {
        public static List<IHotReloadAction> Diff<T>(string originalXaml, string modifiedXaml)
        {
            var oldInstructions = LoadXaml<T>(originalXaml, false);
            var newInstructions = LoadXaml<T>(modifiedXaml, false);

            var differ = new IlDiffer(oldInstructions, newInstructions);
            var diffToAction = new DiffToAction(AvaloniaXamlAstLoader.TypeSystem, newInstructions);

            var diff = differ.Diff();
            return diffToAction.ToActions(diff);
        }

        private static List<RecordingIlEmitter.RecordedInstruction> LoadXaml<T>(
            string xaml,
            bool patchIl)
        {
            return AvaloniaXamlAstLoader.Load(
                xaml,
                "Test.xaml",
                typeof(T),
                typeof(T).Assembly,
                patchIl: patchIl);
        }
    }
}
