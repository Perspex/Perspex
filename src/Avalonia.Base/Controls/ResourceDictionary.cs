using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : AvaloniaDictionary<object, object?>, IResourceDictionary
    {
        private IResourceHost? _owner;
        private AvaloniaList<IResourceProvider>? _mergedDictionaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary()
        {
            CollectionChanged += OnCollectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary(IResourceHost owner)
            : this()
        {
            Owner = owner;
        }

        public IResourceHost? Owner
        {
            get => _owner;
            private set
            {
                if (_owner != value)
                {
                    _owner = value;
                    OwnerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public IList<IResourceProvider> MergedDictionaries
        {
            get
            {
                if (_mergedDictionaries == null)
                {
                    _mergedDictionaries = new AvaloniaList<IResourceProvider>();
                    _mergedDictionaries.ResetBehavior = ResetBehavior.Remove;
                    _mergedDictionaries.ForEachItem(
                        x =>
                        {
                            if (Owner is object)
                            {
                                x.AddOwner(Owner);
                            }
                        },
                        x =>
                        {
                            if (Owner is object)
                            {
                                x.RemoveOwner(Owner);
                            }
                        }, null);
                }

                return _mergedDictionaries;
            }
        }

        bool IResourceNode.HasResources
        {
            get
            {
                if (Count > 0)
                {
                    return true;
                }

                if (_mergedDictionaries?.Count > 0)
                {
                    foreach (var i in _mergedDictionaries)
                    {
                        if (i.HasResources)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public event EventHandler? OwnerChanged;

        public bool TryGetResource(object key, out object? value)
        {
            if (TryGetValue(key, out value))
            {
                return true;
            }

            if (_mergedDictionaries != null)
            {
                for (var i = _mergedDictionaries.Count - 1; i >= 0; --i)
                {
                    if (_mergedDictionaries[i].TryGetResource(key, out value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The ResourceDictionary already has a parent.");
            }
            
            Owner = owner;

            var hasResources = Count > 0;
            
            if (_mergedDictionaries is object)
            {
                foreach (var i in _mergedDictionaries)
                {
                    i.AddOwner(owner);
                    hasResources |= i.HasResources;
                }
            }

            if (hasResources)
            {
                owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
            }
        }

        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;

                var hasResources = Count > 0;

                if (_mergedDictionaries is object)
                {
                    foreach (var i in _mergedDictionaries)
                    {
                        i.RemoveOwner(owner);
                        hasResources |= i.HasResources;
                    }
                }

                if (hasResources)
                {
                    owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
        }
    }
}
