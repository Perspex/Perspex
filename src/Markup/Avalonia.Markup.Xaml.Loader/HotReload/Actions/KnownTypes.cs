using System.Linq;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload.Actions
{
    internal class KnownTypes
    {
        public IXamlType Object { get; }
        public IXamlMethod ObjectToString { get; }

        public IXamlType Debug { get; }
        public IXamlMethod DebugWriteLine { get; }

        public IXamlType List { get; }
        public IXamlMethod ListGetItem { get; }
        public IXamlMethod ListInsert { get; }
        public IXamlMethod ListRemoveAt { get; }

        public KnownTypes(IXamlTypeSystem typeSystem)
        {
            List = typeSystem.GetType("System.Collections.IList");
            ListGetItem = List.Methods.First(x => x.Name == "get_Item");
            ListInsert = List.Methods.First(x => x.Name == "Insert");
            ListRemoveAt = List.Methods.First(x => x.Name == "RemoveAt");
            
            Object = typeSystem.GetType("System.Object");
            ObjectToString = Object.Methods.First(x => x.Name == "ToString");

            Debug = typeSystem.GetType("System.Diagnostics.Debug");
            DebugWriteLine = Debug.Methods.First(x => x.Name == "WriteLine");

        }
    }
}
