using System;
using System.IO;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class Program
    {
        private const string _originalDll = "original.dll";
        private const string _references = "references";
        private const string _outDll = "out.dll";

        static int Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 3)
            {
                const string referencesOutputPath = "path/to/Avalonia/samples/Sandbox/obj/Debug/netcoreapp3.1/Avalonia";
                Console.WriteLine(@$"Usage:
    1) dotnet ./Avalonia.Build.Tasks.dll <ReferencesOutputPath>
       , where <ReferencesOutputPath> likes {referencesOutputPath}
    2) dotnet ./Avalonia.Build.Tasks.dll <AssemblyFilePath> <ReferencesFilePath> <OutputPath>
       , where:
           - <AssemblyFilePath> likes {referencesOutputPath}/{_originalDll}
           - <ReferencesFilePath> likes {referencesOutputPath}/{_references}
           - <OutputPath> likes {referencesOutputPath}/{_outDll}");
                return 1;
            }

            try
            {
                var task = args.Length == 1
                    ? CreateTask(args[0])
                    : CreateTask(args[0], args[1], args[2]);
                return task.Execute() ? 0 : 2;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return -1;
            }
        }

        private static ITask CreateTask(string referenceOutputPath)
        {
            var directory = new DirectoryInfo(referenceOutputPath);
            return CreateTask(
                Path.Combine(directory.FullName, _originalDll),
                Path.Combine(directory.FullName, _references),
                Path.Combine(directory.FullName, _outDll));
        }

        private static ITask CreateTask(string assemblyFilePath, string referencesFilePath, string outputPath) =>
            new CompileAvaloniaXamlTask
            {
                AssemblyFile = new FileInfo(assemblyFilePath).FullName,
                ReferencesFilePath = new FileInfo(referencesFilePath).FullName,
                OutputPath = new FileInfo(outputPath).FullName,
                BuildEngine = new ConsoleBuildEngine(),
                ProjectDirectory = Directory.GetCurrentDirectory(),
                VerifyIl = true,
                EnableComInteropPatching = true
            };
    }
}
