using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Xunit;
using Xunit.Extensions;

namespace Avalonia.DesignerSupport.Tests
{
    public class DesignerSupportTests
    {
        private const string DesignerAppPath = "../../../../../src/tools/Avalonia.Designer.HostApp/bin/$BUILD/netcoreapp2.0/Avalonia.Designer.HostApp.dll";
        private readonly Xunit.Abstractions.ITestOutputHelper outputHelper;

        public DesignerSupportTests(Xunit.Abstractions.ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [SkippableTheory,
         InlineData(
            @"..\..\..\..\..\tests/Avalonia.DesignerSupport.TestApp/bin/$BUILD/net5.0/",
            "Avalonia.DesignerSupport.TestApp",
            "Avalonia.DesignerSupport.TestApp.dll",
            @"..\..\..\..\..\tests\Avalonia.DesignerSupport.TestApp\MainWindow.xaml",
            "win32"),
         InlineData(
            @"..\..\..\..\..\samples\ControlCatalog.NetCore\bin\$BUILD\net5.0\",
            "ControlCatalog.NetCore",
            "ControlCatalog.dll",
            @"..\..\..\..\..\samples\ControlCatalog\MainWindow.xaml",
            "win32"),
        InlineData(
            @"..\..\..\..\..\tests/Avalonia.DesignerSupport.TestApp/bin/$BUILD/net5.0/",
            "Avalonia.DesignerSupport.TestApp",
            "Avalonia.DesignerSupport.TestApp.dll",
            @"..\..\..\..\..\tests\Avalonia.DesignerSupport.TestApp\MainWindow.xaml",
            "avalonia-remote"),
        InlineData(
            @"..\..\..\..\..\samples\ControlCatalog.NetCore\bin\$BUILD\net5.0\",
            "ControlCatalog.NetCore",
            "ControlCatalog.dll",
            @"..\..\..\..\..\samples\ControlCatalog\MainWindow.xaml",
            "avalonia-remote")]
        public async Task Designer_In_Win32_Mode_Should_Provide_Valid_Hwnd(
            string outputDir,
            string executableName,
            string assemblyName,
            string xamlFile,
            string method)
        {
            outputDir = Path.GetFullPath(outputDir.Replace('\\', Path.DirectorySeparatorChar));
            xamlFile = Path.GetFullPath(xamlFile.Replace('\\', Path.DirectorySeparatorChar));
            
            if (method == "win32")
                Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var xaml = File.ReadAllText(xamlFile);
            string buildType;
#if DEBUG
            buildType = "Debug";
#else
            buildType = "Release";
#endif
            outputDir = outputDir.Replace("$BUILD", buildType);

            var sessionId = Guid.NewGuid();
            long handle = 0;
            bool success = false;
            string error = null;

            var resultMessageReceivedToken = new CancellationTokenSource();

            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            var transport = new BsonTcpTransport();
            transport.Listen(IPAddress.Loopback, port, conn =>
            {
                conn.OnMessage += async (_, msg) =>
                {
                    if (msg is StartDesignerSessionMessage start)
                    {
                        Assert.Equal(sessionId, Guid.Parse(start.SessionId));
                        if (method == "avalonia-remote")
                        {
                            await conn.Send(new ClientSupportedPixelFormatsMessage
                            {
                                Formats = new[] { PixelFormat.Rgba8888 }
                            });
                            await conn.Send(new ClientViewportAllocatedMessage
                            {
                                DpiX = 96, DpiY = 96, Width = 1024, Height = 768
                            });
                        }

                        await conn.Send(new UpdateXamlMessage
                        {
                            AssemblyPath = Path.Combine(outputDir, assemblyName),
                            Xaml = xaml
                        });
                    }
                    else if (msg is UpdateXamlResultMessage result)
                    {
                        if (result.Error != null)
                        {
                            error = result.Error;
                            outputHelper.WriteLine(result.Error);
                        }
                        else
                            success = true;
                        if (method == "win32")
                            handle = result.Handle != null ? long.Parse(result.Handle) : 0;
                        resultMessageReceivedToken.Cancel();
                        conn.Dispose();
                    }
                };
            });

            var cmdline =
                $"exec --runtimeconfig \"{outputDir}{executableName}.runtimeconfig.json\" --depsfile \"{outputDir}{executableName}.deps.json\" "
                + $" \"{DesignerAppPath.Replace("$BUILD", buildType)}\" "
                + $"--transport tcp-bson://127.0.0.1:{port}/ --session-id {sessionId} --method {method} \"{outputDir}{executableName}.dll\"";

            using (var proc = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet", cmdline)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = outputDir,
                },
                EnableRaisingEvents = true
            })
            {
                proc.Start();

                var cancelled = false;
                try
                {
                    await Task.Delay(10000, resultMessageReceivedToken.Token);
                }
                catch (TaskCanceledException)
                {
                    cancelled = true;
                }

                try
                {
                    proc.Kill();
                }
                catch
                {
                    //
                }

                proc.WaitForExit();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                Assert.True(cancelled,
                    $"Message Not Received.\n" + proc.StandardOutput.ReadToEnd() + "\n" +
                    stderr + "\n" + stdout);
                Assert.True(success, error);
                if (method == "win32")
                    Assert.NotEqual(0, handle);
                

            }
        }
    }
}
