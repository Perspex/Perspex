// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text run properties provider.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Text run properties provider.
    // ----------------------------------------------------------------------
    internal sealed class TextProperties : TextRunProperties
    {
        // ------------------------------------------------------------------
        //
        //  TextRunProperties Implementation
        //
        // ------------------------------------------------------------------

        #region TextRunProperties Implementation

        // ------------------------------------------------------------------
        // Typeface used to format and display text.
        // ------------------------------------------------------------------
        public override Typeface Typeface { get { return _typeface; }  }

        // ------------------------------------------------------------------
        // Em size of font used to format and display text.
        // ------------------------------------------------------------------
        public override double FontRenderingEmSize
        {
            get
            {
                return _fontSize;
            }
        }

        // ------------------------------------------------------------------
        // Text decorations.
        // ------------------------------------------------------------------
        public override TextDecorationCollection TextDecorations { get { return _textDecorations; } }

        // ------------------------------------------------------------------
        // Text foreground bursh.
        // ------------------------------------------------------------------
        public override IBrush ForegroundBrush { get { return _foreground; } }

        // ------------------------------------------------------------------
        // Text background brush.
        // ------------------------------------------------------------------
        public override IBrush BackgroundBrush { get { return _backgroundBrush; } }

        // ------------------------------------------------------------------
        // Text vertical alignment.
        // ------------------------------------------------------------------
        public override BaselineAlignment BaselineAlignment { get { return _baselineAlignment; } }

        // ------------------------------------------------------------------
        // Text culture info.
        // ------------------------------------------------------------------
        public override CultureInfo CultureInfo { get { return _cultureInfo; } }

// TODO        // ------------------------------------------------------------------
// TODO        // Number substitution
// TODO        // ------------------------------------------------------------------
// TODO        public override NumberSubstitution NumberSubstitution { get { return _numberSubstitution; } }
// TODO
// TODO        // ------------------------------------------------------------------
// TODO        // Typography properties
// TODO        // ------------------------------------------------------------------
// TODO        public override TextRunTypographyProperties TypographyProperties{ get { return _typographyProperties; } }

        // ------------------------------------------------------------------
        // TextEffects property
        // ------------------------------------------------------------------
        // TODO public override TextEffectCollection TextEffects { get { return _textEffects; } }

        #endregion TextRunProperties Implementation

        // ------------------------------------------------------------------
        // Constructor.
        // ------------------------------------------------------------------
        internal TextProperties(Visual target, bool isTypographyDefaultValue)
        {
            // if none of the number substitution properties have changed, initialize the
            // _numberSubstitution field to a known default value
// TODO            if (!target.HasNumberSubstitutionChanged)
// TODO            {
// TODO                _numberSubstitution = FrameworkElement.DefaultNumberSubstitution;
// TODO            }

            InitCommon(target);
// TODO            if (!isTypographyDefaultValue)
// TODO            {
// TODO                _typographyProperties = TextElement.GetTypographyProperties(target);
// TODO            }
// TODO            else
// TODO            {
// TODO                _typographyProperties = Typography.Default;
// TODO            }

            _baselineAlignment = BaselineAlignment.Baseline;
        }

        internal TextProperties(IAvaloniaObject target, StaticTextPointer position, bool inlineObjects, bool getBackground, double pixelsPerDip)
        {
            // if none of the number substitution properties have changed, we may be able to
            // initialize the _numberSubstitution field to a known default value
            // TODO: We dont support number substitutions yet, but it's also questionable whether we need this optimization
            // TODO StyledElement fce = target as StyledElement;
            // TODO if (fce != null)
            // TODO {
            // TODO     if (!fce.HasNumberSubstitutionChanged)
            // TODO     {
            // TODO         _numberSubstitution = FrameworkContentElement.DefaultNumberSubstitution;
            // TODO     }
            // TODO }

            InitCommon(target);

            // TODO _typographyProperties = GetTypographyProperties(target);
            if (!inlineObjects)
            {
                _baselineAlignment = DynamicPropertyReader.GetBaselineAlignment(target);

                if (!position.IsNull)
                {
                    TextDecorationCollection highlightDecorations = GetHighlightTextDecorations(position);
                    if (highlightDecorations != null)
                    {
                        // Highlights (if present) take precedence over property value TextDecorations.
                        _textDecorations = highlightDecorations;
                    }
                }

                if (getBackground)
                {
                    _backgroundBrush = DynamicPropertyReader.GetBackgroundBrush(target);
                }
            }
            else
            {
                _baselineAlignment = DynamicPropertyReader.GetBaselineAlignmentForInlineObject(target);
                _textDecorations = DynamicPropertyReader.GetTextDecorationsForInlineObject(target, _textDecorations);

                if (getBackground)
                {
                    _backgroundBrush = DynamicPropertyReader.GetBackgroundBrushForInlineObject(position);
                }
            }
        }

        // Copy constructor, with override for default TextDecorationCollection value.
        internal TextProperties(TextProperties source, TextDecorationCollection textDecorations)
        {
            _backgroundBrush = source._backgroundBrush;
            _typeface = source._typeface;
            _fontSize = source._fontSize;
            _foreground = source._foreground;
            // TODO _textEffects = source._textEffects;
            _cultureInfo = source._cultureInfo;
            // TODO _numberSubstitution = source._numberSubstitution;
            // TODO _typographyProperties = source._typographyProperties;
            _baselineAlignment = source._baselineAlignment;
            _textDecorations = textDecorations;
        }

        // assigns values to all fields except for _typographyProperties, _baselineAlignment,
        // and _background, which are set appropriately in each constructor
        private void InitCommon(IAvaloniaObject target)
        {
            _typeface = DynamicPropertyReader.GetTypeface(target);

            _fontSize = target.GetValue(TextElement.FontSizeProperty);
            _foreground = target.GetValue(TextElement.ForegroundProperty);
            // TODO _textEffects = DynamicPropertyReader.GetTextEffects(target);

            _cultureInfo = DynamicPropertyReader.GetCultureInfo(target);
            _textDecorations = DynamicPropertyReader.GetTextDecorations(target);

            // as an optimization, we may have already initialized _numberSubstitution to a default
            // value if none of the NumberSubstitution dependency properties have changed
            // TODO if (_numberSubstitution == null)
            // TODO {
            // TODO     _numberSubstitution = DynamicPropertyReader.GetNumberSubstitution(target);
            // TODO }
        }

        // Gathers text decorations set on scoping highlights.
        // If no highlight properties are found, returns null
        private static TextDecorationCollection GetHighlightTextDecorations(StaticTextPointer highlightPosition)
        {
            TextDecorationCollection textDecorations = null;
            Highlights highlights = highlightPosition.TextContainer.Highlights;

            if (highlights == null)
            {
                return textDecorations;
             }

             //
             // Speller
             //
             textDecorations = highlights.GetHighlightValue(highlightPosition, LogicalDirection.Forward, typeof(SpellerHighlightLayer)) as TextDecorationCollection;

             //
             // IME composition
             //
 #if UNUSED_IME_HIGHLIGHT_LAYER
             TextDecorationCollection imeTextDecorations = highlights.GetHighlightValue(highlightPosition, LogicalDirection.Forward, typeof(FrameworkTextComposition)) as TextDecorationCollection;
             if (imeTextDecorations != null)
             {
                 textDecorations = imeTextDecorations;
             }
 #endif

             return textDecorations;
        }

        // ------------------------------------------------------------------
        // Retrieve typography properties from specified element.
        // ------------------------------------------------------------------
// TODO        private static TypographyProperties GetTypographyProperties(IAvaloniaObject element)
// TODO        {
// TODO            Debug.Assert(element != null);
// TODO
// TODO            TextBlock tb = element as TextBlock;
// TODO            if (tb != null)
// TODO            {
// TODO                if(!tb.IsTypographyDefaultValue)
// TODO                {
// TODO                    return TextElement.GetTypographyProperties(element);
// TODO                }
// TODO                else
// TODO                {
// TODO                    return Typography.Default;
// TODO                }
// TODO            }
// TODO
// TODO            TextBox textBox = element as TextBox;
// TODO            if (textBox != null)
// TODO            {
// TODO                if (!textBox.IsTypographyDefaultValue)
// TODO                {
// TODO                    return TextElement.GetTypographyProperties(element);
// TODO                }
// TODO                else
// TODO                {
// TODO                    return Typography.Default;
// TODO                }
// TODO            }
// TODO
// TODO            TextElement te = element as TextElement;
// TODO            if (te != null)
// TODO            {
// TODO                return te.TypographyPropertiesGroup;
// TODO            }
// TODO
// TODO            FlowDocument fd = element as FlowDocument;
// TODO            if (fd != null)
// TODO            {
// TODO               return fd.TypographyPropertiesGroup;
// TODO            }
// TODO
// TODO            // return default typography properties group
// TODO            return Typography.Default;
// TODO        }

        /// <summary>
        /// Set the BackgroundBrush
        /// </summary>
        /// <param name="backgroundBrush">The brush to set to</param>
        internal void SetBackgroundBrush(Brush backgroundBrush)
        {
            _backgroundBrush = backgroundBrush;
        }

        /// <summary>
        /// Set the ForegroundBrush
        /// </summary>
        /// <param name="foregroundBrush">The brush to set to</param>
        internal void SetForegroundBrush(Brush foregroundBrush)
        {
            _foreground = foregroundBrush;
        }

        // ------------------------------------------------------------------
        // Typeface.
        // ------------------------------------------------------------------
        private Typeface _typeface;

        // ------------------------------------------------------------------
        // Font size.
        // ------------------------------------------------------------------
        private double _fontSize;

        // ------------------------------------------------------------------
        // Foreground brush.
        // ------------------------------------------------------------------
        private IBrush _foreground;

        // ------------------------------------------------------------------
        // Text effects flags.
        // ------------------------------------------------------------------
        // TODO private TextEffectCollection _textEffects;

        // ------------------------------------------------------------------
        // Text decorations.
        // ------------------------------------------------------------------
        private TextDecorationCollection _textDecorations;

        // ------------------------------------------------------------------
        // Baseline alignment.
        // ------------------------------------------------------------------
        private BaselineAlignment _baselineAlignment;

        // ------------------------------------------------------------------
        // Text background brush.
        // ------------------------------------------------------------------
        private IBrush _backgroundBrush;

        // ------------------------------------------------------------------
        // Culture info.
        // ------------------------------------------------------------------
        private CultureInfo _cultureInfo;

        // ------------------------------------------------------------------
        // Number Substitution
        // ------------------------------------------------------------------
        // TODO private NumberSubstitution _numberSubstitution;

        // ------------------------------------------------------------------
        // Typography properties group.
        // ------------------------------------------------------------------
        // TODO private TextRunTypographyProperties _typographyProperties;
    }
}
