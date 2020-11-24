using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    public abstract class DefinitionList<T> : AvaloniaList<T> where T : DefinitionBase
    {
        public DefinitionList()
        {
            ResetBehavior = ResetBehavior.Remove;
            CollectionChanged += OnCollectionChanged;
        }

        private Grid _parent;

        internal Grid Parent
        {
            get => _parent;
            set => SetParent(value);
        }

        private void SetParent(Grid value)
        {
            _parent = value;

            foreach (var pair in this.Select((definitions, index) => (definitions, index)))
            {
                pair.definitions.Parent = value;
                pair.definitions.Index = pair.index;
            }
        }

        internal void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateParentGrid();

            foreach (var nI in this.Select((d, i) => (d, i)))
                nI.d._parentIndex = nI.i;

            foreach (var nD in e.NewItems?.Cast<DefinitionBase>()
                            ?? Enumerable.Empty<DefinitionBase>())
            {
                nD.Parent = this.Parent;
                nD.OnEnterParentTree();
            }

            foreach (var oD in e.OldItems?.Cast<DefinitionBase>()
                            ?? Enumerable.Empty<DefinitionBase>())
            {
                oD.OnExitParentTree();
            }            
        }


        public virtual void UpdateParentGrid()
        {
        }
    }
}