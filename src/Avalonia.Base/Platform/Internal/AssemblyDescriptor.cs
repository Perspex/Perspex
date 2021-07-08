using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Platform.Internal
{
    internal interface IAssemblyDescriptor
    {
        Assembly Assembly { get; }
        Dictionary<string, IAssetDescriptor> Resources { get; }
        Dictionary<string, IAssetDescriptor> AvaloniaResources { get; }
        string Name { get; }
    }

    internal class AssemblyDescriptor : IAssemblyDescriptor
    {
        public AssemblyDescriptor(Assembly assembly)
        {
            Assembly = assembly;

            if (assembly != null)
            {
                Resources = assembly.GetManifestResourceNames()
                    .ToDictionary(n => n, n => (IAssetDescriptor)new AssemblyResourceDescriptor(assembly, n));
                Name = assembly.GetName().Name;
                using (var resources = assembly.GetManifestResourceStream(Constants.AvaloniaResourceName))
                {
                    if (resources != null)
                    {
                        Resources.Remove(Constants.AvaloniaResourceName);

                        var indexLength = new BinaryReader(resources).ReadInt32();
                        var index = AvaloniaResourcesIndexReaderWriter.Read(new SlicedStream(resources, 4, indexLength));
                        var baseOffset = indexLength + 4;
                        AvaloniaResources = index.ToDictionary(
                            GetPathRooted,
                            r => (IAssetDescriptor) new AvaloniaResourceDescriptor(
                                assembly, baseOffset + r.Offset, r.Size));
                    }
                }
            }
        }

        public Assembly Assembly { get; }
        public Dictionary<string, IAssetDescriptor> Resources { get; }
        public Dictionary<string, IAssetDescriptor> AvaloniaResources { get; }
        public string Name { get; }

        private static string GetPathRooted(AvaloniaResourcesIndexEntry r) =>
            r.Path[0] == '/' ? r.Path : '/' + r.Path;
    }
}
