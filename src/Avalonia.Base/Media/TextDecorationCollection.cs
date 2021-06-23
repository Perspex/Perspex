using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// A collection that holds <see cref="TextDecoration"/> objects.
    /// </summary>
    public class TextDecorationCollection : AvaloniaList<TextDecoration>
    {
        /// <summary>
        /// Parses a <see cref="TextDecorationCollection"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="TextDecorationCollection"/>.</returns>
        public static TextDecorationCollection Parse(string s)
        {
            var locations = new List<TextDecorationLocation>();

            using (var tokenizer = new StringTokenizer(s, ',', "Invalid text decoration."))
            {
                while (tokenizer.TryReadString(out var name))
                {
                    var location = GetTextDecorationLocation(name);

                    if (locations.Contains(location))
                    {
                        throw new ArgumentException("Text decoration already specified.", nameof(s));
                    }

                    locations.Add(location);
                }
            }

            var textDecorations = new TextDecorationCollection();

            foreach (var textDecorationLocation in locations)
            {
                textDecorations.Add(new TextDecoration { Location = textDecorationLocation });
            }

            return textDecorations;
        }

        /// <summary>
        /// Parses a <see cref="TextDecorationLocation"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="TextDecorationLocation"/>.</returns>
        private static TextDecorationLocation GetTextDecorationLocation(string s)
        {
            if (Enum.TryParse<TextDecorationLocation>(s,true, out var location))
            {
                return location;
            }

            throw new ArgumentException("Could not parse text decoration.", nameof(s));
        }
    }
}
