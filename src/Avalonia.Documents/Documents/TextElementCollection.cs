// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Generic type for TextElement collections
//

using Avalonia;
using Avalonia.Controls;
using Avalonia.Documents;
using Avalonia.Documents.Internal;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using System.Collections; // IList
    using System.Collections.Generic; // ICollection<T>
    /*    using System.Windows.Controls;*/ // TextBlock, ContentControl and AccessText
    using MS.Internal; // Invariant
    /*    using MS.Internal.Documents;*/ // FlowDocumentView

    /// <summary>
    /// </summary>
    public class TextElementCollection<TextElementType> : IList, ICollection<TextElementType> where TextElementType : TextElement
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// </summary>
        /// <param name="owner">
        /// Owner of this TextElementCollection. 
        /// In case when a collection is created for children of a parent, owner is the parent object.
        /// In case when a collection is created as a sibling collection of a TextElement, owner is that TextElement object. 
        /// This distinction is very important, so that a collection belongs semantically to the owner it was created for.
        /// </param>
        /// <param name="isOwnerParent">
        /// Flag indicating if the owner is a parent of objects in this collection or is a member object.
        /// </param>
        internal TextElementCollection(IAvaloniaObject owner, bool isOwnerParent)
        {
            if (isOwnerParent)
            {
                Invariant.Assert(owner is TextElement/* || owner is FlowDocument */ || owner is NewTextBlock);
            }
            else
            {
                Invariant.Assert(owner is TextElement);
            }

            _owner = owner;
            _isOwnerParent = isOwnerParent;

            _indexCache = new ElementIndexCache(-1, null);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        //...................................................................
        //
        //  ICollection<TextElementType> Members
        //
        //...................................................................

        #region ICollection<TextElementType> Members

        /// <summary>
        /// Adds an unlinked TextElement item to the end of this collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TextElementType item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            ValidateChild(item);

            this.TextContainer.BeginChange();
            try
            {
                item.RepositionWithContent(this.ContentEnd);
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            // Note: We need to remember the textcontainer for any delete operations in this collection.
            // This is important for the case when the owner of the collection is a sibling member.
            // In a scenario where you remove the owner itself from the collection, 
            // the owner belongs to another tree after the reposition.

            TextContainer textContainer = this.TextContainer;
            textContainer.BeginChange();
            try
            {
                textContainer.DeleteContentInternal(this.ContentStart, this.ContentEnd);
            }
            finally
            {
                textContainer.EndChange();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TextElementType item)
        {
            if (item == null)
            {
                return false;
            }

            TextElementType element;

            for (element = this.FirstChild; element != null; element = (TextElementType)element.NextElement)
            {
                if (element == item)
                    break;
            }

            return (element == item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(TextElementType[] array, int arrayIndex)
        {
            ((ICollection)this).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;

                TextElement element;

                if (_indexCache.IsValid(this))
                {
                    element = _indexCache.Element;
                    count += _indexCache.Index;
                }
                else
                {
                    element = this.FirstChild;
                }

                while (element != null)
                {
                    count++;
                    element = element.NextElement;
                }

                return count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes an item from a collection and from a tree.
        /// The element removed can be re-inserted later in another collection of a tree.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// True if removal was successful.
        /// </returns>
        public bool Remove(TextElementType item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.Parent != this.Parent)
            {
                return false;
            }

            // Note: We need to remember the textcontainer for any delete operations in this collection.
            // This is important for the case when the owner of the collection is a sibling member.
            // In a scenario where you remove the owner itself from the collection, 
            // the owner belongs to another tree after the reposition.

            TextContainer textContainer = this.TextContainer;
            textContainer.BeginChange();
            try
            {
                item.RepositionWithContent(null);
            }
            finally
            {
                textContainer.EndChange();
            }

            return true;
        }

        #endregion ICollection<TextElementType> Members


        /// <summary>
        /// Inserts a unlinked TextElement newItem after previousSibling, which is supposed to be an existing member of this collection.
        /// </summary>
        /// <param name="previousSibling">
        /// TextElement after which the newItem is to be inserted
        /// </param>
        /// <param name="newItem">
        /// A TextElement to be inserted into the collection after the previousSibling.
        /// It must be unlinked from a tree before insertion.
        /// </param>
        public void InsertAfter(TextElementType previousSibling, TextElementType newItem)
        {
            if (previousSibling == null)
            {
                throw new ArgumentNullException("previousSibling");
            }

            if (newItem == null)
            {
                throw new ArgumentNullException("newItem");
            }

            if (previousSibling.Parent != this.Parent)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextElementCollection_PreviousSiblingDoesNotBelongToThisCollection, previousSibling.GetType().Name)*/);
            }

            if (newItem.Parent != null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextSchema_TheChildElementBelongsToAnotherTreeAlready, this.GetType().Name)*/);
            }

            ValidateChild(newItem);

            this.TextContainer.BeginChange();
            try
            {
                newItem.RepositionWithContent(previousSibling.ElementEnd);
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        /// <summary>
        /// Inserts a TextElement newItem into a collection before a nextSibling TextElement.
        /// </summary>
        /// <param name="nextSibling">
        /// TextElement before which the newItem is to be inserted
        /// </param>
        /// <param name="newItem">
        /// A TextElement to be inserted into the collection before the nextSibling.
        /// It must be unlinked from a tree before insertion.
        /// </param>
        public void InsertBefore(TextElementType nextSibling, TextElementType newItem)
        {
            if (nextSibling == null)
            {
                throw new ArgumentNullException("nextSibling");
            }

            if (newItem == null)
            {
                throw new ArgumentNullException("newItem");
            }

            if (nextSibling.Parent != this.Parent)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextElementCollection_NextSiblingDoesNotBelongToThisCollection, nextSibling.GetType().Name)*/);
            }

            if (newItem.Parent != null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextSchema_TheChildElementBelongsToAnotherTreeAlready, this.GetType().Name)*/);
            }

            ValidateChild(newItem);

            this.TextContainer.BeginChange();
            try
            {
                newItem.RepositionWithContent(nextSibling.ElementStart);
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        /// <summary>
        /// Adds the elements of an IEnumerable to this collection.
        /// </summary>
        /// <param name="range">
        /// Elements to add.
        /// </param>
        public void AddRange(IEnumerable range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            IEnumerator enumerator = range.GetEnumerator();
            if (enumerator == null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_NoEnumerator), "range"*/);
            }

            this.TextContainer.BeginChange();
            try
            {
                while (enumerator.MoveNext())
                {
                    TextElementType element = enumerator.Current as TextElementType;

                    if (element == null)
                    {
                        // REVIEW: It would be better design if we reviewed all elements in the range before starting an insert.
                        // Otherwise, we might insert half the elements and then throw.
                        throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_ItemHasUnexpectedType, "range", typeof(TextElementType).Name, typeof(TextElementType).Name), "value"*/);
                    }

                    Add(element);
                }
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        //...................................................................
        //
        //  IEnumerable<TextElementType> Members
        //
        //...................................................................

        #region IEnumerable<TextElementType> Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TextElementType> GetEnumerator()
        {
            return new TextElementEnumerator<TextElementType>(this.ContentStart, this.ContentEnd);
        }

        #endregion IEnumerable<TextElementType> Members

        //...................................................................
        //
        //  IEnumerable Members
        //
        //...................................................................

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new RangeContentEnumerator(this.ContentStart, this.ContentEnd);
        }

        #endregion IEnumerable Members

        //...................................................................
        //
        //  IList Members
        //
        //...................................................................

        #region IList Members

        /// <summary>
        /// Method that does the work for IList.Add.
        /// </summary>
        internal virtual int OnAdd(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (!(value is TextElementType))
            {
                throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_TextElementTypeExpected, typeof(TextElementType).Name), "value"*/);
            }

            ValidateChild((TextElementType)value);

            this.TextContainer.BeginChange();
            try
            {
                bool isCacheSafePreviousIndex = _indexCache.IsValid(this);

                this.Add((TextElementType)value);

                return IndexOfInternal(value, isCacheSafePreviousIndex);
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        int IList.Add(object value)
        {
            return OnAdd(value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            TextElementType item = value as TextElementType;

            if (item == null)
            {
                return false;
            }

            return this.Contains(item);
        }

        int IList.IndexOf(object value)
        {
            return IndexOfInternal(value, false /* isCacheSafePreviousIndex */);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            TextElementType newItem = value as TextElementType;

            if (newItem == null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_TextElementTypeExpected, typeof(TextElementType).Name), "value"*/);
            }

            if (index < 0)
            {
                throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
            }

            if (newItem.Parent != null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextSchema_TheChildElementBelongsToAnotherTreeAlready, this.GetType().Name)*/);
            }

            ValidateChild(newItem);

            this.TextContainer.BeginChange();
            try
            {
                TextPointer position;

                if (this.FirstChild == null)
                {
                    if (index != 0)
                    {
                        throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
                    }
                    position = this.ContentStart;
                }
                else
                {
                    bool atCollectionEnd;
                    TextElementType element = GetElementAtIndex(index, out atCollectionEnd);

                    if (!atCollectionEnd && element == null)
                    {
                        throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
                    }

                    position = atCollectionEnd ? this.ContentEnd : element.ElementStart;
                }

                position.InsertTextElement(newItem);

                SetCache(index, newItem);
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return this.IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            TextElementType item = value as TextElementType;

            if (item == null)
            {
                return;
            }

            this.Remove(item);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAtInternal(index);
        }

        object IList.this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
                }

                TextElementType element = GetElementAtIndex(index);

                if (element == null)
                {
                    throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
                }

                SetCache(index, element);

                return element;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (!(value is TextElementType))
                {
                    throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_TextElementTypeExpected, typeof(TextElementType).Name), "value"*/);
                }

                ValidateChild((TextElementType)value);

                this.TextContainer.BeginChange();
                try
                {
                    // Remove old element.
                    TextElementType nextElement = RemoveAtInternal(index);

                    // Insert new element.
                    TextPointer position = (nextElement == null) ? this.ContentEnd : nextElement.ElementStart;
                    position.InsertTextElement((TextElementType)value);

                    // Reset the cache.
                    SetCache(index, (TextElementType)value);
                }
                finally
                {
                    this.TextContainer.EndChange();
                }
            }
        }

        #endregion IList Members

        #region ICollection Members

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            int count = this.Count;

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            Type elementType = array.GetType().GetElementType();
            if (elementType == null || !elementType.IsAssignableFrom(typeof(TextElementType)))
            {
                throw new ArgumentException("array");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (arrayIndex > array.Length)
            {
                throw new ArgumentException("arrayIndex");
            }

            if (array.Length < arrayIndex + count)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextElementCollection_CannotCopyToArrayNotSufficientMemory, count, arrayIndex, array.Length)*/);
            }

            for (TextElementType element = (TextElementType)this.FirstChild; element != null; element = (TextElementType)element.NextElement)
            {
                array.SetValue(element, arrayIndex++);
            }
        }

        int ICollection.Count
        {
            get
            {
                return this.Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                //  Provide correct implementation for this member
                return true;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                //  Provide correct implementation for this member
                return this.TextContainer;
            }
        }

        #endregion ICollection Members

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        // Owner of this TextElementCollection. 
        // In case when a collection is created for children of a parent, owner is the parent object.
        // In case when a collection is created as a sibling collection of a TextElement, owner is that TextElement object. 
        // This distinction is very important, so that a collection belongs semantically to the owner it was created for.
        internal IAvaloniaObject Owner
        {
            get
            {
                return _owner;
            }
        }

        // Parent of all TextElementType objects in this collection. 
        // Note that a parent may or may not be the owner of the collection.
        internal IAvaloniaObject Parent
        {
            get
            {
                return _isOwnerParent ? _owner : ((TextElement)_owner).Parent;
            }
        }

        // The TextContainer associated with this collection's parent.
        internal TextContainer TextContainer
        {
            get
            {
                TextContainer textContainer;

                if (_owner is NewTextBlock)
                {
                    textContainer = (TextContainer) ((NewTextBlock)_owner).TextContainer;
                }
                //else if (_owner is FlowDocument)
                //{
                //    textContainer = ((FlowDocument)_owner).TextContainer;
                //}
                else
                {
                    textContainer = ((TextElement)_owner).TextContainer;
                }

                return textContainer;
            }
        }

        /// <value>
        /// Returns a first item of this collection
        /// </value>
        internal TextElementType FirstChild
        {
            get
            {
                TextElementType firstChild;

                if (this.Parent is TextElement)
                {
                    firstChild = (TextElementType)((TextElement)this.Parent).FirstChildElement;
                }
                else
                {
                    TextTreeTextElementNode node = this.TextContainer.FirstContainedNode as TextTreeTextElementNode;
                    firstChild = (TextElementType)(node == null ? null : node.TextElement);
                }

                return firstChild;
            }
        }

        /// <value>
        /// Returns a first item of this collection
        /// </value>
        internal TextElementType LastChild
        {
            get
            {
                TextElementType lastChild;

                if (this.Parent is TextElement)
                {
                    lastChild = (TextElementType)((TextElement)this.Parent).LastChildElement;
                }
                else
                {
                    TextTreeTextElementNode node = this.TextContainer.LastContainedNode as TextTreeTextElementNode;
                    lastChild = (TextElementType)(node == null ? null : node.TextElement);
                }

                return lastChild;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Worker for IList.RemoveAt and IList.set_Item.
        // Returns the element following the element removed.
        private TextElementType RemoveAtInternal(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
            }

            TextElementType element = GetElementAtIndex(index);

            if (element == null)
            {
                throw new IndexOutOfRangeException(/*SR.Get(SRID.TextElementCollection_IndexOutOfRange)*/);
            }

            TextElementType nextElement = (TextElementType)element.NextElement;

            // Note: We need to remember the textcontainer for any delete operations in this collection.
            // This is important for the case when the owner of the collection is a sibling member.
            // In a scenario where you remove the owner itself from the collection, 
            // the owner belongs to another tree after the reposition.

            TextContainer textContainer = this.TextContainer;
            textContainer.BeginChange();
            try
            {
                // Prepare to reset the cache.
                // We're about to remove the current cache, so we need to find a neighbor.
                TextElementType newElementCache = nextElement;
                if (newElementCache == null)
                {
                    newElementCache = (TextElementType)element.PreviousElement;
                    index--;
                }

                // Remove the element.
                element.RepositionWithContent(null);

                // Reset the cache.
                if (newElementCache != null)
                {
                    SetCache(index, newElementCache);
                }
            }
            finally
            {
                textContainer.EndChange();
            }

            return nextElement;
        }

        // Returns an element at a given index, or null if the index is out of range.
        private TextElementType GetElementAtIndex(int index)
        {
            bool atCollectionEnd;
            return GetElementAtIndex(index, out atCollectionEnd);
        }

        // Returns an element at a given index, or null if the index is out of range.
        // If the index exactly equals the collection count (and hence the return value
        // is null), atCollectionEnd is set true.
        private TextElementType GetElementAtIndex(int index, out bool atCollectionEnd)
        {
            TextElementType element;
            bool forward = true;

            if (_indexCache.IsValid(this))
            {
                if (_indexCache.Index == index)
                {
                    element = _indexCache.Element;
                    index = 0;
                }
                else if (_indexCache.Index < index)
                {
                    element = _indexCache.Element;
                    index = index - _indexCache.Index;
                }
                else // _indexCache.Index > index
                {
                    element = _indexCache.Element;
                    index = _indexCache.Index - index;
                    forward = false;
                }
            }
            else
            {
                element = this.FirstChild;
            }

            while (index > 0 && element != null)
            {
                element = (TextElementType)(forward ? element.NextElement : element.PreviousElement);
                index--;
            }

            atCollectionEnd = (index == 0 && element == null);
            return element;
        }

        // Sets the element/index cache.
        private void SetCache(int index, TextElementType item)
        {
            _indexCache = new ElementIndexCache(index, item);
            TextElementCollectionHelper.MarkClean(this.Parent, this);
        }

        // Returns the index of an object in the collection, or -1 if the
        // object is not a member.
        //
        // If isCacheSafePreviousIndex is true, the cache is guaranteed to be
        // a valid index/element pair of a preceding element, regardless of
        // whether or not _idexCache.IsValid is true.
        private int IndexOfInternal(object value, bool isCacheSafePreviousIndex)
        {
            TextElementType item = value as TextElementType;

            if (value == null)
            {
                return -1;
            }

            // Early out on a cache hit.
            if (_indexCache.IsValid(this))
            {
                if ((object)item == (object)_indexCache.Element)
                {
                    return _indexCache.Index;
                }
            }

            int index;
            TextElementType element;

            if (isCacheSafePreviousIndex)
            {
                index = _indexCache.Index;
                element = _indexCache.Element;
            }
            else
            {
                index = 0;
                element = this.FirstChild;
            }

            while (element != null)
            {
                if (element == item)
                {
                    SetCache(index, item);
                    return index;
                }

                element = (TextElementType)element.NextElement;
                index++;
            }

            return -1;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method validates a child of this collection.
        /// Default implementation does nothing as TextElementType is a valid child type.
        /// InlineCollection overrides this method to do additional schema validation for its children.
        /// </summary>
        internal virtual void ValidateChild(TextElementType child)
        {
            return;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <value>
        /// </value>
        private TextPointer ContentStart
        {
            get
            {
                return
                    this.Parent is TextElement ? ((TextElement)this.Parent).ContentStart : this.TextContainer.Start;
            }
        }

        /// <value>
        /// </value>
        private TextPointer ContentEnd
        {
            get
            {
                return
                    this.Parent is TextElement ? ((TextElement)this.Parent).ContentEnd : this.TextContainer.End;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        // A cached TextElementType/index pair.
        // Used to speed up IList calls with locality.
        private struct ElementIndexCache
        {
            internal ElementIndexCache(int index, TextElementType element)
            {
                // index == -1/element == null means "empty".
                Invariant.Assert(index == -1 || element != null);

                _index = index;
                _element = element;
            }

            // True if the cache is reliable.  Otherwise, the tree has been
            // modified and it is not possible to guarantee a valid cache.
            internal bool IsValid(TextElementCollection<TextElementType> collection)
            {
                return _index >= 0 && TextElementCollectionHelper.IsCleanParent(_element.Parent, collection);
            }

            // Cached element index in this collection.
            internal int Index { get { return _index; } }

            // Cached element in this collection.
            internal TextElementType Element { get { return _element; } }

            // Cached element index in this collection.
            private readonly int _index;

            // Cached element in this collection.
            private readonly TextElementType _element;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // Object associated with this collection -- its children or siblings are the collection members.
        private IAvaloniaObject _owner;

        // Flag indicating if owner is a parent of objects in this collection or is a sibling object.
        private bool _isOwnerParent;

        // Cached element/index pair used to speed up IList calls.
        private ElementIndexCache _indexCache;

        #endregion Private Fields
    }
}
