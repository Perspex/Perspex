using System.Collections.Generic;

namespace Avalonia.Input
{
    public class DataObject : IDataObject
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public bool Contains(string dataFormat)
        {
            return _items.ContainsKey(dataFormat);
        }

        public object? Get(string dataFormat)
        {
            if (_items.ContainsKey(dataFormat))
                return _items[dataFormat];
            return null;
        }

        public IEnumerable<string> GetDataFormats()
        {
            return _items.Keys;
        }

        public IEnumerable<string>? GetFileNames()
        {
            return Get(DataFormats.FileNames) as IEnumerable<string>;
        }

        public string? GetText()
        {
            return Get(DataFormats.Text) as string;
        }

        public void Set(string dataFormat, object value)
        {
            _items[dataFormat] = value;
        }
    }
}
