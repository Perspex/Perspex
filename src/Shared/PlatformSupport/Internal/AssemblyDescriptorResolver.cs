using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Platform.Internal;

namespace Avalonia.Shared.PlatformSupport.Internal
{
    internal class AssemblyDescriptorResolver : IAssemblyDescriptorResolver
    {
        private readonly Dictionary<string, IAssemblyDescriptor> _assemblyNameCache
            = new Dictionary<string, IAssemblyDescriptor>();
        
        public IAssemblyDescriptor Get(string name)
        {
            if (!_assemblyNameCache.TryGetValue(name, out var descriptor))
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var match = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == name);
                if (match != null)
                {
                    _assemblyNameCache[name] = descriptor = new AssemblyDescriptor(match);
                }
                else
                {
#if __IOS__
                    // iOS does not support loading assemblies dynamically!
                    throw new InvalidOperationException(
                        $"Assembly {name} needs to be referenced and explicitly loaded before loading resources");
#else
                    _assemblyNameCache[name] = descriptor = new AssemblyDescriptor(Assembly.Load(name));
#endif
                }
            }

            return descriptor;
        }
    }
}
