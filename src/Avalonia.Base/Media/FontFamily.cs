using System;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    public sealed class FontFamily
    {
        public const string DefaultFontFamilyName = "$Default";

        static FontFamily()
        {
            Default = new FontFamily(DefaultFontFamilyName);
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
        public FontFamily(string name) : this(null, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
        /// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
        /// <exception cref="T:System.ArgumentException">Base uri must be an absolute uri.</exception>
        public FontFamily(Uri baseUri, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var fontFamilySegment = GetFontFamilyIdentifier(name);

            if (fontFamilySegment.Source != null)
            {
                if (baseUri != null && !baseUri.IsAbsoluteUri)
                {
                    throw new ArgumentException("Base uri must be an absolute uri.", nameof(baseUri));
                }

                Key = new FontFamilyKey(fontFamilySegment.Source, baseUri);
            }

            FamilyNames = new FamilyNameCollection(fontFamilySegment.Name);
        }

        /// <summary>
        /// Represents the default font family
        /// </summary>
        public static FontFamily Default { get; }

        /// <summary>
        /// Gets the primary family name of the font family.
        /// </summary>
        /// <value>
        /// The primary name of the font family.
        /// </value>
        public string Name => FamilyNames.PrimaryFamilyName;

        /// <summary>
        /// Gets the family names.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        public FamilyNameCollection FamilyNames { get; }

        /// <summary>
        /// Gets the key for associated assets.
        /// </summary>
        /// <value>
        /// The family key.
        /// </value>
        /// <remarks>Key is only used for custom fonts.</remarks>
        public FontFamilyKey Key { get; }

        /// <summary>
        /// Returns <c>True</c> if this instance is the system's default.
        /// </summary>
        public bool IsDefault => Name.Equals(DefaultFontFamilyName);

        /// <summary>
        /// Implicit conversion of string to FontFamily
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator FontFamily(string s)
        {
            return new FontFamily(s);
        }

        private struct FontFamilyIdentifier
        {
            public FontFamilyIdentifier(string name, Uri source)
            {
                Name = name;
                Source = source;
            }

            public string Name { get; }

            public Uri Source { get; }
        }

        private static FontFamilyIdentifier GetFontFamilyIdentifier(string name)
        {
            var segments = name.Split('#');

            switch (segments.Length)
            {
                case 1:
                    {
                        return new FontFamilyIdentifier(segments[0], null);
                    }

                case 2:
                    {
                        var source = segments[0].StartsWith("/")
                            ? new Uri(segments[0], UriKind.Relative)
                            : new Uri(segments[0], UriKind.RelativeOrAbsolute);

                        return new FontFamilyIdentifier(segments[1], source);
                    }

                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
        }

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s) => Parse(s, null);

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s, Uri baseUri)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Specified family is not supported.", nameof(s));
            }

            return new FontFamily(baseUri, s);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Key != null)
            {
                return Key + "#" + FamilyNames;
            }

            return FamilyNames.ToString();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((FamilyNames != null ? FamilyNames.GetHashCode() : 0) * 397) ^ (Key != null ? Key.GetHashCode() : 0);
            }
        }

        public static bool operator !=(FontFamily a, FontFamily b)
        {
            return !(a == b);
        }

        public static bool operator ==(FontFamily a, FontFamily b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return !(a is null) && a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is FontFamily other))
            {
                return false;
            }

            if (!Equals(Key, other.Key))
            {
                return false;
            }

            return other.FamilyNames.Equals(FamilyNames);
        }
    }
}
