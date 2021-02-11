// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A static class providing information about text content schema
//

using System.Collections.Specialized;
using System.Net.Mime;
using Avalonia;
using Avalonia.Documents;
using Avalonia.Documents.Internal;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using MS.Internal;
    //using System.Collections.Generic;
    //using System.Windows.Controls; // TextBox, TextBlock
    //using System.Windows.Media; // Brush

    /// <summary>
    /// Provides an information about text structure schema.
    /// The schema is used in editing operations for maintaining
    /// content integrity.
    /// </summary>
    /// <remarks>
    /// Currently this class is totally private and hard-coded
    /// for some particular text structure.
    /// The intention is to make this mechanism public -
    /// as part of Text OM, so that custom backing store implementation
    /// could provide its own schemas.
    /// But we need to experiment with this approach first to make
    /// sure that it is feasible.
    /// </remarks>
    internal static class TextSchema
    {
        // ...............................................................
        //
        // Constructors
        //
        // ...............................................................

        static TextSchema()
        {
            // Initialize TextElement inheritable properties
            AvaloniaProperty[] textElementPropertyList = new AvaloniaProperty[]
                {
                    //FrameworkElement.LanguageProperty,
                    //FrameworkElement.FlowDirectionProperty,
                    //NumberSubstitution.CultureSourceProperty,
                    //NumberSubstitution.SubstitutionProperty,
                    //NumberSubstitution.CultureOverrideProperty,
                    TextElement.FontFamilyProperty,
                    TextElement.FontStyleProperty,
                    TextElement.FontWeightProperty,
                    //TextElement.FontStretchProperty,
                    TextElement.FontSizeProperty,
                    TextElement.ForegroundProperty,
                };

            _inheritableTextElementProperties = new AvaloniaProperty[textElementPropertyList.Length /*+ Typography.TypographyPropertiesList.Length*/];
            Array.Copy(textElementPropertyList, 0, _inheritableTextElementProperties, 0, textElementPropertyList.Length);
            //Array.Copy(Typography.TypographyPropertiesList, 0, _inheritableTextElementProperties, textElementPropertyList.Length, Typography.TypographyPropertiesList.Length);

            // Initialize Block/FlowDocument inheritable properties
            //AvaloniaProperty[] blockPropertyList = new AvaloniaProperty[]
            //    {
            //        Block.TextAlignmentProperty,
            //        Block.LineHeightProperty,
            //        Block.IsHyphenationEnabledProperty,
            //    };

            //_inheritableBlockProperties = new AvaloniaProperty[blockPropertyList.Length + _inheritableTextElementProperties.Length];
            //Array.Copy(blockPropertyList, 0, _inheritableBlockProperties, 0, blockPropertyList.Length);
            //Array.Copy(_inheritableTextElementProperties, 0, _inheritableBlockProperties, blockPropertyList.Length, _inheritableTextElementProperties.Length);

            //
            // Initialize TableCell related inheritable properties.
            //
            //AvaloniaProperty[] tableCellPropertyList = new AvaloniaProperty[]
            //    {
            //        Block.TextAlignmentProperty
            //    };
            //_inheritableTableCellProperties = new AvaloniaProperty[tableCellPropertyList.Length + _inheritableTextElementProperties.Length];
            //Array.Copy(tableCellPropertyList, _inheritableTableCellProperties, tableCellPropertyList.Length);
            //Array.Copy(_inheritableTextElementProperties, 0, _inheritableTableCellProperties, tableCellPropertyList.Length, _inheritableTextElementProperties.Length);
        }

        // ...............................................................
        //
        // Element Content Model
        //
        // ...............................................................

        internal static bool IsInTextContent(ITextPointer position)
        {
            return IsValidChild(position, typeof(string));
        }

#if UNUSED
        internal static bool IsValidChild(TextElement parent, TextElement child)
        {
            return ValidateChild(parent, child, false /* throwIfIllegalChild */, false /* throwIfIllegalHyperlinkDescendent */);
        }
#endif

        internal static bool ValidateChild(TextElement parent, TextElement child, bool throwIfIllegalChild, bool throwIfIllegalHyperlinkDescendent)
        {
            // Disallow nested hyperlink elements.
            if (TextSchema.HasHyperlinkAncestor(parent) &&
                TextSchema.HasIllegalHyperlinkDescendant(child, throwIfIllegalHyperlinkDescendent))
            {
                return false;
            }

            bool isValidChild = IsValidChild(parent.GetType(), child.GetType());

            if (!isValidChild && throwIfIllegalChild)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_ChildTypeIsInvalid, parent.GetType().Name, child.GetType().Name)*/);
            }

            return isValidChild;
        }

        internal static bool IsValidChild(TextElement parent, Type childType)
        {
            return ValidateChild(parent, childType, false /* throwIfIllegalChild */, false /* throwIfIllegalHyperlinkDescendent */);
        }

        internal static bool ValidateChild(TextElement parent, Type childType, bool throwIfIllegalChild, bool throwIfIllegalHyperlinkDescendent)
        {
            // Disallow nested hyperlink elements.
            if (TextSchema.HasHyperlinkAncestor(parent))
            {
                if (typeof(Hyperlink).IsAssignableFrom(childType) /*|| typeof(AnchoredBlock).IsAssignableFrom(childType)*/)
                {
                    if (throwIfIllegalHyperlinkDescendent)
                    {
                        throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_IllegalHyperlinkChild, childType)*/);
                    }
                    return false;
                }
            }

            bool isValidChild = IsValidChild(parent.GetType(), childType);

            if (!isValidChild && throwIfIllegalChild)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_ChildTypeIsInvalid, parent.GetType().Name, childType.Name)*/);
            }

            return isValidChild;
        }

        internal static bool IsValidChild(TextPointer position, Type childType)
        {
            return ValidateChild(position, childType, false /* throwIfIllegalChild */, false /* throwIfIllegalHyperlinkDescendent */);
        }

        internal static bool ValidateChild(TextPointer position, Type childType, bool throwIfIllegalChild, bool throwIfIllegalHyperlinkDescendent)
        {
            IAvaloniaObject parent = position.Parent;

            if (parent == null)
            {
                TextElement leftElement = position.GetAdjacentElementFromOuterPosition(LogicalDirection.Backward);
                TextElement rightElement = position.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                return (leftElement == null || IsValidSibling(leftElement.GetType(), childType)) && 
                    (rightElement == null || IsValidSibling(rightElement.GetType(), childType));
            }

            if (parent is TextElement)
            {
                return ValidateChild((TextElement)parent, childType, throwIfIllegalChild, throwIfIllegalHyperlinkDescendent);
            }

            bool isValidChild = IsValidChild(parent.GetType(), childType);

            if (!isValidChild && throwIfIllegalChild)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_ChildTypeIsInvalid, parent.GetType().Name, childType.Name)*/);
            }

            return isValidChild;
        }

        internal static bool IsValidSibling(Type siblingType, Type newType)
        {
            if (typeof(Inline).IsAssignableFrom(newType))
            {
                return typeof(Inline).IsAssignableFrom(siblingType);
            }
            //else if (typeof(Block).IsAssignableFrom(newType))
            //{
            //    return typeof(Block).IsAssignableFrom(siblingType);
            //}
            //else if (typeof(TableRowGroup).IsAssignableFrom(newType))
            //{
            //    return typeof(TableRowGroup).IsAssignableFrom(siblingType);
            //}
            //else if (typeof(TableRow).IsAssignableFrom(newType))
            //{
            //    return typeof(TableRow).IsAssignableFrom(siblingType);
            //}
            //else if (typeof(TableCell).IsAssignableFrom(newType))
            //{
            //    return typeof(TableCell).IsAssignableFrom(siblingType);
            //}
            //else if (typeof(ListItem).IsAssignableFrom(newType))
            //{
            //    return typeof(ListItem).IsAssignableFrom(siblingType);
            //}
            else
            {
                Invariant.Assert(false, "unexpected value for newType");
                return false;
            }
        }

        internal static bool IsValidChild(ITextPointer position, Type childType)
        {
            // Disallow nested hyperlink elements.
            if (typeof(TextElement).IsAssignableFrom(position.ParentType) &&
                TextPointerBase.IsInHyperlinkScope(position))
            {
                if (typeof(Hyperlink).IsAssignableFrom(childType) /*|| typeof(AnchoredBlock).IsAssignableFrom(childType)*/)
                {
                    return false;
                }
            }

            return IsValidChild(position.ParentType, childType);
        }

        internal static bool IsValidChildOfContainer(Type parentType, Type childType)
        {
            Invariant.Assert(!typeof(TextElement).IsAssignableFrom(parentType));
            return IsValidChild(parentType, childType);
        }

        // Walks parents of this TextElement until it finds a hyperlink ancestor.
        internal static bool HasHyperlinkAncestor(TextElement element)
        {
            Inline ancestor = element as Inline;

            while (ancestor != null && !(ancestor is Hyperlink))
            {
                ancestor = ancestor.Parent as Inline;
            }

            return ancestor != null;
        }

        /// <summary>
        /// Returns true indicatinng that a type can be skipped for pointer normalization -
        /// it is formattinng tag not producing a caret stop position.
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        internal static bool IsFormattingType(Type elementType)
        {
            return 
                typeof(Run).IsAssignableFrom(elementType) || 
                typeof(Span).IsAssignableFrom(elementType);
        }

        /// <summary>
        /// Determine if the given type is "known"-- that is, is part of the framework as
        /// opposed to a custom, user-defined type.
        /// </summary>
        internal static bool IsKnownType(Type elementType)
        {
            return elementType.Module == typeof(TextElement).Module /*|| presentationframework elementType.Module == typeof(StyledElement).Module*/; // presentationcore
        }

        internal static bool IsNonFormattingInline(Type elementType)
        {
            return typeof(Inline).IsAssignableFrom(elementType) && !IsFormattingType(elementType);
        }

        internal static bool IsMergeableInline(Type elementType)
        {
            return IsFormattingType(elementType) && !IsNonMergeableInline(elementType);
        }

        internal static bool IsNonMergeableInline(Type elementType)
        {
            TextElementEditingBehaviorAttribute att = (TextElementEditingBehaviorAttribute)Attribute.GetCustomAttribute(elementType, typeof(TextElementEditingBehaviorAttribute));
            if (att != null && att.IsMergeable == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true for a type which allows paragraph merging
        /// across its boundary.
        /// Hard-structured elements like Table, Floater, Figure
        /// does not allow paragraph merging.
        /// </summary>
        //internal static bool AllowsParagraphMerging(Type elementType)
        //{
        //    return
        //        typeof(Paragraph).IsAssignableFrom(elementType) ||
        //        typeof(ListItem).IsAssignableFrom(elementType) ||
        //        typeof(List).IsAssignableFrom(elementType) ||
        //        typeof(BitVector32.Section).IsAssignableFrom(elementType);
        //}

        /// <summary>
        /// Classifies the elementType as a generalized Paragraph -
        /// a block behaving similar to paragraph in regards of
        /// margins, indentations, bullets, and other paragraph properties.
        /// </summary>
        /// <param name="elementType">
        /// Element type to check
        /// </param>
        /// <returns>
        /// true if the element can be treated as a paragraph
        /// </returns>
        //internal static bool IsParagraphOrBlockUIContainer(Type elementType)
        //{
        //    return
        //        typeof(Paragraph).IsAssignableFrom(elementType) ||
        //        typeof(BlockUIContainer).IsAssignableFrom(elementType);
        //}

        // Identifies any block element
        //internal static bool IsBlock(Type type)
        //{
        //    return ( //
        //        typeof(Block).IsAssignableFrom(type));
        //}

        internal static bool IsBreak(Type type)
        {
            return ( //
                typeof(LineBreak).IsAssignableFrom(type));
        }

        // ...............................................................
        //
        // Formatting Properties
        //
        // ...............................................................

        // Helper for defining whether text decoration value is non-empty collection
        internal static bool HasTextDecorations(object value)
        {
            return (value is TextDecorationCollection) && ((TextDecorationCollection)value).Count > 0;
        }

        // From a given element type returns one of statically known
        // reduceElement parameter: True value of this parameter indicates that
        // serialization goes into XamlPackage, so all elements
        // can be preserved as is; otherwise some of them must be
        // reduced into simpler representations (such as InlineUIContainer -> Run
        // and BlockUIContainer -> Paragraph).
        internal static Type GetStandardElementType(Type type, bool reduceElement)
        {
            // Run-derived elements
            // --------------------
            if (typeof(Run).IsAssignableFrom(type))
            {
                // Must be after all elements derived from Run
                return typeof(Run);
            }

            // Span-derived elements
            // ---------------------
            else if (typeof(Hyperlink).IsAssignableFrom(type))
            {
                return typeof(Hyperlink);
            }
            else if (typeof(Span).IsAssignableFrom(type))
            {
                // Must be after all other standard Span-derived elements such as Hyperlink, Bold, etc.
                return typeof(Span);
            }

            // Other Inline elements
            // ---------------------
            else if (typeof(InlineUIContainer).IsAssignableFrom(type))
            {
                return reduceElement ? typeof(Run) : typeof(InlineUIContainer);
            }
            else if (typeof(LineBreak).IsAssignableFrom(type))
            {
                return typeof(LineBreak);
            }
            //else if (typeof(Floater).IsAssignableFrom(type))
            //{
            //    return typeof(Floater);
            //}
            //else if (typeof(Figure).IsAssignableFrom(type))
            //{
            //    return typeof(Figure);
            //}

            // Block-derived elements
            // ----------------------
            //else if (typeof(Paragraph).IsAssignableFrom(type))
            //{
            //    return typeof(Paragraph);
            //}
            //else if (typeof(BitVector32.Section).IsAssignableFrom(type))
            //{
            //    return typeof(BitVector32.Section);
            //}
            //else if (typeof(List).IsAssignableFrom(type))
            //{
            //    return typeof(List);
            //}
            //else if (typeof(Table).IsAssignableFrom(type))
            //{
            //    return typeof(Table);
            //}
            //else if (typeof(BlockUIContainer).IsAssignableFrom(type))
            //{
            //    return reduceElement ? typeof(Paragraph) : typeof(BlockUIContainer);
            //}

            // Other TextElements
            // ------------------
            //else if (typeof(ListItem).IsAssignableFrom(type))
            //{
            //    return typeof(ListItem);
            //}
            //else if (typeof(TableColumn).IsAssignableFrom(type))
            //{
            //    return typeof(TableColumn);
            //}
            //else if (typeof(TableRowGroup).IsAssignableFrom(type))
            //{
            //    return typeof(TableRowGroup);
            //}
            //else if (typeof(TableRow).IsAssignableFrom(type))
            //{
            //    return typeof(TableRow);
            //}
            //else if (typeof(TableCell).IsAssignableFrom(type))
            //{
            //    return typeof(TableCell);
            //}

            // To make compiler happy in cases of Invariant.Assert - return something
            Invariant.Assert(false, "We do not expect any unknown elements derived directly from TextElement, Block or Inline. Schema must have been checking for that");
            return null;
        }

        // Returns a list of inheritable properties applicable to a particular type
        internal static AvaloniaProperty[] GetInheritableProperties(Type type)
        {
            //if (typeof(TableCell).IsAssignableFrom(type))
            //{
            //    return _inheritableTableCellProperties;
            //}

            //if (typeof(Block).IsAssignableFrom(type) || typeof(FlowDocument).IsAssignableFrom(type))
            //{
            //    return _inheritableBlockProperties;
            //}

            Invariant.Assert(typeof(TextElement).IsAssignableFrom(type)/* || typeof(TableColumn).IsAssignableFrom(type)*/,"type must be one of TextElement, FlowDocument or TableColumn");

            return _inheritableTextElementProperties;
        }

        // Returns a list of noninheritable properties applicable to a particular type
        // They are safe to be transferred from outer scope to inner scope when the outer one 
        // is removed (e.g. TextRangeEdit.RemoveUnnecessarySpans(...)). 
        internal static AvaloniaProperty[] GetNoninheritableProperties(Type type)
        {
            // Run-derived elements
            // --------------------
            if (typeof(Run).IsAssignableFrom(type))
            {
                // Must be after all elements derived from Run
                return _inlineProperties;
            }

            // Span-derived elements
            // ---------------------
            else if (typeof(Hyperlink).IsAssignableFrom(type))
            {
                return _hyperlinkProperties;
            }
            else if (typeof(Span).IsAssignableFrom(type))
            {
                // Must be after all other standard Span-derived elements such as Hyperlink, Bold, etc.
                return _inlineProperties;
            }

            // Other Inline elements
            // ---------------------
            else if (typeof(InlineUIContainer).IsAssignableFrom(type))
            {
                return _inlineProperties;
            }
            else if (typeof(LineBreak).IsAssignableFrom(type))
            {
                return _emptyPropertyList;
            }
            //else if (typeof(Floater).IsAssignableFrom(type))
            //{
            //    return _floaterProperties;
            //}
            //else if (typeof(Figure).IsAssignableFrom(type))
            //{
            //    return _figureProperties;
            //}

            // Block-derived elements
            // ----------------------
            //else if (typeof(Paragraph).IsAssignableFrom(type))
            //{
            //    return _paragraphProperties;
            //}
            //else if (typeof(BitVector32.Section).IsAssignableFrom(type))
            //{
            //    return _blockProperties;
            //}
            //else if (typeof(List).IsAssignableFrom(type))
            //{
            //    return _listProperties;
            //}
            //else if (typeof(Table).IsAssignableFrom(type))
            //{
            //    return _tableProperties;
            //}
            //else if (typeof(BlockUIContainer).IsAssignableFrom(type))
            //{
            //    return _blockProperties;
            //}

            // Other TextElements
            // ------------------
            //else if (typeof(ListItem).IsAssignableFrom(type))
            //{
            //    return _listItemProperties;
            //}
            //else if (typeof(TableColumn).IsAssignableFrom(type))
            //{
            //    return _tableColumnProperties;
            //}
            //else if (typeof(TableRowGroup).IsAssignableFrom(type))
            //{
            //    return _tableRowGroupProperties;
            //}
            //else if (typeof(TableRow).IsAssignableFrom(type))
            //{
            //    return _tableRowProperties;
            //}
            //else if (typeof(TableCell).IsAssignableFrom(type))
            //{
            //    return _tableCellProperties;
            //}

            Invariant.Assert(false, "We do not expect any unknown elements derived directly from TextElement. Schema must have been checking for that");
            return _emptyPropertyList; // to make compiler happy
        }

        // Compares two values for equality
        /// <summary>
        /// Property comparison helper.
        /// Compares property values for equivalence from serialization
        /// standpoint. In editing we consider properties equal
        /// if they have the same serialized representation.
        /// Differences coming from current dynamic state changes
        /// should not affect comparison if they are not going to be
        /// visible after serialization.
        /// Instantiation dirrefences are also insignificant.
        /// </summary>
        /// <param name="value1">
        /// </param>
        /// <param name="value2">
        /// </param>
        /// <returns>
        /// True if two values have the same serialized representation
        /// </returns>
        internal static bool ValuesAreEqual(object value1, object value2)
        {
            if ((object)value1 == (object)value2) // this check includes two nulls
            {
                return true;
            }

            // Comparing null with empty collections
            if (value1 == null)
            {
                if (value2 is TextDecorationCollection)
                {
                    TextDecorationCollection decorations2 = (TextDecorationCollection)value2;
                    return decorations2.Count == 0;
                }
                //else if (value2 is TextEffectCollection)
                //{
                //    TextEffectCollection effects2 = (TextEffectCollection)value2;
                //    return effects2.Count == 0;
                //}
                return false;
            }
            else if (value2 == null)
            {
                if (value1 is TextDecorationCollection)
                {
                    TextDecorationCollection decorations1 = (TextDecorationCollection)value1;
                    return decorations1.Count == 0;
                }
                //else if (value1 is TextEffectCollection)
                //{
                //    TextEffectCollection effects1 = (TextEffectCollection)value1;
                //    return effects1.Count == 0;
                //}
                return false;
            }

            // Must be of exactly the same types (really ?)
            // Should we ever return true for different types?
            if (value1.GetType() != value2.GetType())
            {
                return false;
            }

            // Special cases for known types: TextDecorations, FontFamily, Brush
            if (value1 is TextDecorationCollection)
            {
                TextDecorationCollection decorations1 = (TextDecorationCollection)value1;
                TextDecorationCollection decorations2 = (TextDecorationCollection)value2;
                return decorations1.ValueEquals(decorations2);
            }
            else if (value1 is FontFamily)
            {
                FontFamily fontFamily1 = (FontFamily)value1;
                FontFamily fontFamily2 = (FontFamily)value2;
                return fontFamily1.Equals(fontFamily2);
            }
            else if (value1 is IBrush)
            {
                return AreBrushesEqual((IBrush)value1, (IBrush)value2);
            }
            else
            {
                string string1 = value1.ToString();
                string string2 = value2.ToString();
                return string1 == string2;
            }
        }

        /// <summary>
        /// Tests whether it is the paragraph property or not.
        /// Used to decide should the property be applied to character runs or to paragraphs
        /// in TextRange.ApplyProperty()
        /// </summary>
        //internal static bool IsParagraphProperty(AvaloniaProperty formattingProperty)
        //{
        //    // Check inheritable paragraph properties
        //    for (int i = 0; i < _inheritableBlockProperties.Length; i++)
        //    {
        //        if (formattingProperty == _inheritableBlockProperties[i])
        //        {
        //            return true;
        //        }
        //    }

        //    // Check non-inheritable paragraph properties
        //    for (int i = 0; i < _paragraphProperties.Length; i++)
        //    {
        //        if (formattingProperty == _paragraphProperties[i])
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Returns true if this property is applicable to inline character formatting element
        /// </summary>
        internal static bool IsCharacterProperty(AvaloniaProperty formattingProperty)
        {
            // Check inheritable inline properties
            for (int i = 0; i < _inheritableTextElementProperties.Length; i++)
            {
                if (formattingProperty == _inheritableTextElementProperties[i])
                {
                    return true;
                }
            }

            // Check non-inheritable Inline properties
            for (int i = 0; i < _inlineProperties.Length; i++)
            {
                if (formattingProperty == _inlineProperties[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if this property is a character property NOT affecting formatting.
        /// </summary>
        internal static bool IsNonFormattingCharacterProperty(AvaloniaProperty property)
        {
            for (int i = 0; i < _nonFormattingCharacterProperties.Length; i++)
            {
                if (property == _nonFormattingCharacterProperties[i])
                {
                    return true;
                }
            }

            return false;
        }

        internal static AvaloniaProperty[] GetNonFormattingCharacterProperties()
        {
            return _nonFormattingCharacterProperties;
        }

        /// <summary>
        /// Returns true if this property is a structural property of inline element.
        /// </summary>
        /// <remarks>
        /// A structural character property is more strict for its scope than other (non-structural) inline properties (such as fontweight).
        /// While the associativity rule holds true for non-structural properties when there values are equal,
        ///     (FontWeight)A (FontWeight)B == (FontWeight) AB
        /// this does not hold true for structual properties even when there values may be equal,
        ///     (FlowDirection)A (FlowDirection)B != (FlowDirection)A B 
        /// Hence, these properties require special logic in setting, merging, splitting rules for inline elements.
        /// </remarks>
        internal static bool IsStructuralCharacterProperty(AvaloniaProperty formattingProperty)
        {
            int i;

            for (i = 0; i < _structuralCharacterProperties.Length; i++)
            {
                if (formattingProperty == _structuralCharacterProperties[i])
                    break;
            }

            return (i < _structuralCharacterProperties.Length);
        }

        // Returns true if a property value can be incremented or decremented
        internal static bool IsPropertyIncremental(AvaloniaProperty property)
        {
            if (property == null)
            {
                return false;
            }

            Type propertyType = property.PropertyType;

            return
                typeof(double).IsAssignableFrom(propertyType) ||
                typeof(long).IsAssignableFrom(propertyType) ||
                typeof(int).IsAssignableFrom(propertyType) ||
                typeof(Thickness).IsAssignableFrom(propertyType);
        }

        // Set of properties affecting editing behavior that must be transferred
        // from hosting UI elements (like RichTextBox) to FlowDocument to ensure appropriate
        // editing behavior. 
        // This is especially important for FlowDocuments with FormattingDefaults="Standalone".
        //internal static AvaloniaProperty[] BehavioralProperties
        //{
        //    get
        //    {
        //        return _behavioralPropertyList;
        //    }
        //}

        //internal static AvaloniaProperty[] ImageProperties
        //{
        //    get
        //    {
        //        return _imagePropertyList;
        //    }
        //}

        // List of structural properties.
        internal static AvaloniaProperty[] StructuralCharacterProperties
        {
            get
            {
                return _structuralCharacterProperties;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static bool IsValidChild(Type parentType, Type childType)
        {
            // Text Content
            if (parentType == null || // Make sure that TextPointer.ParentType is never null. This happens in PasswordBox and in TextBox.
                typeof(Run).IsAssignableFrom(parentType) )
                //||
                //typeof(TextBox).IsAssignableFrom(parentType) ||
                //typeof(PasswordBox).IsAssignableFrom(parentType))
            {
                // NOTE: Even though we use TextBlock or FlowDocumentView for TextBox's render scope,
                // a parent for position will be directly TextBlock or PasswordBox, thus allowing
                // text content. Otherwise neither TextBlock nor FlowDocumentView allow direct text content -
                // only through Run.
                return childType == typeof(string);
            }
            // TextBlock allowed children
            else if (typeof(NewTextBlock).IsAssignableFrom(parentType))
            {
                return typeof(Inline).IsAssignableFrom(childType) /* TODO &&
                    !typeof(AnchoredBlock).IsAssignableFrom(childType)*/;
            }
            // Hyperlink allowed children
            else if (typeof(Hyperlink).IsAssignableFrom(parentType))
            {
                return typeof(Inline).IsAssignableFrom(childType) &&
                    !typeof(Hyperlink).IsAssignableFrom(childType) /*&&*/
                    /*!typeof(AnchoredBlock).IsAssignableFrom(childType)*/;
            }
            // Inline items
            else if (typeof(Span).IsAssignableFrom(parentType) /* || typeof(Paragraph).IsAssignableFrom(parentType) || typeof(AccessText).IsAssignableFrom(parentType)*/)
            {
                return typeof(Inline).IsAssignableFrom(childType);
            }
            // Inline UIElements
            else if (typeof(InlineUIContainer).IsAssignableFrom(parentType))
            {
                return typeof(StyledElement).IsAssignableFrom(childType);
            }
            //// List Content
            //else if (typeof(List).IsAssignableFrom(parentType))
            //{
            //    return typeof(ListItem).IsAssignableFrom(childType);
            //}
            //// Table Content
            //else if (typeof(Table).IsAssignableFrom(parentType))
            //{
            //    return typeof(TableRowGroup).IsAssignableFrom(childType);
            //}
            //else if (typeof(TableRowGroup).IsAssignableFrom(parentType))
            //{
            //    return typeof(TableRow).IsAssignableFrom(childType);
            //}
            //else if (typeof(TableRow).IsAssignableFrom(parentType))
            //{
            //    return typeof(TableCell).IsAssignableFrom(childType);
            //}
            //// Block Content
            //// We can move this test up to improve perf
            //else if (
            //    typeof(BitVector32.Section).IsAssignableFrom(parentType) ||
            //    typeof(ListItem).IsAssignableFrom(parentType) ||
            //    typeof(TableCell).IsAssignableFrom(parentType) ||
            //    typeof(Floater).IsAssignableFrom(parentType) ||
            //    typeof(Figure).IsAssignableFrom(parentType) ||
            //    typeof(FlowDocument).IsAssignableFrom(parentType))
            //{
            //    return typeof(Block).IsAssignableFrom(childType);
            //}
            //// Block UIElements
            //else if (typeof(BlockUIContainer).IsAssignableFrom(parentType))
            //{
            //    return typeof(UIElement).IsAssignableFrom(childType);
            //}
            else
            {
                return false;
            }
        }

        // Returns true if passed textelement has any Hyperlink or AnchoredBlock descendant.
        // It this context, the element or one of its ancestors is a Hyperlink.
        private static bool HasIllegalHyperlinkDescendant(TextElement element, bool throwIfIllegalDescendent)
        {
            TextPointer start = element.ElementStart;
            TextPointer end = element.ElementEnd;

            while (start.CompareTo(end) < 0)
            {
                TextPointerContext forwardContext = start.GetPointerContext(LogicalDirection.Forward);
                if (forwardContext == TextPointerContext.ElementStart)
                {
                    TextElement nextElement = (TextElement)start.GetAdjacentElement(LogicalDirection.Forward);

                    if (nextElement is Hyperlink /*||*/
                        /*nextElement is AnchoredBlock*/)
                    {
                        if (throwIfIllegalDescendent)
                        {
                            throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_IllegalHyperlinkChild, nextElement.GetType())*/);
                        }
                        return true;
                    }
                }

                start = start.GetNextContextPosition(LogicalDirection.Forward);
            }
            return false;
        }

        private static bool AreBrushesEqual(IBrush brush1, IBrush brush2)
        {
            SolidColorBrush solidBrush1 = brush1 as SolidColorBrush;
            if (solidBrush1 != null)
            {
                return solidBrush1.Color.Equals(((SolidColorBrush)brush2).Color);
            }
            else
            {
                // When the brush is not serializable to string, we consider values equal only if they are equal as objects
                //string string1 = DPTypeDescriptorContext.GetStringValue(TextElement.BackgroundProperty, brush1);
                //string string2 = DPTypeDescriptorContext.GetStringValue(TextElement.BackgroundProperty, brush2);
                //return string1 != null && string2 != null ? string1 == string2 : false;
                return brush1.Equals(brush2);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // List of all inheritable properties applicable to TextElement types
        private static readonly AvaloniaProperty[] _inheritableTextElementProperties;

        // Block element adds a few inhertiable properties that dont apply to other TextElement types, 
        // this list includes inheritable properties applicable to Block and also root FlowDocument types.
        //private static readonly AvaloniaProperty[] _inheritableBlockProperties;

        // TableCell element adds inheritable TextAlignmentProperty that doesn't apply to other TextElement types.
        //private static readonly AvaloniaProperty[] _inheritableTableCellProperties;

        // List of all non-inheritable properties applicable to Hyperlink element
        private static readonly AvaloniaProperty[] _hyperlinkProperties = new AvaloniaProperty[]
            {
                Hyperlink.NavigateUriProperty,
                //Hyperlink.TargetNameProperty,
                Hyperlink.CommandProperty,
                Hyperlink.CommandParameterProperty,
                //Hyperlink.CommandTargetProperty,

                // Inherits Inline Properties
                Inline.BaselineAlignmentProperty,
                Inline.TextDecorationsProperty,

                // Inherits TextElement properties
                TextElement.BackgroundProperty,
                //TextElement.TextEffectsProperty, -- the property is not supported in text editor

                // Inherits FrameworkContentElement properties
                //FrameworkContentElement.ToolTipProperty,
            };

        // List of all non-inheritable properties applicable to Inline element
        private static readonly AvaloniaProperty[] _inlineProperties = new AvaloniaProperty[]
            {
                Inline.BaselineAlignmentProperty,
                Inline.TextDecorationsProperty,

                // Inherits TextElement properties
                TextElement.BackgroundProperty,
                //TextElement.TextEffectsProperty, -- the property is not supported in text editor
            };

        // List of all non-inheritable properties applicable to Paragraph element
        //private static readonly AvaloniaProperty[] _paragraphProperties = new AvaloniaProperty[]
        //    {
        //        Paragraph.MinWidowLinesProperty,
        //        Paragraph.MinOrphanLinesProperty,
        //        Paragraph.TextIndentProperty,
        //        Paragraph.KeepWithNextProperty,
        //        Paragraph.KeepTogetherProperty,
        //        Paragraph.TextDecorationsProperty,

        //        // Inherits Block properties
        //        Block.MarginProperty,
        //        Block.PaddingProperty,
        //        Block.BorderThicknessProperty,
        //        Block.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to List element
        //private static readonly AvaloniaProperty[] _listProperties = new AvaloniaProperty[]
        //    {
        //        List.MarkerStyleProperty,
        //        List.MarkerOffsetProperty,
        //        List.StartIndexProperty,

        //        // Inherits Block properties
        //        Block.MarginProperty,
        //        Block.PaddingProperty,
        //        Block.BorderThicknessProperty,
        //        Block.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to ListItem element
        //private static readonly AvaloniaProperty[] _listItemProperties = new AvaloniaProperty[]
        //    {
        //        // Adds owner to Block properties
        //        ListItem.MarginProperty,
        //        ListItem.PaddingProperty,
        //        ListItem.BorderThicknessProperty,
        //        ListItem.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to Table element
        //private static readonly AvaloniaProperty[] _tableProperties = new AvaloniaProperty[]
        //    {
        //        Table.CellSpacingProperty,

        //        // Inherits Block properties
        //        Block.MarginProperty,
        //        Block.PaddingProperty,
        //        Block.BorderThicknessProperty,
        //        Block.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to TableColumn element
        //private static readonly AvaloniaProperty[] _tableColumnProperties = new AvaloniaProperty[]
        //    {
        //        TableColumn.WidthProperty,
        //        TableColumn.BackgroundProperty,
        //    };

        // List of all non-inheritable properties applicable to TableRowGroup element
        //private static readonly AvaloniaProperty[] _tableRowGroupProperties = new AvaloniaProperty[]
        //    {
        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to TableRow element
        //private static readonly AvaloniaProperty[] _tableRowProperties = new AvaloniaProperty[]
        //    {
        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to TableCell element
        //private static readonly AvaloniaProperty[] _tableCellProperties = new AvaloniaProperty[]
        //    {
        //        TableCell.ColumnSpanProperty,
        //        TableCell.RowSpanProperty,

        //        // Adds ownership to Block properties
        //        TableCell.PaddingProperty,
        //        TableCell.BorderThicknessProperty,
        //        TableCell.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to Floater element
        //private static readonly AvaloniaProperty[] _floaterProperties = new AvaloniaProperty[]
        //    {
        //        Floater.HorizontalAlignmentProperty,
        //        Floater.WidthProperty,

        //        // Adds ownership to Block properties
        //        Floater.MarginProperty,
        //        Floater.PaddingProperty,
        //        Floater.BorderThicknessProperty,
        //        Floater.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to Figure element
        //private static readonly AvaloniaProperty[] _figureProperties = new AvaloniaProperty[]
        //    {
        //        Figure.HorizontalAnchorProperty,
        //        Figure.VerticalAnchorProperty,
        //        Figure.HorizontalOffsetProperty,
        //        Figure.VerticalOffsetProperty,
        //        Figure.CanDelayPlacementProperty,
        //        Figure.WrapDirectionProperty,
        //        Figure.WidthProperty,
        //        Figure.HeightProperty,

        //        // Adds ownership to Block properties
        //        Figure.MarginProperty,
        //        Figure.PaddingProperty,
        //        Figure.BorderThicknessProperty,
        //        Figure.BorderBrushProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to Block element
        //private static readonly AvaloniaProperty[] _blockProperties = new AvaloniaProperty[]
        //    {
        //        Block.MarginProperty,
        //        Block.PaddingProperty,
        //        Block.BorderThicknessProperty,
        //        Block.BorderBrushProperty,
        //        Block.BreakPageBeforeProperty,
        //        Block.BreakColumnBeforeProperty,
        //        Block.ClearFloatersProperty,
        //        Block.IsHyphenationEnabledProperty,

        //        // Inherits TextElement properties
        //        TextElement.BackgroundProperty,
        //        //TextElement.TextEffectsProperty, -- the property is not supported in text editor
        //    };

        // List of all non-inheritable properties applicable to TextElement element
        private static readonly AvaloniaProperty[] _textElementPropertyList = new AvaloniaProperty[]
            {
                TextElement.BackgroundProperty,
                //TextElement.TextEffectsProperty, -- the property is not supported in text editor
            };

        // List of all non-inheritable properties applicable to Image element
        //private static readonly AvaloniaProperty[] _imagePropertyList = new AvaloniaProperty[]
        //    {
        //        MediaTypeNames.Image.SourceProperty,
        //        MediaTypeNames.Image.StretchProperty,
        //        MediaTypeNames.Image.StretchDirectionProperty,

        //        // Inherits FrameworkElement properties
        //        //FrameworkElement.StyleProperty,
        //        //FrameworkElement.OverridesDefaultStyleProperty,
        //        //FrameworkElement.DataContextProperty,
        //        FrameworkElement.LanguageProperty,
        //        //FrameworkElement.NameProperty,
        //        //FrameworkElement.TagProperty,
        //        //FrameworkElement.InputScopeProperty,
        //        FrameworkElement.LayoutTransformProperty,
        //        FrameworkElement.WidthProperty,
        //        FrameworkElement.MinWidthProperty,
        //        FrameworkElement.MaxWidthProperty,
        //        FrameworkElement.HeightProperty,
        //        FrameworkElement.MinHeightProperty,
        //        FrameworkElement.MaxHeightProperty,
        //        //FrameworkElement.FlowDirectionProperty,
        //        FrameworkElement.MarginProperty,
        //        FrameworkElement.HorizontalAlignmentProperty,
        //        FrameworkElement.VerticalAlignmentProperty,
        //        //FrameworkElement.FocusVisualStyleProperty,
        //        FrameworkElement.CursorProperty,
        //        FrameworkElement.ForceCursorProperty,
        //        //FrameworkElement.FocusableProperty,
        //        FrameworkElement.ToolTipProperty,
        //        //FrameworkElement.ContextMenuProperty,

        //        // Inherits UIElement properties
        //        //UIElement.AllowDropProperty,
        //        UIElement.RenderTransformProperty,
        //        UIElement.RenderTransformOriginProperty,
        //        UIElement.OpacityProperty,
        //        UIElement.OpacityMaskProperty,
        //        UIElement.BitmapEffectProperty,
        //        UIElement.BitmapEffectInputProperty,
        //        UIElement.VisibilityProperty,
        //        UIElement.ClipToBoundsProperty,
        //        UIElement.ClipProperty,
        //        UIElement.SnapsToDevicePixelsProperty,

        //        // Inherits TextBlock properties
        //        TextBlock.BaselineOffsetProperty,
        //    };

        // Behavioral property list
        //private static readonly AvaloniaProperty[] _behavioralPropertyList = new AvaloniaProperty[] 
        //    { 
        //        StyledElement.AllowDropProperty,
        //    };

        // Empty property list
        private static readonly AvaloniaProperty[] _emptyPropertyList = new AvaloniaProperty[] { };

        // Structural property list.
        // NB: Existing code depends on these being inheritable properties.
        private static readonly AvaloniaProperty[] _structuralCharacterProperties = new AvaloniaProperty[]
            {
                Inline.FlowDirectionProperty,
            };

        // List of inline properties (both inheritable or non-inheritable) that are "content" properties, not "formatting" properties.
        private static readonly AvaloniaProperty[] _nonFormattingCharacterProperties = new AvaloniaProperty[]
            {
                //FrameworkElement.FlowDirectionProperty,
                //FrameworkElement.LanguageProperty,
                Run.TextProperty,
            };

        #endregion Private Fields
    }
}
