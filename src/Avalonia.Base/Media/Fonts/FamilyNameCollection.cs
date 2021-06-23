using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public sealed class FamilyNameCollection : IReadOnlyList<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyNameCollection"/> class.
        /// </summary>
        /// <param name="familyNames">The family names.</param>
        /// <exception cref="ArgumentException">familyNames</exception>
        public FamilyNameCollection(string familyNames)
        {
            if (familyNames == null)
            {
                throw new ArgumentNullException(nameof(familyNames));
            }

            Names = Array.ConvertAll(familyNames.Split(','), p => p.Trim());

            PrimaryFamilyName = Names[0];

            HasFallbacks = Names.Count > 1;
        }

        /// <summary>
        /// Gets the primary family name.
        /// </summary>
        /// <value>
        /// The primary family name.
        /// </value>
        public string PrimaryFamilyName { get; }

        /// <summary>
        /// Gets a value indicating whether fallbacks are defined.
        /// </summary>
        /// <value>
        ///   <c>true</c> if fallbacks are defined; otherwise, <c>false</c>.
        /// </value>
        public bool HasFallbacks { get; }

        /// <summary>
        /// Gets the internal collection of names.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        internal IReadOnlyList<string> Names { get; }

        /// <summary>
        /// Returns an enumerator for the name collection.
        /// </summary>
        public ImmutableReadOnlyListStructEnumerator<string> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<string>(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            for (var index = 0; index < Names.Count; index++)
            {
                builder.Append(Names[index]);

                if (index == Names.Count - 1)
                {
                    break;
                }

                builder.Append(", ");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            if (Count == 0)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;

                for (var i = 0; i < Names.Count; i++)
                {
                    string name = Names[i];

                    hash = hash * 23 + name.GetHashCode();
                }

                return hash;
            }
        }

        public static bool operator !=(FamilyNameCollection a, FamilyNameCollection b)
        {
            return !(a == b);
        }

        public static bool operator ==(FamilyNameCollection a, FamilyNameCollection b)
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
            if (!(obj is FamilyNameCollection other))
            {
                return false;
            }

            if (other.Count != Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (Names[i] != other.Names[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int Count => Names.Count;

        public string this[int index] => Names[index];
    }
}
