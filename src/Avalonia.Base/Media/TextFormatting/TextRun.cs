using System.Diagnostics;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a portion of a <see cref="TextLine"/> object.
    /// </summary>
    [DebuggerTypeProxy(typeof(TextRunDebuggerProxy))]
    public abstract class TextRun
    {
        public const int DefaultTextSourceLength = 1;

        /// <summary>
        ///  Gets the text source length.
        /// </summary>
        public virtual int TextSourceLength => DefaultTextSourceLength;

        /// <summary>
        /// Gets the text run's text.
        /// </summary>
        public virtual ReadOnlySlice<char> Text => default;

        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public virtual TextRunProperties Properties => null;

        private class TextRunDebuggerProxy
        {
            private readonly TextRun _textRun;

            public TextRunDebuggerProxy(TextRun textRun)
            {
                _textRun = textRun;
            }

            public string Text
            {
                get
                {
                    unsafe
                    {
                        fixed (char* charsPtr = _textRun.Text.Buffer.Span)
                        {
                            return new string(charsPtr, 0, _textRun.Text.Length);
                        }
                    }
                }
            }

            public TextRunProperties Properties => _textRun.Properties;
        }
    }
}
