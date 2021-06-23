namespace Avalonia.Platform
{
    public interface IModuleEnvironmentChecker
    {
        bool IsCompatible { get; }
    }
}