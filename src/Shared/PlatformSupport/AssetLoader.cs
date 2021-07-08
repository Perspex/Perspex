using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;
using Avalonia.Platform.Internal;
using Avalonia.Shared.PlatformSupport.Internal;
using Avalonia.Utilities;

namespace Avalonia.Shared.PlatformSupport
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public class AssetLoader : IAssetLoader
    {
        private static IAssemblyDescriptorResolver _assemblyDescriptorResolver
            = new AssemblyDescriptorResolver();

        private AssemblyDescriptor _defaultResmAssembly;

        /// <remarks>
        /// Introduced for tests.
        /// </remarks>
        internal static void SetAssemblyDescriptorResolver(IAssemblyDescriptorResolver resolver) =>
            _assemblyDescriptorResolver = resolver;

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

        public static void RegisterResUriParsers() => UriUtilities.RegisterResUriParsers();
    }
}
