using System.Collections.Generic;

namespace Avalonia.Input
{
    /// <summary>
    /// Interface to access information about the data of a drag-and-drop operation.
    /// </summary>
    public interface IDataObject
    {
        /// <summary>
        /// Lists all formats which are present in the DataObject.
        /// <seealso cref="DataFormats"/>
        /// </summary>
        IEnumerable<string> GetDataFormats();

        /// <summary>
        /// Checks whether a given DataFormat is present in this object
        /// <seealso cref="DataFormats"/>
        /// </summary>
        bool Contains(string dataFormat);

        /// <summary>
        /// Returns the dragged text if the DataObject contains any text.
        /// <seealso cref="DataFormats.Text"/>
        /// </summary>
        string? GetText();

        /// <summary>
        /// Returns a list of filenames if the DataObject contains filenames.
        /// <seealso cref="DataFormats.FileNames"/>
        /// </summary>
        IEnumerable<string>? GetFileNames();
        
        /// <summary>
        /// Tries to get the data of the given DataFormat.
        /// </summary>
        object? Get(string dataFormat);
    }
}
