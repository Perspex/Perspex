using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.UnitTests
{
    public class MockAssetLoader : IAssetLoader
    {
        private Dictionary<Uri, string> _assets;

        public MockAssetLoader(params (string, string)[] assets)
        {
            _assets = assets.ToDictionary(x => new Uri(x.Item1, UriKind.RelativeOrAbsolute), x => x.Item2);
        }

        public bool Exists(Uri uri, Uri baseUri = null)
        {
            return _assets.ContainsKey(uri);
        }

        public Stream Open(Uri uri, Uri baseUri = null)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_assets[uri]));
        }

        public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri baseUri = null)
        {
            return (Open(uri, baseUri), (Assembly)null);
        }

        public Assembly GetAssembly(Uri uri, Uri baseUri = null)
        {
            return null;
        }

        public IEnumerable<Uri> GetAssets(Uri uri, Uri baseUri)
        {
            return _assets.Keys.Where(
                x => x.GetUnescapeAbsolutePath().IndexOf(uri.GetUnescapeAbsolutePath(), StringComparison.Ordinal) >= 0);
        }

        public void SetDefaultAssembly(Assembly asm)
        {
            throw new NotImplementedException();
        }
    }
}
