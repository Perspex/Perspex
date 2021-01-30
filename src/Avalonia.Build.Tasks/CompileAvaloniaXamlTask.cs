using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out MessageImportance outputImportance);

            OutputPath = OutputPath ?? AssemblyFile;
            var outputPdb = GetPdbPath(OutputPath);
            var input = AssemblyFile;
            var inputPdb = GetPdbPath(input);
            // Make a copy and delete the original file to prevent MSBuild from thinking that everything is OK 
            if (OriginalCopyPath != null)
            {
                File.Copy(AssemblyFile, OriginalCopyPath, true);
                input = OriginalCopyPath;
                File.Delete(AssemblyFile);

                if (File.Exists(inputPdb))
                {
                    var copyPdb = GetPdbPath(OriginalCopyPath);
                    File.Copy(inputPdb, copyPdb, true);
                    File.Delete(inputPdb);
                    inputPdb = copyPdb;
                }
            }

            var msg = $"CompileAvaloniaXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{OutputPath}";
            BuildEngine.LogMessage(msg, outputImportance < MessageImportance.Low ? MessageImportance.High : outputImportance);

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine, input,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(),
                ProjectDirectory, OutputPath, VerifyIl, outputImportance,
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null,
                EnableComInteropPatching, SkipXamlCompilation);
            if (!res.Success)
                return false;
            if (!res.WrittenFile)
            {
                File.Copy(input, OutputPath, true);
                if(File.Exists(inputPdb))
                    File.Copy(inputPdb, outputPdb, true);
            }
            return true;
        }

        private string GetPdbPath(string p)
        {
            var d = Path.GetDirectoryName(p);
            var f = Path.GetFileNameWithoutExtension(p);
            var rv = f + ".pdb";
            if (d != null)
                rv = Path.Combine(d, rv);
            return rv;
        }
        
        [Required]
        public string AssemblyFile { get; set; }
        [Required]
        public string ReferencesFilePath { get; set; }
        [Required]
        public string OriginalCopyPath { get; set; }
        [Required]
        public string ProjectDirectory { get; set; }
        
        public string OutputPath { get; set; }

        public bool VerifyIl { get; set; }
        
        public bool EnableComInteropPatching { get; set; }
        public bool SkipXamlCompilation { get; set; }
        
        public string AssemblyOriginatorKeyFile { get; set; }
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        public string ReportImportance { get; set; }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
    }
}
