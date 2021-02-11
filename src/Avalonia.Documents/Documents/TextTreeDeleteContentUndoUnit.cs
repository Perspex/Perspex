// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for TextContainer.DeleteContent calls.
//

using Avalonia;
using MS.Internal;
//using System;
using System.IO;
//using System.Windows.Controls;
//using System.Windows.Markup;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Media.TextFormatting;

//using System.Security;

namespace System.Windows.Documents
{
    // Undo unit for TextContainer.DeleteContent calls.
    internal class TextTreeDeleteContentUndoUnit : TextTreeUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new instance.
        // start/end span the content to copy into the new undo unit -- they
        // should always share the same scoping TextElement.
        internal TextTreeDeleteContentUndoUnit(TextContainer tree, TextPointer start, TextPointer end) : base(tree, start.GetSymbolOffset())
        {
            TextTreeNode node;
            TextTreeNode haltNode;

            start.DebugAssertGeneration();
            end.DebugAssertGeneration();
            Invariant.Assert(start.GetScopingNode() == end.GetScopingNode(), "start/end have different scope!");

            node = start.GetAdjacentNode(LogicalDirection.Forward);
            haltNode = end.GetAdjacentNode(LogicalDirection.Forward);

            // Walk the content, copying runs as we go.
            _content = CopyContent(node, haltNode);
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // Called by the undo manager.  Restores tree state to its condition
        // when the unit was created.  Assumes the tree state matches conditions
        // just after the unit was created.
        //
        // Note that inside the scope of this call we'll also create redo records,
        // which are very fragmented -- one for each TextPointerContext run.
        // In the future, if we have perf issues, we could consider disabling
        // reentrant calls to the UndoManager and instead manually adding a single
        // undo unit here to prevent the fragmentation.
        public override void DoCore()
        {
            TextPointer navigator;
            ContentContainer container;

            VerifyTreeContentHashCode();

            // We need forward gravity to make following inserts work.
            navigator = new TextPointer(this.TextContainer, this.SymbolOffset, LogicalDirection.Forward);

            for (container = _content; container != null; container = container.NextContainer)
            {
                container.Do(navigator);
            }
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Used also in TextTreeExtractElementUndoUnit
        //internal static TableColumn[] SaveColumns(Table table)
        //{
        //    TableColumn[] savedColumns;
        //    if (table.Columns.Count > 0)
        //    {
        //        savedColumns = new TableColumn[table.Columns.Count];
        //        for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
        //        {
        //            savedColumns[columnIndex] = CopyColumn(table.Columns[columnIndex]);
        //        }
        //    }
        //    else
        //    {
        //        savedColumns = null;
        //    }

        //    return savedColumns;
        //}

        // Used also in TextTreeExtractElementUndoUnit
        //internal static void RestoreColumns(Table table, TableColumn[] savedColumns)
        //{
        //    if (savedColumns != null)
        //    {
        //        for (int columnIndex = 0; columnIndex < savedColumns.Length; columnIndex++)
        //        {
        //            if (table.Columns.Count <= columnIndex)
        //            {
        //                table.Columns.Add(CopyColumn(savedColumns[columnIndex]));
        //            }
        //        }
        //    }
        //}

        //private static TableColumn CopyColumn(TableColumn sourceTableColumn)
        //{
        //    TableColumn newTableColumn = new TableColumn();
        //    LocalValueEnumerator properties = sourceTableColumn.GetLocalValueEnumerator();
        //    while (properties.MoveNext())
        //    {
        //        LocalValueEntry propertyEntry = properties.Current;
        //        if (!propertyEntry.Property.ReadOnly)
        //        {
        //            newTableColumn.SetValue(propertyEntry.Property, propertyEntry.Value);
        //        }
        //    }

        //    return newTableColumn;
        //}

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Walks the tree from node to the end of its sibling list,
        // copying content along the way,
        // halting when/if haltNode is encountered. haltNode may be
        // null, in which case we walk to the end of the sibling list.
        //
        // Returns a ContentContainer holding a deep copy of all content
        // walked.
        //
        // This method is called recursively when TextElement nodes
        // are encountered.
        private ContentContainer CopyContent(TextTreeNode node, TextTreeNode haltNode)
        {
            ContentContainer firstContainer;
            ContentContainer container;
            ContentContainer nextContainer;
            TextTreeTextNode textNode;
            //TextTreeObjectNode objectNode;
            TextTreeTextElementNode elementNode;

            firstContainer = null;
            container = null;

            while (node != haltNode && node != null)
            {
                textNode = node as TextTreeTextNode;
                if (textNode != null)
                {
                    node = CopyTextNode(textNode, haltNode, out nextContainer);
                }
                else
                {
                    //objectNode = node as TextTreeObjectNode;
                    //if (objectNode != null)
                    //{
                    //    node = CopyObjectNode(objectNode, out nextContainer);
                    //}
                    //else
                    //{
                        Invariant.Assert(node is TextTreeTextElementNode, "Unexpected TextTreeNode type!");
                        elementNode = (TextTreeTextElementNode)node;

                        node = CopyElementNode(elementNode, out nextContainer);
                    //}
                }

                if (container == null)
                {
                    firstContainer = nextContainer;
                }
                else
                {
                    container.NextContainer = nextContainer;
                }
                container = nextContainer;
            }

            return firstContainer;
        }

        // Copies a run of text into a ContentContainer.
        // Returns the next node to examine.
        private TextTreeNode CopyTextNode(TextTreeTextNode textNode, TextTreeNode haltNode, out ContentContainer container)
        {
            SplayTreeNode node;
            char[] text;
            int count;
            int symbolOffset;

            Invariant.Assert(textNode != haltNode, "Expect at least one node to copy!");

            symbolOffset = textNode.GetSymbolOffset(this.TextContainer.Generation);

            // Get a count of all the characters we're about to copy.
            count = 0;
            node = textNode;

            do
            {
                count += textNode.SymbolCount;

                node = textNode.GetNextNode();
                textNode = node as TextTreeTextNode;
            }
            while (textNode != null && textNode != haltNode);

            // Allocate storage.
            text = new char[count];

            // Copy the text.
            TextTreeText.ReadText(this.TextContainer.RootTextBlock, symbolOffset, count, text, 0 /*startIndex*/);

            container = new TextContentContainer(text);

            return (TextTreeNode)node;
        }


        /// <summary>
        /// Copies an embedded UIElement into a ContentContainer.
        /// Returns the next node to examine.
        /// </summary>
        //private TextTreeNode CopyObjectNode(TextTreeObjectNode objectNode, out ContentContainer container)
        //{

        //    string xml = XamlWriter.Save(objectNode.EmbeddedElement);

        //    container = new ObjectContentContainer(xml, objectNode.EmbeddedElement);

        //    return (TextTreeNode)objectNode.GetNextNode();
        //}

        // Copies a TextElement and all its contained content into a ContentContainer.
        // Returns the next node to examine.
        private TextTreeNode CopyElementNode(TextTreeTextElementNode elementNode, out ContentContainer container)
        {
            //if(elementNode.TextElement is Table)
            //{
            //    container = new TableElementContentContainer(elementNode.TextElement as Table,
            //                                            GetPropertyRecordArray(elementNode.TextElement),
            //                                            CopyContent((TextTreeNode)elementNode.GetFirstContainedNode(), null));
            //}
            //else
            //{
                container = new ElementContentContainer(elementNode.TextElement.GetType(),
                                                        GetPropertyRecordArray(elementNode.TextElement),
                                                        elementNode.TextElement.Resources,
                                                        CopyContent((TextTreeNode)elementNode.GetFirstContainedNode(), null));
            //}

            return (TextTreeNode)elementNode.GetNextNode();
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // ContentContainer encapsulates a serialized copy of one of
        //  - Text run.
        //  - UIElement.
        //  - TextElement.
        private abstract class ContentContainer
        {
            // Inserts the content held by this container at a specified position.
            // Navigator is positioned just past the new content on return.
            // Navigator is expected to have forward gravity.
            internal abstract void Do(TextPointer navigator);

            // The content following this container.  Always a different
            // container type.
            internal ContentContainer NextContainer
            {
                get { return _nextContainer; }
                set { _nextContainer = value; }
            }

            private ContentContainer _nextContainer;
        }

        // A serialized text run.
        private class TextContentContainer : ContentContainer
        {
            internal TextContentContainer(char[] text)
            {
                _text = text;
            }

            // Inserts the content held by this container at a specified position.
            // Navigator is positioned just past the new content on return.
            // Navigator is expected to have forward gravity.
            internal override void Do(TextPointer navigator)
            {
                navigator.TextContainer.InsertTextInternal(navigator, _text);
            }

            // The text covered by this run.
            private readonly char[] _text;
        }

        // A serialized UIElement.
        //private class ObjectContentContainer : ContentContainer
        //{
        //    internal ObjectContentContainer(string xml, object element)
        //    {
        //        _xml = xml;

        //        // Store a strong reference to the deleted element.
        //        // Note that we are not supposed to use this reference in any other way
        //        // as just to prevent garbage collector to delete object.
        //        // We need to keep it in memory for ensuring that image cache
        //        // keeps the same uri references to image bitmaps.
        //        // Otherwise uri-s of packaged (pasted) images will be broken
        //        // on undo.
        //        _element = element;
        //    }

        //    // Inserts the content held by this container at a specified position.
        //    // Navigator is positioned just past the new content on return.
        //    // Navigator is expected to have forward gravity.
        //    internal override void Do(TextPointer navigator)
        //    {
        //        IAvaloniaObject embeddedObject = null;

        //        // Get the object to be inserted.
        //        // If xml is null which we set it not to call SaveAsXml in the partial trust envirnment,
        //        // create the dummy Grid object to be inserted so that we can sync TextContainer
        //        // count with the undo unit.
        //        if (_xml != null)
        //        {
        //            try
        //            {
        //                embeddedObject = (IAvaloniaObject)XamlReader.Load(new XmlTextReader(new StringReader(_xml)));
        //            }
        //            catch (XamlParseException e)
        //            {
        //                Invariant.Assert(e != null); // Placed here for debugging convenience - to be able to see the exception.
        //            }
        //        }

        //        // When we cannot parse the object back, we loose it and substitute by an empty Grid
        //        // as an embeddedElement placeholder.
        //        if (embeddedObject == null)
        //        {
        //            embeddedObject = new Grid();
        //        }

        //        navigator.TextContainer.InsertEmbeddedObjectInternal(navigator, embeddedObject);
        //    }

        //    // Serialized UIElement xml.
        //    private readonly string _xml;

        //    // Stores a strong reference to the deleted content to make sure
        //    // that all image data remains in image cache.
        //    // The object is not supposed to be used in any other sense
        //    // in this undo unit - only as a strong reference.
        //    // Image cache keeps the association between an image bitmap data
        //    // and its source url until the bitmapdata is referred to by a strong reference.
        //    // That's why we need this.
        //    private readonly Object _element;
        //}

        // A serialized TextElement.
        private class ElementContentContainer : ContentContainer
        {
            // Creates a new instance.
            // childContainer holds all content covered by this TextElement.
            internal ElementContentContainer(Type elementType, PropertyRecord[] localValues, IResourceDictionary resources, ContentContainer childContainer)
            {
                _elementType = elementType;
                _localValues = localValues;
                _childContainer = childContainer;
                _resources = resources;
            }

            // Inserts the content held by this container at a specified position.
            // Navigator is positioned just past the new content on return.
            // Navigator is expected to have forward gravity.
            internal override void Do(TextPointer navigator)
            {
                ContentContainer container;
                TextElement element;

                // Insert the element.
                element = (TextElement)Activator.CreateInstance(_elementType);
                element.Reposition(navigator, navigator);

                // Get inside its scope.
                navigator.MoveToNextContextPosition(LogicalDirection.Backward);

                // Set local values.
                // Shouldn't we call this with deferLoad=true and call EndDeferLoad after all parameters set?
                navigator.TextContainer.SetValues(navigator, TextTreeUndoUnit.ArrayToLocalValueEnumerator(_localValues));

                // Restore resources
                element.Resources = _resources;

                // Insert contained content.
                for (container = _childContainer; container != null; container = container.NextContainer)
                {
                    container.Do(navigator);
                }

                // Move outside the element's scope again.
                navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            // TextElement type.
            private readonly Type _elementType;

            // Local property values set on the TextElement.
            private readonly PropertyRecord []_localValues;

            // Resources defined locally on the TextElement
            private readonly IResourceDictionary _resources;

            // Contained content.
            private readonly ContentContainer _childContainer;
        }

        // A serialized Table Element
        //private class TableElementContentContainer : ElementContentContainer
        //{
        //    internal TableElementContentContainer(Table table, PropertyRecord []localValues, ContentContainer childContainer) :
        //        base(table.GetType(), localValues, table.Resources, childContainer)
        //    {
        //        _cpTable = table.TextContainer.Start.GetOffsetToPosition(table.ContentStart);
        //        _columns = SaveColumns(table);
        //    }

        //    internal override void Do(TextPointer navigator)
        //    {
        //        base.Do(navigator);

        //        if(_columns != null)
        //        {
        //            TextPointer textPointerTable = new TextPointer(navigator.TextContainer.Start, _cpTable, LogicalDirection.Forward); 
        //            Table table = (Table) textPointerTable.Parent;
        //            RestoreColumns(table, _columns);
        //        }
        //    }

        //    private TableColumn[] _columns;
        //    private int _cpTable;
        //}

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Serialized content.
        private readonly ContentContainer _content;

        #endregion Private Fields
    }
}

