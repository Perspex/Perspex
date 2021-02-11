// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace MS.Internal.Text
{
    /// <summary>
    /// TextModifier that can modify properties of a text span. 
    /// It supports modifying TextDecorations and bid embedding levels 
    /// over a span of runs.
    /// </summary>
    internal class TextSpanModifier : TextModifier
    {
        private int                      _length;
        
        private TextDecorationCollection _modifierDecorations;
        private IBrush                    _modifierBrush;
        private FlowDirection            _flowDirection;
        private bool                     _hasDirectionalEmbedding;

        /// <summary>
        /// Creates a TextSpanModifier with the specified length and
        /// properties to modify TextDecorations. 
        /// Instance created will not affect bidi embedding level. 
        /// </summary>
        public TextSpanModifier(int length, TextDecorationCollection textDecorations, IBrush foregroundBrush)
        {
            _length = length;
            _modifierDecorations = textDecorations;
            _modifierBrush = foregroundBrush;
        }

        /// <summary>
        /// Creates a TextSpanModifier with the specified length and
        /// properties. Instance created will modify Bidi embedding level. It will also modify TextDecorations if the
        /// input TextDecorations is not null.
        /// </summary>
        public TextSpanModifier(int length, TextDecorationCollection textDecorations, IBrush foregroundBrush, FlowDirection flowDirection)
            : this (length, textDecorations, foregroundBrush)
        {
            _hasDirectionalEmbedding = true;
            _flowDirection = flowDirection;
        }     

        /// <summary>
        /// Character length
        /// </summary>
        public sealed override int TextSourceLength
        {
            get { return _length; }
        }

        /// <summary>
        /// A set of properties shared by every characters in the run
        /// It is null for a TextModifier run.
        /// </summary>
        public sealed override TextRunProperties Properties
        {
            get { return null; }
        }

        /// <summary>
        /// Modifies the properties of a text run.
        /// </summary>
        /// <param name="properties">Properties of a text run or the return value of
        /// ModifyProperties for a nested text modifier.</param>
        /// <returns>Returns the actual text run properties to be used for formatting,
        /// subject to further modification by text modifiers at outer scopes.</returns>
        public sealed override TextRunProperties ModifyProperties(TextRunProperties properties)
        {
            // Get the text decorations applied to the text modifier run. If there are
            // none, we don't change anything.
            if (properties == null || _modifierDecorations == null || _modifierDecorations.Count == 0)
                return properties;        

            // Let brush be the foreground brush for the text modifier run. Any text
            // decorations defined at the text modifier scope that have a null Pen
            // should be drawn using this brush, which means we may need to copy some
            // TextDecoration objects and set the Pen property on the copies. We can
            // elide this if the same brush is used at both scopes. We shouldn't miss
            // too many optimization opportunities by using the (lower cost) reference
            // comparison here because in most cases where the brushes are equal it's
            // because it's an inherited property.
            IBrush brush = _modifierBrush;
            if (object.ReferenceEquals(brush, properties.ForegroundBrush))
            {
                // No need to set the pen property.
                brush = null;
            }

            // We're going to create a merged set of text decorations.
            TextDecorationCollection mergedDecorations;

            // Get the text decorations of the affected run, if any.
            TextDecorationCollection runDecorations = properties.TextDecorations;
            if (runDecorations == null || runDecorations.Count == 0)
            {
                // Only the text modifier run defines text decorations so
                // we don't need to merge anything.
                if (brush == null)
                {
                    // Use the text decorations of the modifier run.
                    mergedDecorations = _modifierDecorations;
                }
                else
                {
                    // The foreground brushes differ so copy the text decorations to a
                    // new collection and make sure each has a non-null pen.
                    mergedDecorations = CopyTextDecorations(_modifierDecorations, brush);
                }
            }
            else
            {
                // Add the modifier decorations first because we want text decorations
                // defined at the inner scope (e.g., by the run) to be drawn on top.
                mergedDecorations = CopyTextDecorations(_modifierDecorations, brush);

                // Add the text decorations defined at the inner scope; we never need
                // to set the pen for these because they should be drawn using the
                // foreground brush.
                foreach (TextDecoration td in runDecorations)
                {
                    mergedDecorations.Add(td);
                }
            }

            return new MergedTextRunProperties(properties, mergedDecorations);
        }

        public /* TODO override */ bool HasDirectionalEmbedding
        {
            get { return _hasDirectionalEmbedding; }
        }

        public /* TODO override */ FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }

        private TextDecorationCollection CopyTextDecorations(TextDecorationCollection textDecorations, IBrush brush)
        {
            TextDecorationCollection result = new TextDecorationCollection();
            Pen pen = null;

            foreach (TextDecoration td in textDecorations)
            {
                if (td.Stroke == null && brush != null)
                {
                    if (pen == null)
                        pen = new Pen(brush, 1);

                    TextDecoration copy = td.Clone();
                    copy.Stroke = pen.Brush;
                    copy.StrokeThickness = pen.Thickness;
                    result.Add(copy);
                }
                else
                {
                    result.Add(td);
                }
            }

            return result;
        }

        private class MergedTextRunProperties : TextRunProperties
        {
            TextRunProperties _runProperties;
            TextDecorationCollection _textDecorations;

            internal MergedTextRunProperties(
                TextRunProperties runProperties, 
                TextDecorationCollection textDecorations)
            {
                _runProperties = runProperties;
                _textDecorations = textDecorations;
// TODO                PixelsPerDip = _runProperties.PixelsPerDip;
            }

            public override Typeface Typeface
            { 
                get { return _runProperties.Typeface; } 
            }

            public override double FontRenderingEmSize
            {
                get { return _runProperties.FontRenderingEmSize; }
            }

// TODO            public override double FontHintingEmSize
// TODO            {
// TODO                get { return _runProperties.FontHintingEmSize; }
// TODO            }

            public override TextDecorationCollection TextDecorations
            {
                get { return _textDecorations; }
            }

            public override IBrush ForegroundBrush
            {
                get { return _runProperties.ForegroundBrush; }
            }

            public override IBrush BackgroundBrush
            {
                get { return _runProperties.BackgroundBrush; }
            }

            public override CultureInfo CultureInfo
            {
                get { return _runProperties.CultureInfo; }
            }

// TODO            public override TextEffectCollection TextEffects
// TODO            {
// TODO                get { return _runProperties.TextEffects; }
// TODO            }

            public override BaselineAlignment BaselineAlignment
            {
                get { return _runProperties.BaselineAlignment; }
            }

// TODO            public override TextRunTypographyProperties TypographyProperties
// TODO            {
// TODO                get { return _runProperties.TypographyProperties; }
// TODO            }
// TODO
// TODO            public override NumberSubstitution NumberSubstitution
// TODO            {
// TODO                get { return _runProperties.NumberSubstitution; }
// TODO            }
        }
    }
}
