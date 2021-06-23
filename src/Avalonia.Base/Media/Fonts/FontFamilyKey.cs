using System;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents an identifier for a <see cref="FontFamily"/>
    /// </summary>
    public class FontFamilyKey
    {
        /// <summary>
        /// Creates a new instance of <see cref="FontFamilyKey"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="baseUri"></param>
        public FontFamilyKey(Uri source, Uri baseUri = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));

            BaseUri = baseUri;
        }

        /// <summary>
        /// Source of stored font asset that belongs to a <see cref="FontFamily"/>
        /// </summary>
        public Uri Source { get; }

        /// <summary>
        /// A base URI to use if <see cref="Source"/> is relative
        /// </summary>
        public Uri BaseUri { get; }

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
                var hash = (int)2166136261;

                if (Source != null)
                {
                    hash = (hash * 16777619) ^ Source.GetHashCode();
                }

                if (BaseUri != null)
                {
                    hash = (hash * 16777619) ^ BaseUri.GetHashCode();
                }

                return hash;
            }
        }

        public static bool operator !=(FontFamilyKey a, FontFamilyKey b)
        {
            return !(a == b);
        }

        public static bool operator ==(FontFamilyKey a, FontFamilyKey b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return !(a is null) && a.Equals(b);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FontFamilyKey other))
            {
                return false;
            }

            if (Source != other.Source)
            {
                return false;
            }

            if (BaseUri != other.BaseUri)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (!Source.IsAbsoluteUri && BaseUri != null)
            {
                return BaseUri.AbsoluteUri + Source.OriginalString;
            }

            return Source.ToString();
        }
    }
}
