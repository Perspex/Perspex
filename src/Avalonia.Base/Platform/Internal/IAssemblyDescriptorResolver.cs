namespace Avalonia.Platform.Internal
{
    internal interface IAssemblyDescriptorResolver
    {
        IAssemblyDescriptor Get(string name);
    }
}
