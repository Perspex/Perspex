using System;
using System.IO;
using System.Linq;

namespace Avalonia.Build.Tasks
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                if (args.Length == 1)
                    args = new[] {"original.dll", "references", "out.dll"}
                        .Select(x => Path.Combine(args[0], x)).ToArray();
                else
                {
                    Console.Error.WriteLine("input references output");
                    return 1;
                }
            }

            var task = CompileAvaloniaXamlTask.Create(args[0], args[1], args[2]);
            return task.Execute() ? 0 : 2;
        }
    }
}
