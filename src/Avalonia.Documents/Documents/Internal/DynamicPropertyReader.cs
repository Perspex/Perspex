// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Helper methods to retrive dynami properties from
//              IAvaloniaObjects.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Avalonia;
using Avalonia.Data;
using Avalonia.Documents;
using Avalonia.Documents.Internal;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Helper methods to retrive dynami properties from IAvaloniaObjects.
    // ----------------------------------------------------------------------
    internal static class DynamicPropertyReader
    {
        // ------------------------------------------------------------------
        //
        // Property Groups
        //
        // ------------------------------------------------------------------

        #region Property Groups

        // ------------------------------------------------------------------
        // Retrieve typeface properties from specified element.
        // ------------------------------------------------------------------
        internal static Typeface GetTypeface(IAvaloniaObject element)
        {
            Debug.Assert(element != null);

            FontFamily  fontFamily  = (FontFamily)  element.GetValue(TextElement.FontFamilyProperty);
            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            // TODO FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight);
        }

        internal static Typeface GetModifiedTypeface(IAvaloniaObject element, FontFamily fontFamily)
        {
            Debug.Assert(element != null);

            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            // TODO FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight);
        }

        // ------------------------------------------------------------------
        // Retrieve text properties from specified inline object.
        //
        // WORKAROUND: see PS task #13486 & #3399.
        // For inline object go to its parent and retrieve text decoration
        // properties from there.
        // ------------------------------------------------------------------
        internal static TextDecorationCollection GetTextDecorationsForInlineObject(IAvaloniaObject element, TextDecorationCollection textDecorations)
        {
            Debug.Assert(element != null);

            IAvaloniaObject parent = LogicalTreeHelper.GetParent(element);
            TextDecorationCollection parentTextDecorations = null;

            if (parent != null)
            {
                // Get parent text decorations if it is non-null
                parentTextDecorations = GetTextDecorations(parent);
            }

            // see if the two text decorations are equal.
            bool textDecorationsEqual = (textDecorations == null) ?
                                         parentTextDecorations == null
                                       : textDecorations.ValueEquals(parentTextDecorations);

            if (!textDecorationsEqual)
            {
                if (parentTextDecorations == null)
                {
                    textDecorations = null;
                }
                else
                {
                    textDecorations = new TextDecorationCollection();
                    int count = parentTextDecorations.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        textDecorations.Add(parentTextDecorations[i]);
                    }
                }
            }
            return textDecorations;
        }

        /// <summary>
        /// Helper method to get a TextDecorations property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        internal static TextDecorationCollection GetTextDecorations(IAvaloniaObject element)
        {
            return GetCollectionValue(element, Inline.TextDecorationsProperty) as TextDecorationCollection;
        }

        /// <summary>
        /// Helper method to get a TextEffects property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        // TODO internal static TextEffectCollection GetTextEffects(IAvaloniaObject element)
        // TODO {
        // TODO     return GetCollectionValue(element, TextElement.TextEffectsProperty) as TextEffectCollection;
        // TODO }

        /// <summary>
        /// Helper method to get a collection property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        /// <remarks>
        /// Property system's GetValue() call creates a mutable empty collection when the property is accessed for the first time. 
        /// To avoids workingset overhead of those empty collections, we return null instead.  
        /// </remarks>
        private static T GetCollectionValue<T>(IAvaloniaObject element, AvaloniaProperty<T> property) where T : class
        {
            if (element.IsSet(property))
            {
                return element.GetValue(property);
            }

            return null;              
        }        

        #endregion Property Groups

        // ------------------------------------------------------------------
        //
        // Block Properties
        //
        // ------------------------------------------------------------------

        #region Block Properties

        // ------------------------------------------------------------------
        // GetKeepTogether
        // ------------------------------------------------------------------
// TODO        internal static bool GetKeepTogether(IAvaloniaObject element)
// TODO        {
// TODO            Paragraph p = element as Paragraph;
// TODO            return (p != null) ? p.KeepTogether : false;
// TODO        }

// TODO        // ------------------------------------------------------------------
// TODO        // GetKeepWithNext
// TODO        // ------------------------------------------------------------------
// TODO        internal static bool GetKeepWithNext(IAvaloniaObject element)
// TODO        {
// TODO            Paragraph p = element as Paragraph;
// TODO            return (p != null) ? p.KeepWithNext : false;
// TODO        }

// TODO        // ------------------------------------------------------------------
// TODO        // GetMinWidowLines
// TODO        // ------------------------------------------------------------------
// TODO        internal static int GetMinWidowLines(IAvaloniaObject element)
// TODO        {
// TODO            Paragraph p = element as Paragraph;
// TODO            return (p != null) ? p.MinWidowLines : 0;
// TODO        }

// TODO        // ------------------------------------------------------------------
// TODO        // GetMinOrphanLines
// TODO        // ------------------------------------------------------------------
// TODO        internal static int GetMinOrphanLines(IAvaloniaObject element)
// TODO        {
// TODO            Paragraph p = element as Paragraph;
// TODO            return (p != null) ? p.MinOrphanLines : 0;
// TODO        }

        #endregion Block Properties

        // ------------------------------------------------------------------
        //
        // Misc Properties
        //
        // ------------------------------------------------------------------

        #region Misc Properties


        /// <summary>
        /// Gets actual value of LineHeight property. If LineHeight is Double.Nan, returns FontSize*FontFamily.LineSpacing
        /// </summary>
// TODO       internal static double GetLineHeightValue(IAvaloniaObject d)
// TODO       {
// TODO           double lineHeight = (double)d.GetValue(NewTextBlock.LineHeightProperty);
// TODO           // If LineHeight value is 'Auto', treat it as LineSpacing * FontSize.
// TODO           if (double.IsNaN(lineHeight))
// TODO           {
// TODO               FontFamily fontFamily = (FontFamily)d.GetValue(TextElement.FontFamilyProperty);
// TODO               double fontSize = (double)d.GetValue(TextElement.FontSizeProperty);
// TODO               lineHeight = fontFamily.LineSpacing * fontSize;
// TODO           }
// TODO           return Math.Max(TextDpi.MinWidth, Math.Min(TextDpi.MaxWidth, lineHeight));
// TODO       }

        // ------------------------------------------------------------------
        // Retrieve background brush property from specified element.
        // If 'element' is the same object as paragraph owner, ignore background
        // brush, because it is handled outside as paragraph's background.
        // NOTE: This method is only used to read background of text content.
        //
        //      element - Element associated with content. Passed only for
        //              performance reasons; it can be extracted from 'position'.
        //      paragraphOwner - Owner of paragraph (usually the parent of 'element').
        //
        // ------------------------------------------------------------------
        internal static IBrush GetBackgroundBrush(IAvaloniaObject element)
        {
            Debug.Assert(element != null);
            IBrush backgroundBrush = null;

            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            while (backgroundBrush == null && CanApplyBackgroundBrush(element))
            {
                backgroundBrush = element.GetValue(TextElement.BackgroundProperty);
                Invariant.Assert(element is StyledElement);
                element = ((StyledElement)element).Parent;
            }
            return backgroundBrush;
        }

        // ------------------------------------------------------------------
        // Retrieve background brush property from specified UIElement.
        //
        //      position - Exact position of the content.
        // ------------------------------------------------------------------
        internal static IBrush GetBackgroundBrushForInlineObject(StaticTextPointer position)
        {
            object selected;
            IBrush backgroundBrush;

            Debug.Assert(!position.IsNull);

            selected = position.TextContainer.Highlights.GetHighlightValue(position, LogicalDirection.Forward, null /* TODO typeof(TextSelection)*/);

            if (selected == AvaloniaProperty.UnsetValue)
            {
                backgroundBrush = (IBrush) position.GetValue(TextElement.BackgroundProperty);
            }
            else
            {
                backgroundBrush = SelectionHighlightInfo.BackgroundBrush;
            }
            return backgroundBrush;
        }

        // ------------------------------------------------------------------
        // GetBaselineAlignment
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignment(IAvaloniaObject element)
        {
            Inline i = element as Inline;
            BaselineAlignment baselineAlignment = (i != null) ? i.BaselineAlignment : BaselineAlignment.Baseline;

            // Walk up the tree to check if it inherits BaselineAlignment from a parent
            while (i != null && BaselineAlignmentIsDefault(i))
            {
                i = i.Parent as Inline;
            }

            if (i != null)
            {
                // Found an Inline with non-default baseline alignment
                baselineAlignment = i.BaselineAlignment;
            }
            return baselineAlignment;
        }

        // ------------------------------------------------------------------
        // GetBaselineAlignmentForInlineObject
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignmentForInlineObject(IAvaloniaObject element)
        {
            return GetBaselineAlignment(LogicalTreeHelper.GetParent(element));
        }

        // ------------------------------------------------------------------
        // Retrieve CultureInfo property from specified element.
        // ------------------------------------------------------------------
        internal static CultureInfo GetCultureInfo(IAvaloniaObject element)
        {
            // TODO XmlLanguage language = (XmlLanguage) element.GetValue(FrameworkElement.LanguageProperty);
            // TODO try
            // TODO {
            // TODO     return language.GetSpecificCulture();
            // TODO }
            // TODO catch (InvalidOperationException)
            // TODO {
            // TODO     // We default to en-US if no part of the language tag is recognized.
            // TODO     return System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;
            // TODO }
            return CultureInfo.CurrentCulture;
        }

        // ------------------------------------------------------------------
        // Retrieve Number substitution properties from given element
        // ------------------------------------------------------------------
// TODO        internal static NumberSubstitution GetNumberSubstitution(IAvaloniaObject element)
// TODO        {
// TODO            NumberSubstitution numberSubstitution = new NumberSubstitution();
// TODO
// TODO            numberSubstitution.CultureSource = (NumberCultureSource)element.GetValue(NumberSubstitution.CultureSourceProperty);
// TODO            numberSubstitution.CultureOverride = (CultureInfo)element.GetValue(NumberSubstitution.CultureOverrideProperty);
// TODO            numberSubstitution.Substitution = (NumberSubstitutionMethod)element.GetValue(NumberSubstitution.SubstitutionProperty);
// TODO
// TODO            return numberSubstitution;
// TODO        }

        private static bool CanApplyBackgroundBrush(IAvaloniaObject element)
        {
            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            // We will only apply background on Inline elements that are not AnchoredBlocks.
            // NOTE: We ideally do not need the AnchoredBlock check because when walking up the content tree we should hit a block before
            // an AnchoredBlock. Leaving it in in case this helper is used for other purposes.
            if (!(element is Inline) /* TODO || element is AnchoredBlock */)
            {
                return false;
            }
            return true;
        }

        private static bool BaselineAlignmentIsDefault(IAvaloniaObject element)
        {
            Invariant.Assert(element != null);

            // TODO: Check if this is translated right
            return !element.IsSet(Inline.BaselineAlignmentProperty);
        }

        #endregion Misc Properties
    }
 }
