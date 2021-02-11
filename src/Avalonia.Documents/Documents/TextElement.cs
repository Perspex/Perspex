// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Base class for content in text based FrameworkElement.
//

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

using System.Collections;
//using System.ComponentModel;
//using System.Windows.Controls;
using System.Windows.Markup;
//using System.Windows.Media;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using MS.Internal;
//using MS.Internal.PresentationFramework;
//using MS.Internal.PtsHost.UnsafeNativeMethods; // PTS restrictions
//using MS.Internal.Text;

namespace System.Windows.Documents
{
    /// <summary>
    /// TextElement is an  base class for content in text based FrameworkElement
    /// controls such as Text, FlowDocument, or RichTextBox.  TextElements span
    /// other content, applying property values or providing structural information.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public abstract class TextElement : StyledElement, IAddChild
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        //static TextElement()
        //{
        //    // For attached properties metadata specific to the type needs to be set using OverrideMetadata
        //    // instead of passing it during property registration. Otherwise all types will get it.
        //    PropertyChangedCallback typographyChanged = new PropertyChangedCallback(OnTypographyChanged);

        //    // Registering typography properties metadata
        //    AvaloniaProperty[] typographyProperties = Typography.TypographyPropertiesList;
        //    for (int i = 0; i < typographyProperties.Length; i++)
        //    {
        //        typographyProperties[i].OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(typographyChanged));
        //    }
        //}

        /// <summary>
        /// Internal constructor to prevent publicly derived classes.
        /// </summary>
        internal TextElement() : base()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Extracts this TextElement from its current position, if any, and
        /// inserts it at a specified location.
        /// </summary>
        /// <param name="start">
        /// New start position.
        /// </param>
        /// <param name="end">
        /// New end position.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if start and end are not
        /// positioned within the same document, or if start is positioned
        /// after end, or if start == null but end != null.
        /// </exception>
        /// <remarks>
        /// This method extracts the TextElement from its current position,
        /// leaving behind any contained content, before inserting the TextElement
        /// at a new location.
        ///
        /// If start is null, end must also be null, and the TextElement will
        /// not be inserted into a new document.
        /// </remarks>
        internal void Reposition(TextPointer start, TextPointer end)
        {
            TextContainer tree;

            if (start != null)
            {
                ValidationHelper.VerifyPositionPair(start, end);
            }
            else if (end != null)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextElement_UnmatchedEndPointer)*/);
            }

            if (start != null)
            {
                // start/end must be equally scoped.  But we want to discount
                // this TextElement when considering scoping -- it will be
                // extracted before the final insert.

                SplayTreeNode startNode = start.GetScopingNode();
                // Suppress presharp 6506: Parameter 'end' to this public method must be validated.
                // We already validated it indirectly above, when calling ValidationHelper.VerifyPositionPair.
#pragma warning suppress 6506
                SplayTreeNode endNode = end.GetScopingNode();

                if (startNode == _textElementNode)
                {
                    startNode = _textElementNode.GetContainingNode();
                }
                if (endNode == _textElementNode)
                {
                    endNode = _textElementNode.GetContainingNode();
                }

                if (startNode != endNode)
                {
                    throw new ArgumentException(/*SR.Get(SRID.InDifferentScope, "start", "end")*/);
                }
            }

            if (this.IsInTree)
            {
                tree = EnsureTextContainer();

                if (start == null)
                {
                    //
                    // Case 0: Extract this element from its tree.
                    //

                    tree.BeginChange();
                    try
                    {
                        tree.ExtractElementInternal(this);
                    }
                    finally
                    {
                        tree.EndChange();
                    }
                }
                else
                {
                    // Presharp doesn't understand that by design TextPointer.TextContainer can never be null.
#pragma warning suppress 6506
                    if (tree == start.TextContainer)
                    {
                        //
                        // Case 1: extract and insert this TextElement within the same tree.
                        //

                        tree.BeginChange();
                        try
                        {
                            tree.ExtractElementInternal(this);
                            tree.InsertElementInternal(start, end, this);
                        }
                        finally
                        {
                            tree.EndChange();
                        }
                    }
                    else
                    {
                        //
                        // Case 2: extract and insert this TextElement from one tree to another tree.
                        //

                        tree.BeginChange();
                        try
                        {
                            tree.ExtractElementInternal(this);
                        }
                        finally
                        {
                            tree.EndChange();
                        }

                        // Presharp doesn't understand that by design TextPointer.TextContainer can never be null.
#pragma warning suppress 56506
                        start.TextContainer.BeginChange();
                        try
                        {
                            start.TextContainer.InsertElementInternal(start, end, this);
                        }
                        finally
                        {
                            start.TextContainer.EndChange();
                        }
                    }
                }
            }
            else if (start != null)
            {
                //
                // Case 3: insert this TextElement to a new tree (this is no current tree).
                //

                start.TextContainer.BeginChange();
                try
                {
                    start.TextContainer.InsertElementInternal(start, end, this);
                }
                finally
                {
                    start.TextContainer.EndChange();
                }
            }
        }

        /// <summary>
        /// Extracts this TextElement and its content from its current
        /// position, if any, and inserts it at a specified location.
        /// </summary>
        /// <param name="textPosition">
        /// New position.
        /// </param>
        /// <remarks>
        /// This method extracts the TextElement from its current position,
        /// including any contained content, before inserting the TextElement
        /// at a new location.
        ///
        /// If textPosition is null, the TextElement will not be inserted into
        /// a new document.
        /// </remarks>
        internal void RepositionWithContent(TextPointer textPosition)
        {
            TextContainer tree;

            if (textPosition == null)
            {
                if (this.IsInTree)
                {
                    tree = EnsureTextContainer();

                    // Presharp doesn't understand that by design EnsureTextContainer can never return null.
#pragma warning suppress 6506
                    tree.BeginChange();
                    try
                    {
                        tree.DeleteContentInternal(this.ElementStart, this.ElementEnd);
                    }
                    finally
                    {
                        tree.EndChange();
                    }
                }
            }
            else
            {
                tree = textPosition.TextContainer;

                // Presharp doesn't understand that by design TextPointer.TextContainer can never be null.
#pragma warning suppress 56506
                tree.BeginChange();
                try
                {
                    tree.InsertElementInternal(textPosition, textPosition, this);
                }
                finally
                {
                    tree.EndChange();
                }
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        internal static readonly AttachedProperty<TextElement> ContainerTextElementProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, TextElement>("ContainerTextElement");

        /// <summary>
        /// A TextRange spanning the content of this element.
        /// </summary>
        internal TextRange TextRange
        {
            get
            {
                VerifyAccess();

                TextContainer tree = EnsureTextContainer();

                TextPointer contentStart = new TextPointer(tree, _textElementNode, ElementEdge.AfterStart, LogicalDirection.Backward);
                contentStart.Freeze();

                TextPointer contentEnd = new TextPointer(tree, _textElementNode, ElementEdge.BeforeEnd, LogicalDirection.Forward);
                contentEnd.Freeze();

                return new TextRange(contentStart, contentEnd);
            }
        }

        /// <summary>
        /// A TextPointer located just before the start edge of this TextElement.
        /// </summary>
        /// <Remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection set Forward.
        /// </Remarks>
        public TextPointer ElementStart
        {
            get
            {
                TextContainer tree;
                TextPointer elementStart;

                tree = EnsureTextContainer();

                elementStart = new TextPointer(tree, _textElementNode, ElementEdge.BeforeStart, LogicalDirection.Forward);
                elementStart.Freeze();

                return elementStart;
            }
        }

        internal StaticTextPointer StaticElementStart
        {
            get
            {
                TextContainer tree = EnsureTextContainer();

                return new StaticTextPointer(tree, _textElementNode, 0);
            }
        }

        /// <summary>
        /// A TextPointer located just past the start edge of this TextElement.
        /// </summary>
        /// <Remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection set Backward.
        /// </Remarks>
        public TextPointer ContentStart
        {
            get
            {
                TextContainer tree;
                TextPointer contentStart;

                tree = EnsureTextContainer();

                contentStart = new TextPointer(tree, _textElementNode, ElementEdge.AfterStart, LogicalDirection.Backward);
                contentStart.Freeze();

                return contentStart;
            }
        }

        internal StaticTextPointer StaticContentStart
        {
            get
            {
                TextContainer tree = EnsureTextContainer();

                return new StaticTextPointer(tree, _textElementNode, 1);
            }
        }

        /// <summary>
        /// A TextPointer located just before the end edge of this TextElement.
        /// </summary>
        /// <Remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection set Forward.
        /// </Remarks>
        public TextPointer ContentEnd
        {
            get
            {
                TextContainer tree;
                TextPointer contentEnd;

                tree = EnsureTextContainer();

                contentEnd = new TextPointer(tree, _textElementNode, ElementEdge.BeforeEnd, LogicalDirection.Forward);
                contentEnd.Freeze();

                return contentEnd;
            }
        }

        internal StaticTextPointer StaticContentEnd
        {
            get
            {
                TextContainer tree = EnsureTextContainer();

                return new StaticTextPointer(tree, _textElementNode, _textElementNode.SymbolCount - 1);
            }
        }

        // Returns true if a position belongs to inner part of this TextElement (ContentStart and ContentEnd including).
        internal bool Contains(TextPointer position)
        {
            TextContainer tree = EnsureTextContainer();
            ValidationHelper.VerifyPosition(tree, position);
            return this.ContentStart.CompareTo(position) <= 0 && this.ContentEnd.CompareTo(position) >= 0;
        }

        /// <summary>
        /// A TextPointer located just after the end edge of this TextElement.
        /// </summary>
        /// <Remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection set Backward.
        /// </Remarks>
        public TextPointer ElementEnd
        {
            get
            {
                TextContainer tree;
                TextPointer elementEnd;

                tree = EnsureTextContainer();

                elementEnd = new TextPointer(tree, _textElementNode, ElementEdge.AfterEnd, LogicalDirection.Backward);
                elementEnd.Freeze();

                return elementEnd;
            }
        }

        internal StaticTextPointer StaticElementEnd
        {
            get
            {
                TextContainer tree = EnsureTextContainer();

                return new StaticTextPointer(tree, _textElementNode, _textElementNode.SymbolCount);
            }
        }

        #region DependencyProperties


        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontFamily>(
                nameof(FontFamily),
                defaultValue: Avalonia.Media.FontFamily.Default,
                inherits: true,
                validate: IsValidFontFamily);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: Avalonia.Media.FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, IBrush>(
                nameof(Foreground),
                Brushes.Black,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<TextElement, IBrush>(
                nameof(Background),
                inherits: true);

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush used to paint the element.
        /// </summary>
        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// The Background property defines the brush used to fill the content area.
        /// </summary>
        public IBrush Background
        {
            get { return (IBrush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// AvaloniaProperty setter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontFamily(IAvaloniaObject element, FontFamily value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// AvaloniaProperty getter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontFamily GetFontFamily(IAvaloniaObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontFamily)element.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// AvaloniaProperty setter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontStyle(IAvaloniaObject element, FontStyle value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// AvaloniaProperty getter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontStyle GetFontStyle(IAvaloniaObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontStyle)element.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// AvaloniaProperty setter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontWeight(IAvaloniaObject element, FontWeight value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// AvaloniaProperty getter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontWeight GetFontWeight(IAvaloniaObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (FontWeight)element.GetValue(FontWeightProperty);
        }

        ///// <summary>
        ///// AvaloniaProperty for <see cref="FontStretch" /> property.
        ///// </summary>
        //[CommonAvaloniaProperty]
        //public static readonly AvaloniaProperty FontStretchProperty =
        //        AvaloniaProperty.RegisterAttached(
        //                "FontStretch",
        //                typeof(FontStretch),
        //                typeof(TextElement),
        //                new FrameworkPropertyMetadata(
        //                        FontStretches.Normal,
        //                        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        ///// <summary>
        ///// The FontStretch property selects a normal, condensed, or extended face from a font family.
        ///// </summary>
        //public FontStretch FontStretch
        //{
        //    get { return (FontStretch) GetValue(FontStretchProperty); }
        //    set { SetValue(FontStretchProperty, value); }
        //}

        ///// <summary>
        ///// AvaloniaProperty setter for <see cref="FontStretch" /> property.
        ///// </summary>
        ///// <param name="element">The element to which to write the attached property.</param>
        ///// <param name="value">The property value to set</param>
        //public static void SetFontStretch(IAvaloniaObject element, FontStretch value)
        //{
        //    if (element == null)
        //    {
        //        throw new ArgumentNullException("element");
        //    }

        //    element.SetValue(FontStretchProperty, value);
        //}

        ///// <summary>
        ///// AvaloniaProperty getter for <see cref="FontStretch" /> property.
        ///// </summary>
        ///// <param name="element">The element from which to read the attached property.</param>
        //public static FontStretch GetFontStretch(IAvaloniaObject element)
        //{
        //    if (element == null)
        //    {
        //        throw new ArgumentNullException("element");
        //    }

        //    return (FontStretch)element.GetValue(FontStretchProperty);
        //}

        /// <summary>
        /// AvaloniaProperty setter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontSize(IAvaloniaObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// AvaloniaProperty getter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        //[TypeConverter(typeof(FontSizeConverter))]
        public static double GetFontSize(IAvaloniaObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (double)element.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// AvaloniaProperty setter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetForeground(IAvaloniaObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// AvaloniaProperty getter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static IBrush GetForeground(IAvaloniaObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return element.GetValue(ForegroundProperty);
        }

        ///// <summary>
        ///// AvaloniaProperty for <see cref="TextEffectCollection" /> property.
        ///// It doesn't affect layout
        ///// </summary>
        //public static readonly AvaloniaProperty TextEffectsProperty =
        //        AvaloniaProperty.Register(
        //                "TextEffects",
        //                typeof(TextEffectCollection),
        //                typeof(TextElement),
        //                new FrameworkPropertyMetadata(
        //                        new FreezableDefaultValueFactory(TextEffectCollection.Empty),
        //                        FrameworkPropertyMetadataOptions.AffectsRender));

        ///// <summary>
        ///// The TextEffects property specifies effects that are added to the text of an element.
        ///// </summary>
        //public TextEffectCollection TextEffects
        //{
        //    get { return (TextEffectCollection) GetValue(TextEffectsProperty); }
        //    set { SetValue(TextEffectsProperty, value); }
        //}

        ///// <summary>
        ///// Class providing access to all text typography properties
        ///// </summary>
        //public Typography Typography
        //{
        //    get
        //    {
        //        return new Typography(this);
        //    }
        //}

        #endregion DependencyProperties

        #region IAddChild

        ///<summary>
        /// Called to add the object as a child.
        ///</summary>
        ///<param name="value">
        /// A Block to add as a child.
        ///</param>
        /// <exception cref="System.ArgumentException">
        /// o must derive from either UIElement or TextElement, or an
        /// ArgumentException will be thrown by this method.
        /// </exception>
        void IAddChild.AddChild(object value)
        {
            Type valueType = value.GetType();

            TextElement te = value as TextElement;

            if (te != null)
            {
                TextSchema.ValidateChild(/*parent:*/this, /*child:*/te, true /* throwIfIllegalChild */, true /* throwIfIllegalHyperlinkDescendent */);
                Append(te);
            }
            else
            {
                IControl uie = value as IControl;
                if (uie != null)
                {
                    InlineUIContainer inlineContainer = this as InlineUIContainer;
                    if (inlineContainer != null)
                    {
                        if (inlineContainer.Child != null)
                        {
                            throw new ArgumentException(/*SR.Get(SRID.TextSchema_ThisInlineUIContainerHasAChildUIElementAlready, this.GetType().Name, ((InlineUIContainer)this).Child.GetType().Name, value.GetType().Name)*/);
                        }

                        inlineContainer.Child = uie;
                    }
                    else
                    {
                        //BlockUIContainer blockContainer = this as BlockUIContainer;
                        //if (blockContainer != null)
                        //{
                        //    if (blockContainer.Child != null)
                        //    {
                        //        throw new ArgumentException(SR.Get(SRID.TextSchema_ThisBlockUIContainerHasAChildUIElementAlready, this.GetType().Name, ((BlockUIContainer)this).Child.GetType().Name, value.GetType().Name));
                        //    }

                        //    blockContainer.Child = uie;
                        //}
                        //else
                        //{
                        if (TextSchema.IsValidChild(/*parent:*/this, /*childType:*/typeof(InlineUIContainer)))
                        {
                            // Create implicit InlineUIContainer wrapper for this UIElement
                            InlineUIContainer implicitInlineUIContainer = Inline.CreateImplicitInlineUIContainer();
                            Append(implicitInlineUIContainer);
                            implicitInlineUIContainer.Child = uie;
                        }
                        else
                        {
                            throw new ArgumentException(/*SR.Get(SRID.TextSchema_ChildTypeIsInvalid, this.GetType().Name, value.GetType().Name)*/);
                        }
                        //}
                    }
                }
                else
                {
                    throw new ArgumentException(/*SR.Get(SRID.TextSchema_ChildTypeIsInvalid, this.GetType().Name, value.GetType().Name)*/);
                }
            }
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to this TextElement
        ///</param>
        /// <ExternalAPI/>
        void IAddChild.AddText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // Check if text run is allowed in this element,
            // and create implicit Run if possible.
            if (TextSchema.IsValidChild(/*parent:*/this, /*childType:*/typeof(string)))
            {
                Append(text);
            }
            else
            {
                // Implicit Run creation
                if (TextSchema.IsValidChild(/*parent:*/this, /*childType:*/typeof(Run)))
                {
                    // NOTE: Do not use new Run(text) constructor to avoid TextContainer creation
                    // which would hit parser perf
                    Run implicitRun = Inline.CreateImplicitRun();

                    Append(implicitRun);

                    implicitRun.Text = text;
                }
                else
                {
                    // Otherwise text is not allowed. Throw if it is not a whitespace
                    if (text.Trim().Length > 0)
                    {
                        throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_TextIsNotAllowed, this.GetType().Name)*/);
                    }

                    // As to whitespace - it can be simply ignored
                }
            }
        }

        #endregion IAddChild

        #region LogicalTree

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected virtual IEnumerator GetChildElementEnumerator()
        {
            return this.IsEmpty
                    ? new RangeContentEnumerator(null, null)
                    : new RangeContentEnumerator(this.ContentStart, this.ContentEnd);
        }


        #endregion LogicalTree

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        ///// <summary>
        ///// Notification that a specified property has been invalidated
        ///// </summary>
        ///// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        //protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        //{
        //    // Always call base.OnPropertyChanged, otherwise Property Engine will not work.
        //    base.OnPropertyChanged(e);

        //    // Note whether or not this change due to a SetValue/ClearValue call.
        //    bool localValueChanged = e.NewValueSource == BaseValueSourceInternal.Local || e.OldValueSource == BaseValueSourceInternal.Local;

        //    if (localValueChanged || e.IsAValueChange || e.IsASubPropertyChange)
        //    {
        //        if (this.IsInTree) // No work to do if no one's listening.
        //        {
        //            // If the modified property affects layout we have some additional
        //            // bookkeeping to take care of.
        //            FrameworkPropertyMetadata fmetadata = e.Metadata as FrameworkPropertyMetadata;
        //            if (fmetadata != null)
        //            {
        //                bool affectsMeasureOrArrange = fmetadata.AffectsMeasure || fmetadata.AffectsArrange || fmetadata.AffectsParentMeasure || fmetadata.AffectsParentArrange;
        //                bool affectsRender = (fmetadata.AffectsRender &&
        //                    (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender));
        //                if (affectsMeasureOrArrange || affectsRender)
        //                {
        //                    TextContainer textContainer = EnsureTextContainer();

        //                    textContainer.BeginChange();
        //                    try
        //                    {
        //                        if (localValueChanged)
        //                        {
        //                            TextTreeUndo.CreatePropertyUndoUnit(this, e);
        //                        }

        //                        if (e.IsAValueChange || e.IsASubPropertyChange)
        //                        {
        //                            NotifyTypographicPropertyChanged(affectsMeasureOrArrange, localValueChanged, e.Property);
        //                        }
        //                    }
        //                    finally
        //                    {
        //                        textContainer.EndChange();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //// Notify our TextContainer that a typographic property has changed
        //// value on this TextElement.
        //// This has the side effect of invalidating layout.
        //internal void NotifyTypographicPropertyChanged(bool affectsMeasureOrArrange, bool localValueChanged, AvaloniaProperty property)
        //{
        //    if (!this.IsInTree) // No work to do if no one's listening.
        //    {
        //        return;
        //    }

        //    TextContainer tree;
        //    TextPointer beforeStart;

        //    tree = EnsureTextContainer();

        //    // Take note that something layout related has changed.
        //    tree.NextLayoutGeneration();

        //    // Notify any external listeners.
        //    if (tree.HasListeners)
        //    {
        //        // Get the position before the start of this element.
        //        beforeStart = new TextPointer(tree, _textElementNode, ElementEdge.BeforeStart, LogicalDirection.Forward);
        //        beforeStart.Freeze();

        //        // Raise ContentAffected event that spans entire TextElement (from BeforeStart to AfterEnd).
        //        tree.BeginChange();
        //        try
        //        {
        //            tree.BeforeAddChange();
        //            if (localValueChanged)
        //            {
        //                tree.AddLocalValueChange();
        //            }
        //            tree.AddChange(beforeStart, _textElementNode.SymbolCount, _textElementNode.IMECharCount,
        //                PrecursorTextChangeType.PropertyModified, property, !affectsMeasureOrArrange);
        //        }
        //        finally
        //        {
        //            tree.EndChange();
        //        }
        //    }
        //}

        #endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //internal static TypographyProperties GetTypographyProperties(IAvaloniaObject element)
        //{
        //    TypographyProperties group = new TypographyProperties();

        //    group.SetStandardLigatures((bool) element.GetValue(Typography.StandardLigaturesProperty));
        //    group.SetContextualLigatures((bool) element.GetValue(Typography.ContextualLigaturesProperty));
        //    group.SetDiscretionaryLigatures((bool) element.GetValue(Typography.DiscretionaryLigaturesProperty));
        //    group.SetHistoricalLigatures((bool) element.GetValue(Typography.HistoricalLigaturesProperty));
        //    group.SetAnnotationAlternates((int) element.GetValue(Typography.AnnotationAlternatesProperty));
        //    group.SetContextualAlternates((bool) element.GetValue(Typography.ContextualAlternatesProperty));
        //    group.SetHistoricalForms((bool) element.GetValue(Typography.HistoricalFormsProperty));
        //    group.SetKerning((bool) element.GetValue(Typography.KerningProperty));
        //    group.SetCapitalSpacing((bool) element.GetValue(Typography.CapitalSpacingProperty));
        //    group.SetCaseSensitiveForms((bool) element.GetValue(Typography.CaseSensitiveFormsProperty));
        //    group.SetStylisticSet1((bool) element.GetValue(Typography.StylisticSet1Property));
        //    group.SetStylisticSet2((bool) element.GetValue(Typography.StylisticSet2Property));
        //    group.SetStylisticSet3((bool) element.GetValue(Typography.StylisticSet3Property));
        //    group.SetStylisticSet4((bool) element.GetValue(Typography.StylisticSet4Property));
        //    group.SetStylisticSet5((bool) element.GetValue(Typography.StylisticSet5Property));
        //    group.SetStylisticSet6((bool) element.GetValue(Typography.StylisticSet6Property));
        //    group.SetStylisticSet7((bool) element.GetValue(Typography.StylisticSet7Property));
        //    group.SetStylisticSet8((bool) element.GetValue(Typography.StylisticSet8Property));
        //    group.SetStylisticSet9((bool) element.GetValue(Typography.StylisticSet9Property));
        //    group.SetStylisticSet10((bool) element.GetValue(Typography.StylisticSet10Property));
        //    group.SetStylisticSet11((bool) element.GetValue(Typography.StylisticSet11Property));
        //    group.SetStylisticSet12((bool) element.GetValue(Typography.StylisticSet12Property));
        //    group.SetStylisticSet13((bool) element.GetValue(Typography.StylisticSet13Property));
        //    group.SetStylisticSet14((bool) element.GetValue(Typography.StylisticSet14Property));
        //    group.SetStylisticSet15((bool) element.GetValue(Typography.StylisticSet15Property));
        //    group.SetStylisticSet16((bool) element.GetValue(Typography.StylisticSet16Property));
        //    group.SetStylisticSet17((bool) element.GetValue(Typography.StylisticSet17Property));
        //    group.SetStylisticSet18((bool) element.GetValue(Typography.StylisticSet18Property));
        //    group.SetStylisticSet19((bool) element.GetValue(Typography.StylisticSet19Property));
        //    group.SetStylisticSet20((bool) element.GetValue(Typography.StylisticSet20Property));
        //    group.SetFraction((FontFraction) element.GetValue(Typography.FractionProperty));
        //    group.SetSlashedZero((bool) element.GetValue(Typography.SlashedZeroProperty));
        //    group.SetMathematicalGreek((bool) element.GetValue(Typography.MathematicalGreekProperty));
        //    group.SetEastAsianExpertForms((bool) element.GetValue(Typography.EastAsianExpertFormsProperty));
        //    group.SetVariants((FontVariants) element.GetValue(Typography.VariantsProperty));
        //    group.SetCapitals((FontCapitals) element.GetValue(Typography.CapitalsProperty));
        //    group.SetNumeralStyle((FontNumeralStyle) element.GetValue(Typography.NumeralStyleProperty));
        //    group.SetNumeralAlignment((FontNumeralAlignment) element.GetValue(Typography.NumeralAlignmentProperty));
        //    group.SetEastAsianWidths((FontEastAsianWidths) element.GetValue(Typography.EastAsianWidthsProperty));
        //    group.SetEastAsianLanguage((FontEastAsianLanguage) element.GetValue(Typography.EastAsianLanguageProperty));
        //    group.SetStandardSwashes((int) element.GetValue(Typography.StandardSwashesProperty));
        //    group.SetContextualSwashes((int) element.GetValue(Typography.ContextualSwashesProperty));
        //    group.SetStylisticAlternates((int) element.GetValue(Typography.StylisticAlternatesProperty));

        //    return group;
        //}

        // ........................................................................
        //
        // Helpers for Text Flow Initialization
        //
        // ........................................................................

        // Recursively calls EndInit for this element and for all its descendants
        internal void DeepEndInit()
        {
            if (!this.IsInitialized)
            {
                if (!this.IsEmpty)
                {
                    IEnumerator children = GetChildElementEnumerator();

                    while (children.MoveNext())
                    {
                        // child.Current could be FrameworkElement, FrameworkContentElement,
                        //  or anything else.  Only recursively call self for FE & FCE.
                        TextElement child = children.Current as TextElement;
                        if (child != null)
                        {
                            child.DeepEndInit();
                        }
                    }
                }

                // Mark the end of the initialization phase
                this.EndInit();
                Invariant.Assert(this.IsInitialized);
            }
        }

        // Returns the common TextElement ancestor of two TextElements.
        internal static TextElement GetCommonAncestor(TextElement element1, TextElement element2)
        {
            if (element1 != element2)
            {
                int depth1 = 0;
                int depth2 = 0;
                TextElement element;

                // Calculate the depths of each TextElement within the tree.
                for (element = element1; element.Parent is TextElement; element = (TextElement)element.Parent)
                {
                    depth1++;
                }
                for (element = element2; element.Parent is TextElement; element = (TextElement)element.Parent)
                {
                    depth2++;
                }

                // Then walk up until we reach an equal depth.

                while (depth1 > depth2 && element1 != element2)
                {
                    element1 = (TextElement)element1.Parent;
                    depth1--;
                }

                while (depth2 > depth1 && element1 != element2)
                {
                    element2 = (TextElement)element2.Parent;
                    depth2--;
                }

                // Then, if necessary, keep going up to the root looking for a match.
                while (element1 != element2)
                {
                    element1 = element1.Parent as TextElement;
                    element2 = element2.Parent as TextElement;
                }
            }

            Invariant.Assert(element1 == element2);
            return element1;
        }


        /// <summary>
        /// Derived classes override this method to get notified when a TextContainer
        /// change affects the text parented by this element.
        /// </summary>
        /// <remarks>
        /// If this TextElement is a Run, this function will be called whenever the text content 
        /// under this Run changes. If this TextElement is not a Run, this function will be called 
        /// most of the time its content changes, but not necessarily always.
        /// </remarks>
        internal virtual void OnTextUpdated()
        {
        }

        /// <summary>
        /// Derived classes override this method to get notified before TextContainer
        /// causes a logical tree change that affects this element.
        /// </summary>
        internal virtual void BeforeLogicalTreeChange()
        {
        }

        /// <summary>
        /// Derived classes override this method to get notified after TextContainer
        /// causes a logical tree change that affects this element.
        /// </summary>
        internal virtual void AfterLogicalTreeChange()
        {
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        //------------------------------------------------------
        // The TextContainer containing this TextElement.
        //------------------------------------------------------
        internal TextContainer TextContainer
        {
            get
            {
                return EnsureTextContainer();
            }
        }

        //------------------------------------------------------
        // Emptiness of an element
        //------------------------------------------------------
        internal bool IsEmpty
        {
            get
            {
                if (_textElementNode == null)
                    return true;

                return (_textElementNode.ContainedNode == null);
            }
        }

        //------------------------------------------------------
        // True if this TextElement is contained within a TextContainer.
        //------------------------------------------------------
        internal bool IsInTree
        {
            get
            {
                return _textElementNode != null;
            }
        }

        //------------------------------------------------------
        // Symbol offset of this.ElementStart.
        //------------------------------------------------------
        internal int ElementStartOffset
        {
            get
            {
                Invariant.Assert(this.IsInTree, "TextElement is not in any TextContainer, caller should ensure this.");
                return _textElementNode.GetSymbolOffset(EnsureTextContainer().Generation) - 1;
            }
        }

        //------------------------------------------------------
        // Symbol offset of this.ContentStart.
        //------------------------------------------------------
        internal int ContentStartOffset
        {
            get
            {
                Invariant.Assert(this.IsInTree, "TextElement is not in any TextContainer, caller should ensure this.");
                return _textElementNode.GetSymbolOffset(EnsureTextContainer().Generation);
            }
        }

        //------------------------------------------------------
        // Symbol offset of this.ContentEnd.
        //------------------------------------------------------
        internal int ContentEndOffset
        {
            get
            {
                Invariant.Assert(this.IsInTree, "TextElement is not in any TextContainer, caller should ensure this.");
                return _textElementNode.GetSymbolOffset(EnsureTextContainer().Generation) + _textElementNode.SymbolCount - 2;
            }
        }

        //------------------------------------------------------
        // Symbol offset of this.ElementEnd.
        //------------------------------------------------------
        internal int ElementEndOffset
        {
            get
            {
                Invariant.Assert(this.IsInTree, "TextElement is not in any TextContainer, caller should ensure this.");
                return _textElementNode.GetSymbolOffset(EnsureTextContainer().Generation) + _textElementNode.SymbolCount - 1;
            }
        }

        //------------------------------------------------------
        // Symbol count of this TextElement, including start/end
        // edges.
        //------------------------------------------------------
        internal int SymbolCount
        {
            get
            {
                return this.IsInTree ? _textElementNode.SymbolCount : 2;
            }
        }

        //------------------------------------------------------
        // The node in a TextContainer representing this TextElement.
        //------------------------------------------------------
        internal TextTreeTextElementNode TextElementNode
        {
            get
            {
                return _textElementNode;
            }

            set
            {
                _textElementNode = value;
            }
        }

        //-------------------------------------------------------------------
        // Typography properties group
        //-------------------------------------------------------------------
        //internal TypographyProperties TypographyPropertiesGroup
        //{
        //    get
        //    {
        //        if (_typographyPropertiesGroup == null)
        //        {
        //            _typographyPropertiesGroup = GetTypographyProperties(this);
        //        }
        //        return _typographyPropertiesGroup;
        //    }
        //}

        //private static void OnTypographyChanged(IAvaloniaObject element, AvaloniaPropertyChangedEventArgs e)
        //{
        //    ((TextElement) element)._typographyPropertiesGroup = null;
        //}

        //------------------------------------------------------
        // Derived classes override this method if they want
        // their left edges to be visible to IMEs.  This is the
        // case for structural elements like Paragraph but not
        // for formatting elements like Inline.
        //------------------------------------------------------
        internal virtual bool IsIMEStructuralElement
        {
            get
            {
                return false;
            }
        }

        //------------------------------------------------------
        // Plain text character count of this element's edges.
        // Used by the IME to convert Paragraph, TableCell, etc.
        // into unicode placeholders.
        //------------------------------------------------------
        internal int IMELeftEdgeCharCount
        {
            get
            {
                int leftEdgeCharCount = 0;

                if (this.IsIMEStructuralElement)
                {
                    if (!this.IsInTree)
                    {
                        // IMELeftEdgeCharCount depends on context, has no meaning outside a tree.
                        leftEdgeCharCount = -1;
                    }
                    else
                    {
                        // The first sibling is always invisible to the IME.
                        // This ensures we don't get into trouble creating implicit
                        // content on IME SetText calls.
                        leftEdgeCharCount = this.TextElementNode.IsFirstSibling ? 0 : 1;
                    }
                }

                return leftEdgeCharCount;
            }
        }

        //------------------------------------------------------
        // Returns true if this node is the leftmost sibling of its parent
        // and visible to the IMEs (ie, is a Block).
        //
        // This is interesting because when we do want to expose
        // element edges to the IMEs (Blocks, TableCell, etc.) we
        // have one exception: the first sibling.  Edges of first
        // siblings must be hidden because the TextEditor will
        // implicitly create first siblings when the IMEs, for example,
        // insert raw text into a TableCell that lacks a Paragraph.
        // The IMEs can't handle the implicit edge creation, so we
        // hide those edges.
        //------------------------------------------------------
        internal virtual bool IsFirstIMEVisibleSibling
        {
            get
            {
                bool isFirstIMEVisibleSibling = false;

                if (this.IsIMEStructuralElement)
                {
                    isFirstIMEVisibleSibling = (this.TextElementNode == null) ? true : this.TextElementNode.IsFirstSibling;
                }

                return isFirstIMEVisibleSibling;
            }
        }

        //------------------------------------------------------
        // Returns a TextElement immediately following this one
        // on the same level of siblings.
        //------------------------------------------------------
        internal TextElement NextElement
        {
            get
            {
                if (!this.IsInTree)
                {
                    return null;
                }

                TextTreeTextElementNode node = _textElementNode.GetNextNode() as TextTreeTextElementNode;
                return (node != null) ? node.TextElement : null;
            }
        }

        //------------------------------------------------------
        // Returns a TextElement immediately preceding this one
        // on the same level of siblings.
        //------------------------------------------------------
        internal TextElement PreviousElement
        {
            get
            {
                if (!this.IsInTree)
                {
                    return null;
                }

                TextTreeTextElementNode node = _textElementNode.GetPreviousNode() as TextTreeTextElementNode;
                return (node != null) ? node.TextElement : null;
            }
        }

        //------------------------------------------------------
        // Returns the first TextElement contained by this
        // TextElement.
        //------------------------------------------------------
        internal TextElement FirstChildElement
        {
            get
            {
                if (!this.IsInTree)
                {
                    return null;
                }

                TextTreeTextElementNode node = _textElementNode.GetFirstContainedNode() as TextTreeTextElementNode;
                return (node != null) ? node.TextElement : null;
            }
        }

        //------------------------------------------------------
        // Returns the last TextElement contained by this
        // TextElement.
        //------------------------------------------------------
        internal TextElement LastChildElement
        {
            get
            {
                if (!this.IsInTree)
                {
                    return null;
                }

                TextTreeTextElementNode node = _textElementNode.GetLastContainedNode() as TextTreeTextElementNode;
                return (node != null) ? node.TextElement : null;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Inserts a string at the end of the content spanned by this TextElement.
        /// </summary>
        /// <param name="textData">
        /// string to insert.
        /// </param>
        private void Append(string textData)
        {
            TextContainer tree;

            if (textData == null)
            {
                throw new ArgumentNullException("textData");
            }

            tree = EnsureTextContainer();

            tree.BeginChange();
            try
            {
                // don't allocate a TextPointer, we shouldn't have to.
                // Change InsertTextInternal to take a node/edge pair instead.
                tree.InsertTextInternal(new TextPointer(tree, _textElementNode, ElementEdge.BeforeEnd), textData);
            }
            finally
            {
                tree.EndChange();
            }
        }

        /// <summary>
        /// Inserts a TextElement at the end of the content spanned by this
        /// TextElement.
        /// </summary>
        /// <param name="element">
        /// TextElement to insert.
        /// </param>
        /// <Remarks>
        /// This method will remove element from TextContainer it was previously
        /// positioned within.  Any content spanned by element will also
        /// be moved.
        /// </Remarks>
        private void Append(TextElement element)
        {
            TextContainer tree;
            TextPointer position;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            tree = EnsureTextContainer();

            tree.BeginChange();
            try
            {
                // don't allocate a TextPointer, we shouldn't have to.
                // Change InsertElementInternal to take a node/edge pair instead.
                position = new TextPointer(tree, _textElementNode, ElementEdge.BeforeEnd);
                tree.InsertElementInternal(position, position, element);
            }
            finally
            {
                tree.EndChange();
            }
        }

        // Demand creates a TextContainer if no tree is associated with this instance.
        // Otherwise returns the exisiting tree, and clears the tree's DeadPositionList.
        private TextContainer EnsureTextContainer()
        {
            TextContainer tree;
            TextPointer start;

            if (this.IsInTree)
            {
                tree = _textElementNode.GetTextTree();
                tree.EmptyDeadPositionList();
            }
            else
            {
                tree = new TextContainer(null, false /* plainTextOnly */);
                start = tree.Start;

                tree.BeginChange();
                try
                {
                    tree.InsertElementInternal(start, start, this);
                }
                finally
                {
                    // No event will be raised, since we know there are no listeners yet!
                    tree.EndChange();
                }

                Invariant.Assert(this.IsInTree);
            }

            return tree;
        }

        private static bool IsValidFontFamily(object o)
        {
            FontFamily value = o as FontFamily;
            return (value != null);
        }

        ///// <summary>
        ///// <see cref="AvaloniaProperty.ValidateValueCallback"/>
        ///// </summary>
        //private static bool IsValidFontSize(object value)
        //{
        //    double fontSize = (double) value;
        //    double minFontSize = TextDpi.MinWidth;
        //    double maxFontSize = Math.Min(1000000, PTS.MaxFontSize);

        //    if (Double.IsNaN(fontSize))
        //    {
        //        return false;
        //    }
        //    if (fontSize < minFontSize)
        //    {
        //        return false;
        //    }
        //    if (fontSize > maxFontSize)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The node in a TextContainer representing this TextElement.
        private TextTreeTextElementNode _textElementNode;

        //-------------------------------------------------------------------
        // Typography Group Property
        //-------------------------------------------------------------------
        //private TypographyProperties _typographyPropertiesGroup = Typography.Default;

        #endregion Private Fields
    }
}
