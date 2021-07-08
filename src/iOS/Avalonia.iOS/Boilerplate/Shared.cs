using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Platform.Internal;
using Avalonia.Platform.Interop;
using Avalonia.Utilities;

namespace Avalonia.Shared.PlatformSupport
{
    static class StandardRuntimePlatformServices
    {
        public static void Register(Assembly assembly = null)
        {
            var standardPlatform = new StandardRuntimePlatform();
            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToConstant(standardPlatform)
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly))
                .Bind<IDynamicLibraryLoader>().ToConstant(
#if __IOS__
                    new IOSLoader()
#else
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? (IDynamicLibraryLoader)new Win32Loader()
                    : new UnixLoader()
#endif
                );
        }
    }
    
    
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            return new Timer(_ => tick(), null, interval, interval);
        }

        public IUnmanagedBlob AllocBlob(int size) => new UnmanagedBlob(this, size);
        
        class UnmanagedBlob : IUnmanagedBlob
        {
            private readonly StandardRuntimePlatform _plat;
            private IntPtr _address;
            private readonly object _lock = new object();
#if DEBUG
            private static readonly List<string> Backtraces = new List<string>();
            private static Thread GCThread;
            private readonly string _backtrace;
            private static readonly object _btlock = new object();

            class GCThreadDetector
            {
                ~GCThreadDetector()
                {
                    GCThread = Thread.CurrentThread;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Spawn() => new GCThreadDetector();
            
            static UnmanagedBlob()
            {
                Spawn();
                GC.WaitForPendingFinalizers();
            }
            
#endif
            
            public UnmanagedBlob(StandardRuntimePlatform plat, int size)
            {
                if (size <= 0)
                    throw new ArgumentException("Positive number required", nameof(size));
                _plat = plat;
                _address = plat.Alloc(size);
                GC.AddMemoryPressure(size);
                Size = size;
#if DEBUG
                _backtrace = Environment.StackTrace;
                lock (_btlock)
                    Backtraces.Add(_backtrace);
#endif
            }

            void DoDispose()
            {
                lock (_lock)
                {
                    if (!IsDisposed)
                    {
#if DEBUG
                        lock (_btlock)
                            Backtraces.Remove(_backtrace);
#endif
                        _plat?.Free(_address, Size);
                        GC.RemoveMemoryPressure(Size);
                        IsDisposed = true;
                        _address = IntPtr.Zero;
                        Size = 0;
                    }
                }
            }

            public void Dispose()
            {
#if DEBUG
                if (Thread.CurrentThread.ManagedThreadId == GCThread?.ManagedThreadId)
                {
                    lock (_lock)
                    {
                        if (!IsDisposed)
                        {
                            Console.Error.WriteLine("Native blob disposal from finalizer thread\nBacktrace: "
                                                 + Environment.StackTrace
                                                 + "\n\nBlob created by " + _backtrace);
                        }
                    }
                }
#endif
                DoDispose();
                GC.SuppressFinalize(this);
            }

            ~UnmanagedBlob()
            {
#if DEBUG
                Console.Error.WriteLine("Undisposed native blob created by " + _backtrace);
#endif
                DoDispose();
            }

            public IntPtr Address => IsDisposed ? throw new ObjectDisposedException("UnmanagedBlob") : _address; 
            public int Size { get; private set; }
            public bool IsDisposed { get; private set; }
        }
        
        
        
#if NET461 || NETCOREAPP2_0
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);
        [DllImport("libc", SetLastError = true)]
        private static extern int munmap(IntPtr addr, IntPtr length);
        [DllImport("libc", SetLastError = true)]
        private static extern long sysconf(int name);

        private bool? _useMmap;
        private bool UseMmap 
            => _useMmap ?? ((_useMmap = GetRuntimeInfo().OperatingSystem == OperatingSystemType.Linux)).Value;
        
        IntPtr Alloc(int size)
        {
            if (UseMmap)
            {
                var rv = mmap(IntPtr.Zero, new IntPtr(size), 3, 0x22, -1, IntPtr.Zero);
                if (rv.ToInt64() == -1 || (ulong) rv.ToInt64() == 0xffffffff)
                {
                    var errno = Marshal.GetLastWin32Error();
                    throw new Exception("Unable to allocate memory: " + errno);
                }
                return rv;
            }
            else
                return Marshal.AllocHGlobal(size);
        }

        void Free(IntPtr ptr, int len)
        {
            if (UseMmap)
            {
                if (munmap(ptr, new IntPtr(len)) == -1)
                {
                    var errno = Marshal.GetLastWin32Error();
                    throw new Exception("Unable to free memory: " + errno);
                }
            }
            else
                Marshal.FreeHGlobal(ptr);
        }
#else
        IntPtr Alloc(int size) => Marshal.AllocHGlobal(size);
        void Free(IntPtr ptr, int len) => Marshal.FreeHGlobal(ptr);
#endif
        

    }
    
    internal class IOSLoader : IDynamicLibraryLoader
    {
        IntPtr IDynamicLibraryLoader.LoadLibrary(string dll)
        {
            throw new PlatformNotSupportedException();
        }

        IntPtr IDynamicLibraryLoader.GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            throw new PlatformNotSupportedException();
        }
    }
    
    public class AssetLoader : IAssetLoader
    {
        private static readonly IAssemblyDescriptorResolver _assemblyDescriptorResolver
            = new AssemblyDescriptorResolver();

        private AssemblyDescriptor _defaultResmAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetLoader"/> class.
        /// </summary>
        /// <param name="assembly">
        /// The default assembly from which to load resm: assets for which no assembly is specified.
        /// </param>
        public AssetLoader(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
                _defaultResmAssembly = new AssemblyDescriptor(assembly);
        }

        /// <summary>
        /// Sets the default assembly from which to load assets for which no assembly is specified.
        /// </summary>
        /// <param name="assembly">The default assembly.</param>
        public void SetDefaultAssembly(Assembly assembly)
        {
            _defaultResmAssembly = new AssemblyDescriptor(assembly);
        }

        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        public bool Exists(Uri uri, Uri baseUri = null)
        {
            return GetAsset(uri, baseUri) != null;
        }

        /// <summary>
        /// Opens the asset with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>A stream containing the asset contents.</returns>
        /// <exception cref="FileNotFoundException">
        /// The asset could not be found.
        /// </exception>
        public Stream Open(Uri uri, Uri baseUri = null) => OpenAndGetAssembly(uri, baseUri).Item1;

        /// <summary>
        /// Opens the asset with the requested URI and returns the asset stream and the
        /// assembly containing the asset.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>
        /// The stream containing the resource contents together with the assembly.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// The asset could not be found.
        /// </exception>
        public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri baseUri = null)
        {
            var asset = GetAsset(uri, baseUri);

            if (asset == null)
            {
                throw new FileNotFoundException($"The resource {uri} could not be found.");
            }

            return (asset.GetStream(), asset.Assembly);
        }

        public Assembly GetAssembly(Uri uri, Uri baseUri)
        {
            if (!uri.IsAbsoluteUri && baseUri != null)
                uri = new Uri(baseUri, uri);
            return GetAssembly(uri).Assembly;
        }

        /// <summary>
        /// Gets all assets of a folder and subfolders that match specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">Base URI that is used if <paramref name="uri"/> is relative.</param>
        /// <returns>All matching assets as a tuple of the absolute path to the asset and the assembly containing the asset</returns>
        public IEnumerable<Uri> GetAssets(Uri uri, Uri baseUri)
        {
            if (uri.IsAbsoluteResm())
            {
                var assembly = GetAssembly(uri);

                return assembly?.Resources
                           .Where(x => x.Key.IndexOf(uri.GetUnescapeAbsolutePath(), StringComparison.Ordinal) >= 0)
                           .Select(x => new Uri($"resm:{x.Key}?assembly={assembly.Name}")) ??
                       Enumerable.Empty<Uri>();
            }

            uri = uri.EnsureAbsolute(baseUri);
            if (uri.IsAvares())
            {
                var (asm, path) = GetResAsmAndPath(uri);
                if (asm == null)
                {
                    throw new ArgumentException(
                        "No default assembly, entry assembly or explicit assembly specified; " +
                        "don't know where to look up for the resource, try specifying assembly explicitly.");
                }

                if (asm.AvaloniaResources == null)
                    return Enumerable.Empty<Uri>();

                if (path[path.Length - 1] != '/')
                    path += '/';

                return asm.AvaloniaResources
                    .Where(r => r.Key.StartsWith(path, StringComparison.Ordinal))
                    .Select(x => new Uri($"avares://{asm.Name}{x.Key}"));
            }

            return Enumerable.Empty<Uri>();
        }

        private IAssetDescriptor GetAsset(Uri uri, Uri baseUri)
        {           
            if (uri.IsAbsoluteResm())
            {
                var asm = GetAssembly(uri) ?? GetAssembly(baseUri) ?? _defaultResmAssembly;

                if (asm == null)
                {
                    throw new ArgumentException(
                        "No default assembly, entry assembly or explicit assembly specified; " +
                        "don't know where to look up for the resource, try specifying assembly explicitly.");
                }

                var resourceKey = uri.GetUnescapeAbsolutePath();
                asm.Resources.TryGetValue(resourceKey, out var rv);
                return rv;
            }

            uri = uri.EnsureAbsolute(baseUri);

            if (uri.IsAvares())
            {
                var (asm, path) = GetResAsmAndPath(uri);
                if (asm.AvaloniaResources == null)
                    return null;
                asm.AvaloniaResources.TryGetValue(path, out var desc);
                return desc;
            }

            throw new ArgumentException($"Unsupported url type: " + uri.Scheme, nameof(uri));
        }

        private (IAssemblyDescriptor asm, string path) GetResAsmAndPath(Uri uri)
        {
            var asm = GetAssembly(uri.Authority);
            return (asm, uri.GetUnescapeAbsolutePath());
        }
        
        private IAssemblyDescriptor GetAssembly(Uri uri)
        {
            if (uri != null)
            {
                if (!uri.IsAbsoluteUri)
                    return null;
                if (uri.IsAvares())
                    return GetResAsmAndPath(uri).asm;

                if (uri.IsResm())
                {
                    var assemblyName = uri.GetAssemblyNameFromQuery();
                    if (assemblyName.Length > 0)
                        return GetAssembly(assemblyName);
                }
            }

            return null;
        }

        private IAssemblyDescriptor GetAssembly(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return _assemblyDescriptorResolver.Get(name);
        }

        private class AssemblyDescriptorResolver : IAssemblyDescriptorResolver
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

        public static void RegisterResUriParsers() => UriUtilities.RegisterResUriParsers();
    }
}
