using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using MS.Internal;
using MS.Internal.Documents;
using MS.Internal.Text;
using Line = MS.Internal.Text.Line;
using TextRange = System.Windows.Documents.TextRange;

namespace Avalonia.Documents.Internal
{

    /// <summary>
    /// TextBlockCache caches the properties and Line which can be
    /// reused during Measure, Arrange and Render phase.
    /// </summary>
    ///
    class TextBlockCache
    {
        public LineProperties _lineProperties;
    }

    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class NewTextBlock : Control
    {
        //HACK: We don't have support for RenderSize for now
        internal Size RenderSize => new Size(Bounds.Width, Bounds.Height);

        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<NewTextBlock>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<NewTextBlock>();

        // TODO: Define these attached properties elsewhere (e.g. on a Text class) and AddOwner
        // them into NewTextBlock.

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<NewTextBlock, Control, FontFamily>(
                nameof(FontFamily),
                defaultValue: FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<NewTextBlock, Control, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<NewTextBlock, Control, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<NewTextBlock, Control, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<NewTextBlock, Control, IBrush>(
                nameof(Foreground),
                Brushes.Black,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            AvaloniaProperty.Register<NewTextBlock, double>(
                nameof(LineHeight),
                double.NaN,
                validate: IsValidLineHeight);

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxLinesProperty =
            AvaloniaProperty.Register<NewTextBlock, int>(
                nameof(MaxLines),
                validate: IsValidMaxLines);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<NewTextBlock, string> TextProperty =
            AvaloniaProperty.RegisterDirect<NewTextBlock, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.Register<NewTextBlock, TextAlignment>(nameof(TextAlignment));

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.Register<NewTextBlock, TextWrapping>(nameof(TextWrapping));

        /// <summary>
        /// Defines the <see cref="TextTrimming"/> property.
        /// </summary>
        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.Register<NewTextBlock, TextTrimming>(nameof(TextTrimming));

        /// <summary>
        /// Defines the <see cref="TextDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection> TextDecorationsProperty =
            AvaloniaProperty.Register<NewTextBlock, TextDecorationCollection>(nameof(TextDecorations));

        /// <summary>
        /// Defines the <see cref="BaselineOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BaselineOffsetProperty =
            AvaloniaProperty.Register<NewTextBlock, double>(nameof(BaselineOffset), double.NaN,
                notifying: OnBaselineOffsetChanged);

        public FlowDirection FlowDirection
        {
            get => GetValue(Inline.FlowDirectionProperty);
            set => SetValue(Inline.FlowDirectionProperty, value);
        }

        private string _text;
        private TextLayout _textLayout;
        private Size _constraint;

        /// <summary>
        /// Initializes static members of the <see cref="NewTextBlock"/> class.
        /// </summary>
        static NewTextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<NewTextBlock>(true);

            AffectsRender<NewTextBlock>(BackgroundProperty, ForegroundProperty,
                TextAlignmentProperty, TextDecorationsProperty);

            AffectsMeasure<NewTextBlock>(FontSizeProperty, FontWeightProperty,
                FontStyleProperty, TextWrappingProperty, FontFamilyProperty,
                TextTrimmingProperty, TextProperty, PaddingProperty, LineHeightProperty, MaxLinesProperty);

            Observable.Merge<AvaloniaPropertyChangedEventArgs>(TextProperty.Changed, ForegroundProperty.Changed,
                TextAlignmentProperty.Changed, TextWrappingProperty.Changed,
                TextTrimmingProperty.Changed, FontSizeProperty.Changed,
                FontStyleProperty.Changed, FontWeightProperty.Changed,
                FontFamilyProperty.Changed, TextDecorationsProperty.Changed,
                PaddingProperty.Changed, MaxLinesProperty.Changed, LineHeightProperty.Changed
            ).AddClassHandler<NewTextBlock>((x, _) => x.InvalidateTextLayout());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewTextBlock"/> class.
        /// </summary>
        public NewTextBlock()
        {
            _text = string.Empty;
        }

        /// <summary>
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout
        {
            get
            {
                return _textLayout ?? (_textLayout = CreateTextLayout(_constraint, Text));
            }
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Text"/>.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of each line of content.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of text lines.
        /// </summary>
        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control's text trimming mode.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text decorations.
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get => GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        public double BaselineOffset
        {
            get => GetValue(BaselineOffsetProperty);
            set => SetValue(BaselineOffsetProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFamily GetFontFamily(Control control)
        {
            return control.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static double GetFontSize(Control control)
        {
            return control.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontStyle GetFontStyle(Control control)
        {
            return control.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontWeight GetFontWeight(Control control)
        {
            return control.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The foreground.</returns>
        public static IBrush GetForeground(Control control)
        {
            return control.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontFamily(Control control, FontFamily value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns>The font family.</returns>
        public static void SetForeground(Control control, IBrush value)
        {
            control.SetValue(ForegroundProperty, value);
        }

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of Inline items contained in this TextBlock.
        /// </value>
        [Content]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public InlineCollection Inlines
        {
            get
            {
                return new InlineCollection(this, /*isOwnerParent*/true);
            }
        }

        /// <summary>
        /// TextPointer preceding all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection property set to LogicalDirection.Backward.
        /// </remarks>
        public TextPointer ContentStart
        {
            get
            {
                EnsureComplexContent();

                return (TextPointer)_complexContent.TextContainer.Start;
            }
        }

        /// <summary>
        /// TextPointer following all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true
        /// and LogicalDirection property set to LogicalDirection.Forward.
        /// </remarks>
        public TextPointer ContentEnd
        {
            get
            {
                EnsureComplexContent();

                return (TextPointer)_complexContent.TextContainer.End;
            }
        }

        /// <value>
        /// A TextRange spanning the content of this element.
        /// </value>
        internal TextRange TextRange
        {
            get
            {
                // NOTE: We are creating a new instance of a TextRange on each request.
                // We cannot cache the instance, because it may become incorrect
                // after collapsing TextBlock's content and insertion a new one:
                // the cached range would remain empty, which is incorrect.
                return new TextRange(this.ContentStart, this.ContentEnd);
            }
        }

        /// <summary>
        /// Breaking condition before the Element.
        /// </summary>
        public LineBreakCondition BreakBefore { get { return LineBreakCondition.BreakDesired; } }

        /// <summary>
        /// Breaking condition after the Element.
        /// </summary>
        public LineBreakCondition BreakAfter { get { return LineBreakCondition.BreakDesired; } }

        /// <summary>
        /// Access to all text typography properties.
        /// </summary>
        // TODO public Typography Typography
        // TODO {
        // TODO     get
        // TODO     {
        // TODO         return new Typography(this);
        // TODO     }
        // TODO }

        #endregion Public Properties

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout(Size constraint, string text)
        {
            if (constraint == Size.Empty)
            {
                return null;
            }

            return new TextLayout(
                text ?? string.Empty,
                new Typeface(FontFamily, FontStyle, FontWeight),
                FontSize,
                Foreground,
                TextAlignment,
                TextWrapping,
                TextTrimming,
                TextDecorations,
                constraint.Width,
                constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight);
        }

        /// <summary>
        /// Invalidates <see cref="TextLayout"/>.
        /// </summary>
        protected void InvalidateTextLayout()
        {
            _textLayout = null;
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            InvalidateTextLayout();

            InvalidateMeasure();
        }

        private static bool IsValidMaxLines(int maxLines) => maxLines >= 0;

        private static bool IsValidLineHeight(double lineHeight) => double.IsNaN(lineHeight) || lineHeight > 0;

        /// <value>
        /// Collection of Blocks contained in this element
        /// </value>
        // TODO [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // TODO public BlockCollection Blocks
        // TODO {
        // TODO     get
        // TODO     {
        // TODO         return new BlockCollection(this, /*isOwnerParent*/true);
        // TODO     }
        // TODO }


        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        /// <returns>Computed desired size.</returns>
        protected sealed override Size MeasureOverride(Size constraint)
        {
            VerifyReentrancy();

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif

            // Clear and repopulate our text block cache. (Handles multiple measure before arrange)
            _textBlockCache = null;

            EnsureTextBlockCache();
            LineProperties lineProperties = _textBlockCache._lineProperties;

            // Hook up our TextContainer event listeners if we haven't yet.
            if (CheckFlags(Flags.PendingTextContainerEventInit))
            {
                Invariant.Assert(_complexContent != null);
                InitializeTextContainerListeners();
                SetFlags(false, Flags.PendingTextContainerEventInit);
            }

            // Find out if we can skip measure process. Measure cannot be skipped in following situations:
            // a) content is dirty (properties or content)
            // b) there are inline objects (they may be dynamically sized)
            int lineCount = LineCount;
            if ((lineCount > 0) && IsMeasureValid && InlineObjects == null)
            {
                // Assuming that all of above conditions are true, Measure can be
                // skipped in following situations:
                // 1) TextTrimming == None and:
                //      a) Width is the same, or
                //      b) TextWrapping == NoWrap
                // 2) Width is the same and TextWrapping == NoWrap
                bool bypassMeasure;
                if (lineProperties.TextTrimming == TextTrimming.None)
                {
                    bypassMeasure = MathUtilities.AreClose(constraint.Width, _referenceSize.Width) || (lineProperties.TextWrapping == TextWrapping.NoWrap);
                }
                else
                {
                    bypassMeasure =
                        MathUtilities.AreClose(constraint.Width, _referenceSize.Width) &&
                        (lineProperties.TextWrapping == TextWrapping.NoWrap) &&
                        (MathUtilities.AreClose(constraint.Height, _referenceSize.Height) || lineCount == 1);
                }
                if (bypassMeasure)
                {
                    _referenceSize = constraint;
#if TEXTPANELLAYOUTDEBUG
                    MS.Internal.PtsHost.TextPanelDebug.Log("MeasureOverride bypassed.", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
                    MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
                    MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
                    return _previousDesiredSize;
                }
            }

            // Store constraint size, it is used when measuring inline objects.
            _referenceSize = constraint;

            // Store previous ITextEmbeddable values
            bool formattedOnce = CheckFlags(Flags.FormattedOnce);
            double baselineOffsetPrevious = _baselineOffset;

            // Reset inline objects cache and line metrics cache.
            // They will be fully updated during lines formatting.
            InlineObjects = null;

            // before erasing the line metrics, keep track of how big it was last time
            // so that we can initialize the metrics array to that size this time
            int subsequentLinesInitialSize = (_subsequentLines == null) ? 1 : _subsequentLines.Count;

            ClearLineMetrics();

            if (_complexContent != null)
            {
                _complexContent.TextView.Invalidate();
            }

            // To determine natural size of the text TextAlignment has to be ignored.
            // Since for rendering/hittesting lines are recreated, it can be done without
            // any problems.
            lineProperties.IgnoreTextAlignment = true;
            SetFlags(true, Flags.RequiresAlignment); // Need to update LineMetrics.Start when FinalSize is known.
            SetFlags(true, Flags.FormattedOnce);
            SetFlags(false, Flags.HasParagraphEllipses);
            SetFlags(true, Flags.MeasureInProgress | Flags.TreeInReadOnlyMode);
            var desiredSizeWidth = 0.0;
            var desiredSizeHeight = 0.0;
            bool exceptionThrown = true;
            try
            {
                // Create and format lines until end of paragraph is reached.
                // Since we are disposing line object, it can be reused to format following lines.
                Line line = CreateLine(lineProperties);
                bool endOfParagraph = false;
                int dcp = 0;
                TextLineBreak textLineBreakIn = null;

                Thickness padding = this.Padding;
                Size contentSize = new Size(Math.Max(0.0, constraint.Width - (padding.Left + padding.Right)),
                                            Math.Max(0.0, constraint.Height - (padding.Top + padding.Bottom)));

                while (!endOfParagraph)
                {
                    using(line)
                    {
                        // Format line. Set showParagraphEllipsis flag to false because we do not know whether or not the line will have
                        // paragraph ellipsis at this time. Since TextBlock is auto-sized we do not know the RenderSize until we finish Measure
                        line.Format(dcp, contentSize.Width, GetLineProperties(dcp == 0, lineProperties), textLineBreakIn, /*Show paragraph ellipsis*/ false);

                        double lineHeight = CalcLineAdvance(line.Height, lineProperties);

    #if DEBUG
                        LineMetrics metrics = new LineMetrics(contentSize.Width, line.Length, line.Width, lineHeight, line.BaselineOffset, line.HasInlineObjects(), textLineBreakIn);
    #else
                        LineMetrics metrics = new LineMetrics(line.Length, line.Width, lineHeight, line.BaselineOffset, line.HasInlineObjects(), textLineBreakIn);
    #endif

                        if (!CheckFlags(Flags.HasFirstLine))
                        {
                            SetFlags(true, Flags.HasFirstLine);
                            _firstLine = metrics;
                        }
                        else
                        {
                            if (_subsequentLines == null)
                            {
                                _subsequentLines = new List<LineMetrics>(subsequentLinesInitialSize);
                            }
                            _subsequentLines.Add(metrics);
                        }


                        // Desired width is always max of calculated line widths.
                        // Desired height is sum of all line heights. But if TextTrimming is on
                        // do not overflow the requested height with the exception for the first line.
                        desiredSizeWidth = Math.Max(desiredSizeWidth, line.GetCollapsedWidth());
                        if ((lineProperties.TextTrimming == TextTrimming.None) ||
                            (contentSize.Height >= (desiredSizeHeight + lineHeight)) ||
                            (dcp == 0))
                        {
                            // BaselineOffset is always distance from the Text's top
                            // to the baseline offset of the last line.
                            _baselineOffset = desiredSizeHeight + line.BaselineOffset;

                            desiredSizeHeight += lineHeight;
                        }
                        else
                        {
                            // Note the fact that there are paragraph ellipses
                            SetFlags(true, Flags.HasParagraphEllipses);
                        }

                        textLineBreakIn = line.GetTextLineBreak();

                        endOfParagraph = line.EndOfParagraph;
                        dcp += line.Length;

                        // don't wrap a line that was artificially broken because of excessive length
                        // TODO I don't believe Avalonia has a concept of force-breaking lines
                        // TODO if (!endOfParagraph &&
                        // TODO     lineProperties.TextWrapping == TextWrapping.NoWrap &&
                        // TODO     line.Length == TextStore.MaxCharactersPerLine)
                        // TODO {
                        // TODO     endOfParagraph = true;
                        // TODO }
                    }
                }

                desiredSizeWidth += (padding.Left + padding.Right);
                desiredSizeHeight += (padding.Top + padding.Bottom);

                Invariant.Assert(textLineBreakIn == null); // End of paragraph should have no line break record

                exceptionThrown = false;
            }
            finally
            {
                // Restore original line properties
                lineProperties.IgnoreTextAlignment = false;
                SetFlags(false, Flags.MeasureInProgress | Flags.TreeInReadOnlyMode);

                if(exceptionThrown)
                {
                    ClearLineMetrics();
                }
            }

            // Notify ITextHost that ITextEmbeddable values have been changed, if necessary.
            if (!MathUtilities.AreClose(baselineOffsetPrevious, _baselineOffset))
            {
                CoerceValue(BaselineOffsetProperty);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.MeasureOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            _previousDesiredSize = new Size(desiredSizeWidth, desiredSizeHeight);

            return _previousDesiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        protected sealed override Size ArrangeOverride(Size arrangeSize)
        {
            VerifyReentrancy();

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.BeginScope("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            // Remove all existing visuals. If there are inline objects, they will be added below.
            if (_complexContent != null)
            {
                _complexContent.VisualChildren.Clear();
            }

            ArrayList inlineObjects = InlineObjects;
            int lineCount = LineCount;
            if (inlineObjects != null && lineCount > 0)
            {
                bool exceptionThrown = true;

                SetFlags(true, Flags.TreeInReadOnlyMode);
                SetFlags(true, Flags.ArrangeInProgress);

                try
                {
                    EnsureTextBlockCache();
                    LineProperties lineProperties = _textBlockCache._lineProperties;

                    double wrappingWidth = CalcWrappingWidth(arrangeSize.Width);
                    Vector contentOffset = CalcContentOffset(arrangeSize, wrappingWidth);

                    // Position all inline objects. Recreate only lines that have inline objects
                    // and call arrange on it. Line.Arrange enumerates all inline objects and
                    // sets appropriate transform on them.
                    Line line = CreateLine(lineProperties);
                    int dcp = 0;
                    Vector lineOffset = contentOffset;

                    for (int i = 0; i < lineCount; i++)
                    {
Debug.Assert(lineCount == LineCount);
                        LineMetrics lineMetrics = GetLine(i);

                        if (lineMetrics.HasInlineObjects)
                        {
                            using (line)
                            {
                                // Check if paragraph ellipsis are added to this line
                                bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset.Y - contentOffset.Y);
                                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);

                                // Check that lineMetrics length and line length are in sync
                                // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                                // MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                                // We shut off text alignment for measure, ensure we treat same here.
                                // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                                //if(lineProperties.TextAlignment != TextAlignment.Justify)
                                //{
                                //    Debug.Assert(MathUtilities.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line formatting is not consistent.");
                                //}
                                // Calculated line width might be different from measure width in following cases:
                                // a) dynamically sized children, when FinalSize != AvailableSize
                                // b) non-default horizontal alignment, when FinalSize != AvailableSize
                                // Hence do not assert about matching line width with cached line metrics.

                                // Add inline objects to visual children of the TextBlock visual and
                                // set appropriate transforms.
                                line.Arrange(_complexContent.VisualChildren, lineOffset);
                            }
                        }

                        lineOffset = lineOffset.WithY(lineOffset.Y + lineMetrics.Height);
                        dcp += lineMetrics.Length;
                    }

                    exceptionThrown = false;
                }
                finally
                {
                    SetFlags(false, Flags.TreeInReadOnlyMode);
                    SetFlags(false, Flags.ArrangeInProgress);
                    if(exceptionThrown)
                    {
                       ClearLineMetrics();
                    }
                }
            }

            if (_complexContent != null)
            {
                Dispatcher.UIThread.Post(OnValidateTextView);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("TextBlock.ArrangeOverride", MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
            MS.Internal.PtsHost.TextPanelDebug.EndScope(MS.Internal.PtsHost.TextPanelDebug.Category.MeasureArrange);
#endif
            InvalidateVisual();
            return arrangeSize;
        }

        /// <summary>
        /// Render control's content.
        /// </summary>
        /// <param name="ctx">Drawing context.</param>
        public sealed override void Render(DrawingContext ctx)
        {
            VerifyReentrancy();

            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            // If layout data is not updated do not render the content.
            if (!IsLayoutDataValid) { return; }

            // Draw background in rectangle.
            var background = this.Background;
            if (background != null)
            {
                ctx.DrawRectangle(background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }

            SetFlags(false, Flags.RequiresAlignment);
            SetFlags(true, Flags.TreeInReadOnlyMode);
            try
            {
                // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
                EnsureTextBlockCache();
                LineProperties lineProperties = _textBlockCache._lineProperties;


                double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
                Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
                Point lineOffset = new Point(contentOffset.X, contentOffset.Y);

                // NOTE: All inline objects are UIElements and all of them are direct children of
                // the TextBlock. Hence visuals for those inline objects are already attached.
                // The only responsibility of OnRender is to render text, since transforms for inline
                // objects are set during OnArrange.

                // Create / format / render all lines.
                // Since we are disposing line object, it can be reused to format following lines.
                Line line = CreateLine(lineProperties);
                int dcp = 0;
                bool showParagraphEllipsis = false;
                SetFlags(CheckFlags(Flags.HasParagraphEllipses), Flags.RequiresAlignment);


                int lineCount = LineCount;
                for (int i = 0; i < lineCount; i++)
                {
Debug.Assert(lineCount == LineCount);
                    LineMetrics lineMetrics = GetLine(i);
                    double contentBottom = Math.Max(0.0, RenderSize.Height - Padding.Bottom);

                    // Find out if this is the last rendered line
                    if (CheckFlags(Flags.HasParagraphEllipses))
                    {
                        if (i + 1 < lineCount)
                        {
                            // Calculate bottom offset for next line
                            double nextLineBottomOffset = GetLine(i + 1).Height + lineMetrics.Height + lineOffset.Y;

                            // If the next line will exceed render height by a large margin, we cannot render
                            // it at all and so we should show ellipsis on this one. However if the next line
                            // almost fits, we will render it and so there should be no ellipsis
                            showParagraphEllipsis = MathUtilities.GreaterThan(nextLineBottomOffset, contentBottom) && !MathUtilities.AreClose(nextLineBottomOffset, contentBottom);
                        }
                    }

                    // If paragraph ellipsis are enabled, do not render lines that
                    // extend computed layout size. But if the first line does not fit completely,
                    // render it anyway.
                    if (!CheckFlags(Flags.HasParagraphEllipses) ||
                        (MathUtilities.LessThanOrClose(lineMetrics.Height + lineOffset.Y, contentBottom) || i == 0))
                    {
                        using (line)
                        {
                            Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, showParagraphEllipsis, lineProperties), lineMetrics.TextLineBreak, showParagraphEllipsis);

                            // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                            //if (!showParagraphEllipsis)
                            //{
                            //    // Check consistency of line formatting
                            //    Debug.Assert(line.Length == lineMetrics.Length, "Line length is out of sync");
                            //    // We shut off text alignment for measure, ensure we treat same here.
                            //    if(lineProperties.TextAlignment != TextAlignment.Justify)
                            //    {
                            //        Debug.Assert(MathUtilities.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");
                            //    }
                            //    // Calculated line width might be different from measure width in following cases:
                            //    // a) dynamically sized children, when FinalSize != AvailableSize
                            //    // b) non-default horizontal alignment, when FinalSize != AvailableSize
                            //    // Hence do not assert about matching line width with cached line metrics.
                            //}
                            if (!CheckFlags(Flags.HasParagraphEllipses))
                            {
                                lineMetrics = UpdateLine(i, lineMetrics, line.Start, line.Width);
                            }

                            line.Render(ctx, lineOffset);

                            lineOffset = lineOffset.WithY(lineOffset.Y + lineMetrics.Height);
                            dcp += lineMetrics.Length;
                        }
                    }
                }
            }
            finally
            {
                SetFlags(false, Flags.TreeInReadOnlyMode);
                _textBlockCache = null;
            }
        }

        /// <summary>
        /// Notification that a specified property has been invalidated
        /// </summary>
        /// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TextProperty)
            {
                OnTextChanged(Text);
            }

            // TODO Check for typography properties
            // TODO SetFlags(true, Flags.IsTypographySet);

            if (change.IsEffectiveValueChange)
            {
                if (CheckFlags(Flags.FormattedOnce))
                {
                    var fmetadata = change.Property;
                    if (fmetadata != null)
                    {
                        var affectsRender = fmetadata.CanValueAffectRender();

                        // TODO: Was previously checking for measure/arrange invalidation, might instead use specific events for that
                        if (affectsRender)
                        {
                            // Will throw an exception, if during measure/arrange/render process.
                            VerifyTreeIsUnlocked();

                            // TextRunCache stores properties for every single run fetched so far.
                            // If there are any property changes, which affect measure, arrange or
                            // render, invalidate TextRunCache. It will force TextFormatter to refetch
                            // runs and properties.
                           // _lineProperties = null;
                            _textBlockCache = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hit tests to the correct ContentElement within the ContentHost
        /// that the mouse is over.
        /// </summary>
        /// <param name="point">Mouse coordinates relative to the ContentHost.</param>
        protected virtual IInputElement InputHitTestCore(Point point)
        {
            // If layout data is not updated return 'this'.
            if (!IsLayoutDataValid) { return this; }

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            // If there is only one line and it is already cached, use it to do hit-testing.
            // Otherwise, do following:
            // a) use cached line information to find which line has been hit,
            // b) re-create the line that has been hit,
            // c) hit-test the line.
            IInputElement ie = null;
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            point -= contentOffset; // // Take into account content offset.

            if (point.X < 0 || point.Y < 0) return this;

            ie = null;
            int dcp = 0;
            double lineOffset = 0;

            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                if (lineOffset + lineMetrics.Height > point.Y)
                {
                    // The current line has been hit. Format the line and
                    // retrieve IInputElement from the hit position.
                    Line line = CreateLine(lineProperties);
                    using (line)
                    {
                        // Check if paragraph ellipsis are rendered
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);

                        // Verify consistency of line formatting
                        // Check that lineMetrics.Length is in sync with line.Length
                        // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                        //MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync:");

                        // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                        //Debug.Assert(MathUtilities.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");
                        // Calculated line width might be different from measure width in following cases:
                        // a) dynamically sized children, when FinalSize != AvailableSize
                        // b) non-default horizontal alignment, when FinalSize != AvailableSize
                        // Hence do not assert about matching line width with cached line metrics.

                        if ((line.Start <= point.X) && (line.Start + line.Width >= point.X))
                        {
                            ie = line.InputHitTest(point.X);
                        }
                    }
                    break; // Line covering the point has been found; no need to continue.
                }

                dcp += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

            // If nothing has been hit, assume that element itself has been hit.
            return (ie != null) ? ie : this;
        }

        /// <summary>
        /// Returns an ICollection of bounding rectangles for the given ContentElement
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        /// <remarks>
        /// Looks at the ContentElement e line by line and gets rectangle bounds for each line
        /// </remarks>
        protected virtual ReadOnlyCollection<Rect> GetRectanglesCore(StyledElement child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            // If layout data is not updated we assume that we will not be able to find the element we need and throw excception
            if (!IsLayoutDataValid)
            {
                // return empty collection
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            // Check for complex content
            if (_complexContent == null || !(_complexContent.TextContainer is TextContainer))
            {
                // return empty collection
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            // First find the element start and end position
            TextPointer start = FindElementPosition((IInputElement)child);
            if (start == null)
            {
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            TextPointer end = null;
            if (child is TextElement)
            {
                end = new TextPointer(((TextElement)child).ElementEnd);
            }
            // TODO else if (child is FrameworkContentElement)
            // TODO {
            // TODO     end = new TextPointer(start);
            // TODO     end.MoveByOffset(+1);
            // TODO }

            if (end == null)
            {
                return new ReadOnlyCollection<Rect>(new List<Rect>(0));
            }

            int startOffset = _complexContent.TextContainer.Start.GetOffsetToPosition(start);
            int endOffset = _complexContent.TextContainer.Start.GetOffsetToPosition(end);

            int lineIndex = 0;
            int lineOffset = 0;
            double lineHeightOffset = 0;
            int lineCount = LineCount;
            while (startOffset >= (lineOffset + GetLine(lineIndex).Length) && lineIndex < lineCount)
            {
Debug.Assert(lineCount == LineCount);
                lineOffset += GetLine(lineIndex).Length;
                lineIndex++;
                lineHeightOffset += GetLine(lineIndex).Height;
            }
            Debug.Assert(lineIndex < lineCount);

            int lineStart = lineOffset;
            List<Rect> rectangles = new List<Rect>();
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            do
            {
Debug.Assert(lineCount == LineCount);
                // Check that line index never exceeds line count
                Debug.Assert(lineIndex < lineCount);

                // Create lines as long as they are spanned by the element
                LineMetrics lineMetrics = GetLine(lineIndex);

                Line line = CreateLine(lineProperties);

                using (line)
                {
                    // Check if paragraph ellipsis are rendered
                    bool ellipsis = ParagraphEllipsisShownOnLine(lineIndex, lineOffset);
                    Format(line, lineMetrics.Length, lineStart, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);

                    // Verify consistency of line formatting
                    // Workaround for (Crash when mouse over a Button with TextBlock). Re-enable this assert when MIL Text issue is fixed.
                    if (lineMetrics.Length == line.Length)
                    {
                        //MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                        //Debug.Assert(MathUtilities.AreClose(CalcLineAdvance(line.Height, lineProperties), lineMetrics.Height), "Line height is out of sync.");

                        int boundStart = (startOffset >= lineStart) ? startOffset : lineStart;
                        int boundEnd = (endOffset < lineStart + lineMetrics.Length) ? endOffset : lineStart + lineMetrics.Length;

                        double xOffset = contentOffset.X;
                        double yOffset = contentOffset.Y + lineHeightOffset;
                        List<Rect> lineBounds = line.GetRangeBounds(boundStart, boundEnd - boundStart, xOffset, yOffset);
                        Debug.Assert(lineBounds.Count > 0);
                        rectangles.AddRange(lineBounds);
                    }
                }

                lineStart += lineMetrics.Length;
                lineHeightOffset += lineMetrics.Height;
                lineIndex++;
            }
            while (endOffset > lineStart);

            // Rectangles collection must be non-null
            Invariant.Assert(rectangles != null);
            return new ReadOnlyCollection<Rect>(rectangles);
        }
        
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods


        //-------------------------------------------------------------------
        // Measure child UIElement.
        //
        //      inlineObject - hosted inline object to measure.
        //
        // Returns: Size of the inline object.
        //-------------------------------------------------------------------
        internal Size MeasureChild(InlineObject inlineObject)
        {
            Debug.Assert(_complexContent != null, "Inline objects are supported only in complex content.");

            Size desiredSize;
            // Measure child only during measure pass. If not during measuring
            // use RenderSize.
            if (CheckFlags(Flags.MeasureInProgress))
            {
                // Measure inline objects. Original size constraint is passed,
                // because inline object size should not be dependent on position
                // inside a text line. It should not be also bigger than Text itself.
                Thickness padding = this.Padding;
                Size contentSize = new Size(Math.Max(0.0, _referenceSize.Width - (padding.Left + padding.Right)),
                                            Math.Max(0.0, _referenceSize.Height - (padding.Top + padding.Bottom)));
                inlineObject.Element.Measure(contentSize);
                desiredSize = inlineObject.Element.DesiredSize;

                // Store inline object in the cache.
                ArrayList inlineObjects = InlineObjects;
                bool alreadyCached = false;
                if (inlineObjects == null)
                {
                    InlineObjects = inlineObjects = new ArrayList(1);
                }
                else
                {
                    // Find out if inline object is already cached.
                    for (int index = 0; index < inlineObjects.Count; index++)
                    {
                        if (((InlineObject)inlineObjects[index]).Dcp == inlineObject.Dcp)
                        {
                            Debug.Assert(((InlineObject)inlineObjects[index]).Element == inlineObject.Element, "InlineObject cache is out of sync.");
                            alreadyCached = true;
                            break;
                        }
                    }
                }
                if (!alreadyCached)
                {
                    inlineObjects.Add(inlineObject);
                }
            }
            else
            {
                desiredSize = inlineObject.Element.DesiredSize;
            }
            return desiredSize;
        }

        /// <summary>
        /// Returns a new array of LineResults for the paragraph's lines.
        /// </summary>
        internal ReadOnlyCollection<LineResult> GetLineResults()
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.IncrementCounter("NewTextBlock.GetLines", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);

            // Proper line alignment has to be done bofore LineResults are created.
            // Owherwise Line.Start may have wrong value.
            if (CheckFlags(Flags.RequiresAlignment))
            {
                AlignContent();
            }

            // Calculate content offset.
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            // Create line results
            int lineCount = LineCount;
            List<LineResult> lines = new List<LineResult>(lineCount);
            int dcp = 0;
            double lineOffset = 0;
            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(lineIndex);

                Rect layoutBox = new Rect(contentOffset.X + lineMetrics.Start, contentOffset.Y + lineOffset, lineMetrics.Width, lineMetrics.Height);
                lines.Add(new TextLineResult(this, dcp, lineMetrics.Length, layoutBox, lineMetrics.Baseline, lineIndex));

                lineOffset += lineMetrics.Height;
                dcp += lineMetrics.Length;
            }
            return new ReadOnlyCollection<LineResult>(lines);
        }

        /// <summary>
        /// Retrieves detailed information about a line of text.
        /// </summary>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="index"> Index of the line</param>
        /// <param name="lineVOffset"> Vertical offset of the line</param>
        /// <param name="cchContent">Number of content characters in the line.</param>
        /// <param name="cchEllipses">Number of content characters hidden by ellipses.</param>
        internal void GetLineDetails(int dcp, int index, double lineVOffset, out int cchContent, out int cchEllipses)
        {
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(index >= 0 && index < LineCount);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            LineMetrics lineMetrics = GetLine(index);

            // Retrieve details from the line.
            using (Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false
                TextLineBreak textLineBreak = GetLine(index).TextLineBreak;
                bool ellipsis = ParagraphEllipsisShownOnLine(index, lineVOffset);
                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), textLineBreak, ellipsis);

                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                cchContent = line.ContentLength;
                cchEllipses = line.GetEllipsesLength();
            }
        }

        /// <summary>
        /// Retrieve text position from the distance (relative to the beginning
        /// of specified line).
        /// </summary>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="distance">Distance relative to the beginning of the line.</param>
        /// <param name="lineVOffset">
        /// Vertical offset of the line in which the position lies,
        /// </param>
        /// <param name="index">
        /// Index of the line
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the distance.
        /// </returns>
        internal ITextPointer GetTextPositionFromDistance(int dcp, double distance, double lineVOffset, int index)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("NewTextBlock.GetTextPositionFromDistance", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent(); // TextOM access requires complex content.

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);
            distance -= contentOffset.X;
            lineVOffset -= contentOffset.Y;

            LineMetrics lineMetrics = GetLine(index);
            ITextPointer pos;
            using (Line line = CreateLine(lineProperties))
            {
                MS.Internal.Invariant.Assert(index >= 0 && index < LineCount);
                TextLineBreak textLineBreak = GetLine(index).TextLineBreak;
                bool ellipsis = ParagraphEllipsisShownOnLine(index, lineVOffset);
                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), textLineBreak, ellipsis);

                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                CharacterHit charIndex = line.GetTextPositionFromDistance(distance);
                LogicalDirection logicalDirection;

                logicalDirection = (charIndex.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                pos = _complexContent.TextContainer.Start.CreatePointer(charIndex.FirstCharacterIndex + charIndex.TrailingLength, logicalDirection);
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("NewTextBlock.GetTextPositionFromDistance", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            return pos;
        }

        /// <summary>
        /// Retrieves bounds of an object/character at the specified TextPointer.
        /// Throws IndexOutOfRangeException if position is out of range.
        /// </summary>
        /// <param name="orientedPosition">Position of an object/character.</param>
        /// <returns>Bounds of an object/character.</returns>
        internal Rect GetRectangleFromTextPosition(ITextPointer orientedPosition)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("NewTextBlock.GetRectangleFromTextPosition", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(orientedPosition != null);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            // From TextFormatter get rectangle of a single character.
            // If orientation is Backward, get the length of th previous character.
            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(orientedPosition);
            int originalCharacterIndex = characterIndex;
            if (orientedPosition.LogicalDirection == LogicalDirection.Backward && characterIndex > 0)
            {
                --characterIndex;
            }

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            double lineOffset = 0;
            int dcp = 0;

            Rect rect = Rect.Empty;
            FlowDirection flowDirection = FlowDirection.LeftToRight;

            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
                Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                // characterIndex needs to be within line range. If position points to
                // dcp + line.Length, it means that the next line starts from such position,
                // hence go to the next line.
                // But if this is the last line (EOP character), get rectangle form the last
                // character of the line.
                if (dcp + lineMetrics.Length > characterIndex ||
                    ((dcp + lineMetrics.Length == characterIndex) && (i == lineCount - 1)))
                {
                    using (Line line = CreateLine(lineProperties))
                    {
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);

                        // Check consistency of line length
                        MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                        rect = line.GetBoundsFromTextPosition(characterIndex, out flowDirection);
                    }

                    break;
                }

                dcp += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

            if (!rect.IsEmpty) // Empty rects can't be modified
            {
                rect = rect.Translate(new Vector(contentOffset.X, contentOffset.Y + lineOffset));

                // Return only TopLeft and Height.
                // Adjust rect.Left by taking into account flow direction of the
                // content and orientation of input position.
                if (lineProperties.FlowDirection != flowDirection)
                {
                    if (orientedPosition.LogicalDirection == LogicalDirection.Forward || originalCharacterIndex == 0)
                    {
                        rect = rect.WithX(rect.Right);
                    }
                }
                else
                {
                    // NOTE: check for 'originalCharacterIndex > 0' is only required for position at the beginning
                    //       content with Backward orientation. This should not be a valid position.
                    //       Remove it later
                    if (orientedPosition.LogicalDirection == LogicalDirection.Backward && originalCharacterIndex > 0)
                    {
                        rect = rect.WithX(rect.Right);
                    }
                }

                rect = rect.WithWidth(0);
            }
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("NewTextBlock.GetRectangleFromTextPosition", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif

            return rect;
        }

        /// <summary>
        /// Implementation of TextParagraphView.GetTightBoundingGeometryFromTextPositions.
        /// <seealso cref="TextParagraphView.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StartTimer("NewTextBlock.GetTightBoundingGeometryFromTextPositions", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            Invariant.Assert(IsLayoutDataValid);
            Invariant.Assert(startPosition != null);
            Invariant.Assert(endPosition != null);
            Invariant.Assert(startPosition.CompareTo(endPosition) <= 0);

            Geometry geometry = null;

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent(); // TextOM access requires complex content.

            int dcpPositionStart = _complexContent.TextContainer.Start.GetOffsetToPosition(startPosition);
            int dcpPositionEnd = _complexContent.TextContainer.Start.GetOffsetToPosition(endPosition);

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            Line line = CreateLine(lineProperties);

            int dcpLineStart = 0;
            ITextPointer endOfLineTextPointer = _complexContent.TextContainer.Start.CreatePointer(0);
            double lineOffset = 0;

            int lineCount = LineCount;
            for (int i = 0, count = lineCount; i < count; ++i)
            {
                LineMetrics lineMetrics = GetLine(i);

                if (dcpPositionEnd <= dcpLineStart)
                {
                    //  this line starts after the range's end.
                    //  safe to break from the loop.
                    break;
                }

                int dcpLineEnd = dcpLineStart + lineMetrics.Length;
                endOfLineTextPointer.MoveByOffset(lineMetrics.Length);

                if (dcpPositionStart < dcpLineEnd)
                {
                    using (line)
                    {
                        bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                        Format(line, lineMetrics.Length, dcpLineStart, wrappingWidth, GetLineProperties(dcpLineStart == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);

                        if (Invariant.Strict)
                        {
                            // Check consistency of line formatting
                            MS.Internal.Invariant.Assert(GetLine(i).Length == line.Length, "Line length is out of sync");
                        }

                        int dcpStart = Math.Max(dcpLineStart, dcpPositionStart);
                        int dcpEnd = Math.Min(dcpLineEnd, dcpPositionEnd);

                        if (dcpStart != dcpEnd)
                        {
                            IList<Rect> aryTextBounds = line.GetRangeBounds(dcpStart, dcpEnd - dcpStart, contentOffset.X, contentOffset.Y + lineOffset);

                            if (aryTextBounds.Count > 0)
                            {
                                int j = 0;
                                int c = aryTextBounds.Count;

                                do
                                {
                                    Rect rect = aryTextBounds[j];

                                    if (j == (c - 1)
                                       && dcpPositionEnd >= dcpLineEnd
                                       && TextPointerBase.IsNextToAnyBreak(endOfLineTextPointer, LogicalDirection.Backward))
                                    {
                                        double endOfParaGlyphWidth = FontSize * CaretElement.c_endOfParaMagicMultiplier;
                                        rect = rect.WithWidth(rect.Width + endOfParaGlyphWidth);
                                    }

                                    RectangleGeometry rectGeometry = new RectangleGeometry(rect);
                                    CaretElement.AddGeometry(ref geometry, rectGeometry);
                                } while (++j < c);
                            }
                        }
                    }
                }

                dcpLineStart += lineMetrics.Length;
                lineOffset += lineMetrics.Height;
            }

#if TEXTPANELLAYOUTDEBUG
            MS.Internal.PtsHost.TextPanelDebug.StopTimer("NewTextBlock.GetTightBoundingGeometryFromTextPositions", MS.Internal.PtsHost.TextPanelDebug.Category.TextView);
#endif
            return (geometry);
        }

        /// <summary>
        /// Determines if the given position is at the edge of a caret unit
        /// in the specified direction, and returns true if it is and false otherwise.
        /// Used by the ITextView.IsCaretAtUnitBoundary(ITextPointer position) in
        /// TextParagraphView
        /// </summary>
        /// <param name="position">
        /// Position to test.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal bool IsAtCaretUnitBoundary(ITextPointer position, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);
            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            bool isAtCaretUnitBoundary = false;

            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);
            CharacterHit charHit = new CharacterHit();
            if (position.LogicalDirection == LogicalDirection.Backward)
            {
                if (characterIndex > dcp)
                {
                    // Go to trailing edge of previous character
                    charHit = new CharacterHit(characterIndex - 1, 1);
                }
                else
                {
                    // We should not be at line's start dcp with backward context, except in case this is the first line. This is not
                    // a unit boundary
                    return false;
                }
            }
            else if (position.LogicalDirection == LogicalDirection.Forward)
            {
                // Get leading edge of this character index
                charHit = new CharacterHit(characterIndex, 0);
            }

            LineMetrics lineMetrics = GetLine(lineIndex);
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);

            using (Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                isAtCaretUnitBoundary = line.IsAtCaretCharacterHit(charHit);
            }

            return isAtCaretUnitBoundary;
        }

        /// <summary>
        /// Finds and returns the next position at the edge of a caret unit in
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="direction">
        /// If Forward, this method returns the "caret unit" position following
        /// the initial position.
        /// If Backward, this method returns the caret unit" position preceding
        /// the initial position.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);

            // Process special cases
            if (characterIndex == dcp && direction == LogicalDirection.Backward)
            {
                // Start of line
                if (lineIndex == 0)
                {
                    // First line. Cannot go back any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp
                    Debug.Assert(lineIndex > 0);
                    --lineIndex;
                    dcp -= GetLine(lineIndex).Length;
                    Debug.Assert(dcp >= 0);
                }
            }
            else if (characterIndex == (dcp + GetLine(lineIndex).Length) && direction == LogicalDirection.Forward)
            {
                // End of line
                int lineCount = LineCount;
                if (lineIndex == lineCount - 1)
                {
                    // Cannot go down any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp to next line
                    Debug.Assert(lineIndex < lineCount - 1);
                    dcp += GetLine(lineIndex).Length;
                    ++lineIndex;
                }
            }


            // Creat CharacterHit from characterIndex and call line APIs
            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            CharacterHit textSourceCharacterIndex = new CharacterHit(characterIndex, 0);

            CharacterHit nextCharacterHit;
            LineMetrics lineMetrics = GetLine(lineIndex);

            using (Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                if (direction == LogicalDirection.Forward)
                {
                    // Get the next caret position from the line
                    nextCharacterHit = line.GetNextCaretCharacterHit(textSourceCharacterIndex);
                }
                else
                {
                    // Get previous caret position from the line
                    nextCharacterHit = line.GetPreviousCaretCharacterHit(textSourceCharacterIndex);
                }
            }

            // Determine logical direction for next caret index and create TextPointer from it
            LogicalDirection logicalDirection;
            if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == (dcp + GetLine(lineIndex).Length)) && direction == LogicalDirection.Forward)
            {
                // Going forward brought us to the end of a line, context must be forward for next line
                if (lineIndex == LineCount - 1)
                {
                    // last line so context must stay backward
                    logicalDirection = LogicalDirection.Backward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Forward;
                }
            }
            else if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == dcp) && direction == LogicalDirection.Backward)
            {
                // Going forward brought us to the start of a line, context must be backward for previous line
                if (dcp == 0)
                {
                    // First line, so we will stay forward
                    logicalDirection = LogicalDirection.Forward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Backward;
                }
            }
            else
            {
                logicalDirection = (nextCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            }
            ITextPointer nextCaretPosition = _complexContent.TextContainer.Start.CreatePointer(nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength, logicalDirection);


            // Return nextCaretPosition
            return nextCaretPosition;
        }

        /// <summary>
        /// Finds and returns the position after backspace at the edge of a caret unit in
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="dcp">
        /// Offset of the current position from start of TextContainer
        /// </param>
        /// <param name="lineIndex">
        /// Index of line in which position is found
        /// </param>
        internal ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position, int dcp, int lineIndex)
        {
            Invariant.Assert(IsLayoutDataValid);

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();
            EnsureComplexContent();

            // Get character index for position
            int characterIndex = _complexContent.TextContainer.Start.GetOffsetToPosition(position);

            // Process special cases
            if (characterIndex == dcp)
            {
                if (lineIndex == 0)
                {
                    // Cannot go back any further
                    return position;
                }
                else
                {
                    // Change lineIndex and dcp to previous line
                    Debug.Assert(lineIndex > 0);
                    --lineIndex;
                    dcp -= GetLine(lineIndex).Length;
                    Debug.Assert(dcp >= 0);
                }
            }

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            CharacterHit textSourceCharacterIndex = new CharacterHit(characterIndex, 0);
            CharacterHit backspaceCharacterHit;
            LineMetrics lineMetrics = GetLine(lineIndex);

            // Create and Format line
            using (Line line = CreateLine(lineProperties))
            {
                // Format line. Set showParagraphEllipsis flag to false since we are not using information about
                // ellipsis to change line offsets in this case.
                Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(lineIndex == 0, lineProperties), lineMetrics.TextLineBreak, false);

                // Check consistency of line formatting
                MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");

                backspaceCharacterHit = line.GetBackspaceCaretCharacterHit(textSourceCharacterIndex);
            }
            // Get CharacterHit and call line API

            // Determine logical direction for next caret index and create TextPointer from it
            LogicalDirection logicalDirection;
            if (backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength == dcp)
            {
                // Going forward brought us to the start of a line, context must be backward for previous line
                if (dcp == 0)
                {
                    // First line, so we will stay forward
                    logicalDirection = LogicalDirection.Forward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Backward;
                }
            }
            else
            {
                logicalDirection = (backspaceCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            }
            ITextPointer backspaceCaretPosition = _complexContent.TextContainer.Start.CreatePointer(backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength, logicalDirection);

            // Return backspaceCaretPosition
            return backspaceCaretPosition;
        }

        #endregion Internal methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        //-------------------------------------------------------------------
        // Text formatter object
        //-------------------------------------------------------------------
        internal TextFormatter TextFormatter
        {
            get
            {
                return TextFormatter.Current;
            }
        }

        //-------------------------------------------------------------------
        // Text container.
        //-------------------------------------------------------------------
        internal ITextContainer TextContainer
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.TextContainer;
            }
        }

        //-------------------------------------------------------------------
        // TextView
        //-------------------------------------------------------------------
        internal ITextView TextView
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.TextView;
            }
        }

        //-------------------------------------------------------------------
        // Highlights
        //-------------------------------------------------------------------
        internal Highlights Highlights
        {
            get
            {
                EnsureComplexContent();
                return _complexContent.Highlights;
            }
        }

        //-------------------------------------------------------------------
        // NewTextBlock paragraph properties.
        //-------------------------------------------------------------------
        internal LineProperties ParagraphProperties
        {
            get
            {
                LineProperties lineProperties = GetLineProperties();
                return lineProperties;
            }
        }


        //-------------------------------------------------------------------
        // IsLayoutDataValid
        //-------------------------------------------------------------------
        internal bool IsLayoutDataValid
        {
            get
            {
                return IsMeasureValid && IsArrangeValid &&         // Measure and Arrange are valid
                        CheckFlags(Flags.HasFirstLine) &&
                        !CheckFlags(Flags.ContentChangeInProgress) &&
                        !CheckFlags(Flags.MeasureInProgress) &&
                        !CheckFlags(Flags.ArrangeInProgress);  // Content is not currently changeing
            }
        }

        //-------------------------------------------------------------------
        // HasComplexContent
        //-------------------------------------------------------------------
        internal bool HasComplexContent
        {
            get
            {
                return (_complexContent != null);
            }
        }

        //-------------------------------------------------------------------
        // IsTypographyDefaultValue
        //-------------------------------------------------------------------
        internal bool IsTypographyDefaultValue
        {
            get
            {
                return !CheckFlags(Flags.IsTypographySet);
            }
        }

        //-------------------------------------------------------------------
        // InlineObjects
        //-------------------------------------------------------------------
        private ArrayList InlineObjects
        {
            get { return (_complexContent == null) ? null : _complexContent.InlineObjects; }
            set { if (_complexContent != null) _complexContent.InlineObjects = value; }
        }

        //-------------------------------------------------------------------
        // Is this NewTextBlock control being used by a ContentPresenter/ HyperLink
        // to host its content. If it is then NewTextBlock musn't try to disconnect the
        // logical parent pointer for the content. This flag allows the NewTextBlock
        // to discover this special scenario and behave differently.
        //-------------------------------------------------------------------
        internal bool IsContentPresenterContainer
        {
            get { return CheckFlags(Flags.IsContentPresenterContainer); }
            set { SetFlags(value, Flags.IsContentPresenterContainer); }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Raise TextView.Updated event, if TextView is in a valid state.
        /// </summary>
        private void OnValidateTextView()
        {
            if (IsLayoutDataValid && _complexContent != null)
            {
                _complexContent.TextView.OnUpdated();
            }
        }

        // Inserts text run into NewTextBlock in a form consistent with flow schema requirements.
        //
        // NewTextBlock has dual role as a text container -
        // plain text for TextBox and rich inline-collection content for everything else.
        // In case of plain text we insert only string into the text container,
        // in all other cases we must wrap eact rext run by Run inline element.
        private static void InsertTextRun(ITextPointer position, string text, bool whitespacesIgnorable)
        {
            // We distinguish these two cases by parent of TextContainer:
            // for plain text case it is TextBox.
            if (!(position is TextPointer) || ((TextPointer)position).Parent == null || ((TextPointer)position).Parent is TextBox)
            {
                position.InsertTextInRun(text);
            }
            else
            {
                if (!whitespacesIgnorable || text.Trim().Length > 0)
                {
                    Run implicitRun = Inline.CreateImplicitRun();

                    ((TextPointer)position).InsertTextElement(implicitRun);
                    implicitRun.Text = text;
                }
            }
        }

        // ------------------------------------------------------------------
        // Create appropriate line object.
        // a) SimpleLine, if the content is represented by a string.
        // b) ComplexLine, if the content is represented by a TextContainer.
        // ------------------------------------------------------------------
        private Line CreateLine(LineProperties lineProperties)
        {
            Line line;
            if (_complexContent == null)
                line = new SimpleLine(this, Text, lineProperties.DefaultTextRunProperties);
            else
                line = new ComplexLine(this);
            return line;
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is enabled.  Creates a default TextContainer.
        // ------------------------------------------------------------------
        private void EnsureComplexContent()
        {
            EnsureComplexContent(null);
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is enabled.
        // ------------------------------------------------------------------
        private void EnsureComplexContent(ITextContainer textContainer)
        {
            if (_complexContent == null)
            {
                if (textContainer == null)
                {
                    textContainer = new TextContainer(IsContentPresenterContainer ? null : this, false /* plainTextOnly */);
                }

                _complexContent = new ComplexContent(this, textContainer, false, Text);
                _contentCache = null;

                if (CheckFlags(Flags.FormattedOnce))
                {
                    // If we've already measured at least once, hook up the TextContainer
                    // listeners now.
                    Invariant.Assert(!CheckFlags(Flags.PendingTextContainerEventInit));
                    InitializeTextContainerListeners();

                    // Line layout data cached up to this point will become invalid
                    // becasue of content structure change (implicit Run added).
                    // So we need to clear the cache - we call InvalidateMeasure for this
                    // purpose. However, we do not want to produce a side effect
                    // of making layout invalid as a result of touching ContentStart/ContentEnd
                    // and other properties. For that we need to UpdateLayout when it was
                    // dirtied by our switch.
                    bool wasLayoutValid = this.IsMeasureValid && this.IsArrangeValid;
                    InvalidateMeasure();
                    InvalidateVisual(); //ensure re-rendering too
                    if (wasLayoutValid)
                    {
                        // TODO: Is this equivalent to UpdateLayout
                        var layoutManager = (VisualRoot as ILayoutRoot)?.LayoutManager;
                        layoutManager?.ExecuteLayoutPass();
                    }
                }
                else
                {
                    // Otherwise, wait until our first measure.
                    // This lets us skip the work for all content invalidation
                    // during load, before the first measure.
                    SetFlags(true, Flags.PendingTextContainerEventInit);
                }
            }
        }

        // ------------------------------------------------------------------
        // Make sure that complex content is cleared.
        // ------------------------------------------------------------------
        private void ClearComplexContent()
        {
            if (_complexContent != null)
            {
                _complexContent.Detach(this);
                _complexContent = null;
                Invariant.Assert(_contentCache == null, "Content cache should be null when complex content exists.");
            }
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changing notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChanging(object sender, EventArgs args)
        {
            Debug.Assert(sender == _complexContent.TextContainer, "Received text change for foreign TextContainer.");

            if (CheckFlags(Flags.FormattedOnce))
            {
                // Will throw an exception, if during measure/arrange/render process.
                VerifyTreeIsUnlocked();

                // Remember the fact that content is changing.
                // OnTextContainerEndChanging has to be received after this event.
                SetFlags(true, Flags.ContentChangeInProgress);
            }
        }

        // ------------------------------------------------------------------
        // Invalidates a portion of text affected by a highlight change.
        // ------------------------------------------------------------------
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            Invariant.Assert(args != null);
            Invariant.Assert(args.Ranges != null);
            Invariant.Assert(CheckFlags(Flags.FormattedOnce), "Unexpected Highlights.Changed callback before first format!");

            // The only supported highlight type for TextBlock is SpellerHightlight.
            // TextSelection and HighlightComponent are ignored, because they are handled by
            // separate layer.
            if (true /* TODO args.OwnerType != typeof(SpellerHighlightLayer)*/)
            {
                return;
            }

            // NOTE: Assuming that only rendering only properties are changeing
            //       through highlights.
            InvalidateVisual();
        }

        // ------------------------------------------------------------------
        // Handler for TextContainer changed notifications.
        // ------------------------------------------------------------------
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            Invariant.Assert(args != null);

            if (_complexContent == null)
            {
                // This shouldn't ever happen (we only hook up this handler when we have complex
                // content)... except that it does happen, in cases where NewTextBlock is part of
                // a style that gets changed in response to TextContainer.Changed events.  In such a case,
                // we're an obsolete text control and we don't want to do anything, so just return.
                return;
            }
            Invariant.Assert(sender == _complexContent.TextContainer, "Received text change for foreign TextContainer.");

            if (args.Count == 0)
            {
                // A no-op for this control.  Happens when IMECharCount updates happen
                // without corresponding SymbolCount changes.
                return;
            }

            if (CheckFlags(Flags.FormattedOnce))
            {
                // Will throw an exception, if during measure/arrange/render process.
                VerifyTreeIsUnlocked();
                // Content has been changed, so reset appropriate flag.
                SetFlags(false, Flags.ContentChangeInProgress);
                // Invalidate measure in responce to invalidated content.
                InvalidateMeasure();
            }

            if (!CheckFlags(Flags.TextContentChanging) && args.TextChange != TextChangeType.PropertyModified)
            {
                SetFlags(true, Flags.TextContentChanging);
                try
                {
                    // Use a DeferredTextReference instead of calculating the new
                    // value now for better performance.  Most of the time no
                    // one cares what the new is, and loading our content into a
                    // string can be expensive.
                    // TODO: Removed deferred optimization because I don't know how to do this in Avalonia
                    Text = TextRangeBase.GetTextInternal(TextContainer.Start, TextContainer.End);
                }
                finally
                {
                    SetFlags(false, Flags.TextContentChanging);
                }
            }
        }

        private void EnsureTextBlockCache()
        {
            if (null == _textBlockCache)
            {
                _textBlockCache = new TextBlockCache();
                _textBlockCache._lineProperties = GetLineProperties();
            }
        }

        // ------------------------------------------------------------------
        // Refetch and cache line properties, if needed.
        // ------------------------------------------------------------------
        private LineProperties GetLineProperties()
        {
            // For default text properties always set background to null.
            // REASON: If element associated with the text run is NewTextBlock element, ignore background
            //         brush, because it is handled outside as FrameworkElement's background.

            TextProperties defaultTextProperties = new TextProperties(this, this.IsTypographyDefaultValue);

            // Do not allow hyphenation for plain Text so always pass null for IHyphenate.
            // Pass page width and height as double.MaxValue when creating LineProperties, since NewTextBlock does not restrict
            // TextIndent or LineHeight
            LineProperties lineProperties = new LineProperties(this, this, defaultTextProperties);

            return lineProperties;
        }

        //-------------------------------------------------------------------
        // Get line properties
        //
        //      firstLine - is it for the first line?
        //
        // Returns: Line properties for first/following lines.
        //-------------------------------------------------------------------
        private TextParagraphProperties GetLineProperties(bool firstLine, LineProperties lineProperties)
        {
            return GetLineProperties(firstLine, false, lineProperties);
        }
        private TextParagraphProperties GetLineProperties(bool firstLine, bool showParagraphEllipsis, LineProperties lineProperties)
        {
            GetLineProperties();
            firstLine = firstLine && lineProperties.HasFirstLineProperties;
            if (!showParagraphEllipsis)
            {
                return firstLine ? lineProperties.FirstLineProps : lineProperties;
            }
            else
            {
                return lineProperties.GetParaEllipsisLineProps(firstLine);
            }
        }

        //-------------------------------------------------------------------
        // Calculate line advance distance. This functionality will go away
        // when TextFormatter will be able to handle line height/stacking.
        //
        //      lineHeight - calculated line height
        //
        // Returns: Line advance distance..
        //-------------------------------------------------------------------
        private double CalcLineAdvance(double lineHeight, LineProperties lineProperties)
        {
            return lineProperties.CalcLineAdvance(lineHeight);
        }

        //-------------------------------------------------------------------
        // Calculate offset of the content taking into account horizontal / vertical
        // content alignment.
        //
        // Returns: Content offset value.
        //-------------------------------------------------------------------
        private Vector CalcContentOffset(Size computedSize, double wrappingWidth)
        {
            Vector contentOffset = new Vector();

            Thickness padding = this.Padding;
            Size contentSize = new Size(Math.Max(0.0, computedSize.Width - (padding.Left + padding.Right)),
                                        Math.Max(0.0, computedSize.Height - (padding.Top + padding.Bottom)));

            switch (TextAlignment)
            {
                case TextAlignment.Right:
                    contentOffset = contentOffset.WithX(contentSize.Width - wrappingWidth);
                    break;

                case TextAlignment.Center:
                    contentOffset = contentOffset.WithX((contentSize.Width - wrappingWidth) / 2);
                    break;

                // Default is Left alignment, in this case offset is 0.
            }

            return new Vector(contentOffset.X + padding.Left, contentOffset.Y + padding.Top);
        }

        /// <summary>
        /// Returns true if paragraph ellipsis will be rendered on this line
        /// </summary>
        /// <param name="lineIndex">
        /// Index of the line
        /// </param>
        /// <param name="lineVOffset">
        /// Vertical offset at which line starts
        /// </param>
        private bool ParagraphEllipsisShownOnLine(int lineIndex, double lineVOffset)
        {
            if (lineIndex >= LineCount - 1)
            {
                // Last line. No paragraph ellipses
                return false;
            }

            // Find out if this is the last rendered line
            if (!CheckFlags(Flags.HasParagraphEllipses))
            {
                return false;
            }

            // Calculate bottom offset for next line
            double nextLineBottomOffset = GetLine(lineIndex + 1).Height + GetLine(lineIndex).Height + lineVOffset;
            // If the next line will exceed render height by a large margin, we cannot render
            // it at all and so we should show ellipsis on this one. However if the next line
            // almost fits, we will render it and so there should be no ellipsis
            double contentBottom = Math.Max(0.0, RenderSize.Height - Padding.Bottom);
            if (MathUtilities.GreaterThan(nextLineBottomOffset, contentBottom) && !MathUtilities.AreClose(nextLineBottomOffset, contentBottom))
            {
                return true;
            }

            return false;
        }

        //-------------------------------------------------------------------
        // Calculate wrapping width for lines.
        //
        // Returns: Wrapping width.
        //-------------------------------------------------------------------
        private double CalcWrappingWidth(double width)
        {
            // Reflowing will not happen when Width is between _previousDesiredSize.Width and ReferenceWidth.
            // In some cases _previousDesiredSize.Width > ReferenceSize, use ReferenceSize in those scenarios.
            if (width < _previousDesiredSize.Width)
            {
                width = _previousDesiredSize.Width;
            }
            if (width > _referenceSize.Width)
            {
                width = _referenceSize.Width;
            }

            bool usingReferenceWidth = MathUtilities.AreClose(width, _referenceSize.Width);
            double paddingWidth = Padding.Left + Padding.Right;

            width = Math.Max(0.0, width - paddingWidth);

            // We want FormatLine to make the same decisions it made during Measure,
            // otherwise text can be truncated or trimmed when it shouldn't be.
            // The problem arises when the TextBox has no declared width, so its
            // render size is the same as its desired size.
            //
            // During Measure, LineServices computes the text width in "ideal"
            // coordinates (integer), TextLine converts this to "real" coordinates
            // (double-precision floating-point), and NewTextBlock then adds the
            // padding to yield the desired size.  At render time (or hit-test,
            // or others), this is reversed:  starting with the render size,
            // subtract the padding, then convert from "real" to "ideal".  We want
            // the result to be the same as the original text width, but there are
            // two ways this can fail:
            //   a) in display-mode, conversion from ideal to real involves rounding
            //      to a multiple of the pixel size.  This can cause the final
            //      width to fall short of the original by as much as half a pixel.
            //   b) (width - padding) + padding  might be different from width,
            //      due to floating-point arithmetic error.
            // In either case, if the final width is even slightly smaller than the
            // original text width, LineServices might think there is not enough
            // room to format the entire line.
            //
            // The following code protects against these errors by adjusting
            // the wrapping width upward by a slight amount.  But only if there
            // may have been some loss.

            // No adjustment is needed if we're starting with the same width
            // Measure was given, or if the width is zero
            if (!usingReferenceWidth && width != 0.0)
            {
                // TODO Avalonia currently only supports Ideal, not Display
                // TODO TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(this);
                // TODO if (textFormattingMode == TextFormattingMode.Display)
                // TODO {
                // TODO     // case a: rounding to pixel boundaries can lose up to half a pixel,
                // TODO     // as adjusted for the current DPI setting
                // TODO     width += 0.5 / (GetDpi().DpiScaleY);
                // TODO }

                if (paddingWidth != 0.0)
                {
                    // case b: if padding is involved, add a tiny amount to
                    // protect against roundoff error
                    width += 0.00000000001;
                }
            }

            return width;
        }

        // ------------------------------------------------------------------
        // Wrapper for line.Format that tries to make the same line-break decisions as Measure
        // ------------------------------------------------------------------
        private void Format(Line line, int length, int dcp, double wrappingWidth, TextParagraphProperties paragraphProperties, TextLineBreak textLineBreak, bool ellipsis)
        {
            line.Format(dcp, wrappingWidth, paragraphProperties, textLineBreak, ellipsis);

            // line.Format can reflow (make a different line-break
            // decision than it did during measure), contrary to the comment
            // in CalcWrappingWidth "Reflowing will not happen when Width is
            // between _previousDesiredSize.Width and ReferenceWidth", if the
            // line contains text that gets shaped in a way that reduces the
            // total width.  Here is an example.
            //  Text="ABCDE IAATA Corp."  TextWrapping=Wrap  ReferenceWidth=115
            //  1. Measure calls FormatLine(115), which determines that the full
            //      text is wider than 115 and breaks it after the second word.
            //      The resulting desired width is 83.3167 - the length of
            //      the first line "ABCDE IAATA"
            //  2. Render, HitTest, et al. call FormatLine(83.3167), which determines
            //      that the first two words are already wider than 83.3167 and
            //      breaks after the first word.
            //  3. FormatLine uses unshaped glyph widths to determine how much text
            //      to consider in line-breaking decisions.  But it reports the
            //      width of the lines it produces using shaped glyph widths.
            //      In the example, the sequence "ATA" gets kerned closer together,
            //      making the shaped width of the first two words (83.3167)
            //      about 2.6 pixels less than the unshaped width (85.96).
            //      This is enough to produce the "reflowing".
            // The consequences of reflowing are bad.  In the example, the second
            // word is not rendered, and programmatic editing crashes with FailFast.
            //
            // In light of this, we need to work harder to ensure that reflowing
            // doesn't happen.  The obvious idea to accomplish this is to change
            // FormatLine to use shaped widths throughout, but that would mean
            // changing the callbacks from LineServices and DWrite, and asserting
            // that the changes have no unforseen consequences - out of scope.
            // Instead, we'll call FormatLine with a target width large enough
            // to produce the right line-break.
            //
            // This has consequences, especially when TextAlignment=Justify -
            // the line is justified to the larger width rather than to wrappingWidth,
            // which makes the text extend past the arrange-rect.  To mitigate this,
            // use the smallest width between wrappingWidth and ReferenceWidth that produces the
            // right line-break.
            //
            // This fixes the cases of missing text and FailFast, at the cost of
            //      1. more calls to FormatLine (perf hit)
            //      2. justified text sticks out of the arrange-rect
            // It's pay-for-play - we only do it on lines that reflow.

            if (line.Length < length)   // reflow happened
            {
                double goodWidth = _referenceSize.Width;    // no reflow at this width
                double badWidth = wrappingWidth;            // reflow at this width

                // The smallest good width can't be calcluated in advance, as it's
                // dependent on the shaped and unshaped glyph-widths and the available
                // width in a complicated way.  Instead, binary search.
                const double tolerance = 0.01;  // allow a small overshoot, to limit the number of iterations

                // In practice, the smallest good width is quite close to wrappingWidth,
                // so start with "bottom-up binary search".
                for (double delta = tolerance; /* goodWidth not found */; delta *= 2.0)
                {
                    double width = badWidth + delta;
                    if (width > goodWidth)
                        break;      // don't increase goodWidth

                    line.Format(dcp, width, paragraphProperties, textLineBreak, ellipsis);
                    if (line.Length < length)
                    {
                        badWidth = width;
                    }
                    else
                    {
                        goodWidth = width;
                        break;
                    }
                }

                // now do a regular binary search on the remaining interval
                for (double delta = (goodWidth - badWidth) / 2.0; delta > tolerance; delta /= 2.0)
                {
                    double width = badWidth + delta;
                    line.Format(dcp, width, paragraphProperties, textLineBreak, ellipsis);
                    if (line.Length < length)
                    {
                        badWidth = width;
                    }
                    else
                    {
                        goodWidth = width;
                    }
                }

                // now format at goodwidth, with no reflow
                line.Format(dcp, goodWidth, paragraphProperties, textLineBreak, ellipsis);
            }
        }

        // ------------------------------------------------------------------
        // Aborts calculation by throwing exception if world has changed
        // while in measure / arrange / render process.
        // ------------------------------------------------------------------
        private void VerifyTreeIsUnlocked()
        {
            if (CheckFlags(Flags.TreeInReadOnlyMode))
            {
                throw new InvalidOperationException(/*SR.Get(SRID.IllegalTreeChangeDetected)*/);
            }
        }

        // ------------------------------------------------------------------
        // Do content alignment.
        // ------------------------------------------------------------------
        private void AlignContent()
        {
            Debug.Assert(IsLayoutDataValid);
            Debug.Assert(CheckFlags(Flags.RequiresAlignment));

            // Line props may be invalid, even if Measure/Arrange is valid - rendering only props are changing.
            LineProperties lineProperties = GetLineProperties();

            double wrappingWidth = CalcWrappingWidth(RenderSize.Width);
            Vector contentOffset = CalcContentOffset(RenderSize, wrappingWidth);

            // Create / format all lines.
            // Since we are disposing line object, it can be reused to format following lines.
            Line line = CreateLine(lineProperties);

            int dcp = 0;
            double lineOffset = 0;
            int lineCount = LineCount;
            for (int i = 0; i < lineCount; i++)
            {
Debug.Assert(lineCount == LineCount);
                LineMetrics lineMetrics = GetLine(i);

                using (line)
                {
                    bool ellipsis = ParagraphEllipsisShownOnLine(i, lineOffset);
                    Format(line, lineMetrics.Length, dcp, wrappingWidth, GetLineProperties(dcp == 0, lineProperties), lineMetrics.TextLineBreak, ellipsis);
                    double lineHeight = CalcLineAdvance(line.Height, lineProperties);

                    // Check consistency of line formatting
                    MS.Internal.Invariant.Assert(lineMetrics.Length == line.Length, "Line length is out of sync");
                    Debug.Assert(MathUtilities.AreClose(lineHeight, lineMetrics.Height), "Line height is out of sync.");

                    // Calculated line width might be different from measure width in following cases:
                    // a) dynamically sized children, when FinalSize != AvailableSize
                    // b) non-default horizontal alignment, when FinalSize != AvailableSize
                    // Hence do not assert about matching line width with cached line metrics.

                    lineMetrics = UpdateLine(i, lineMetrics, line.Start, line.Width);
                    dcp += lineMetrics.Length;
                    lineOffset += lineHeight;
                }
            }
            SetFlags(false, Flags.RequiresAlignment);
        }

        // ------------------------------------------------------------------
        // OnRequestBringIntoView is called from the event handler NewTextBlock
        // registers for the event.
        // Handle the event for hosted ContentElements, and raise a new BringIntoView
        // event with the following values:
        // * object: (this)
        // * rect: A rect indicating the position of the ContentElement
        //
        //      sender - The instance handling the event.
        //      args   - RequestBringIntoViewEventArgs indicates the element
        //               and region to scroll into view.
        // ------------------------------------------------------------------
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            NewTextBlock textBlock = sender as NewTextBlock;
            StyledElement child = args.TargetObject as StyledElement;

            if (textBlock != null && child != null)
            {
                if (NewTextBlock.ContainsContentElement(textBlock, child))
                {
                    // Handle original event.
                    args.Handled = true;

                    // Retrieve the first rectangle representing the child and
                    // raise a new BrightIntoView event with such rectangle.

                    ReadOnlyCollection<Rect> rects = textBlock.GetRectanglesCore(child);
                    Invariant.Assert(rects != null, "Rect collection cannot be null.");
                    if (rects.Count > 0)
                    {
                        textBlock.BringIntoView(rects[0]);
                    }
                    else
                    {
                        textBlock.BringIntoView();
                    }
                }
            }
        }

        private static bool ContainsContentElement(NewTextBlock textBlock, StyledElement element)
        {
            if (textBlock._complexContent == null || !(textBlock._complexContent.TextContainer is TextContainer))
            {
                return false;
            }
            else if (element is TextElement)
            {
                if (textBlock._complexContent.TextContainer != ((TextElement)element).TextContainer)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private int LineCount
        {
            get
            {
                if (CheckFlags(Flags.HasFirstLine))
                {
                    return (_subsequentLines == null) ? 1 : _subsequentLines.Count + 1;
                }

                return 0;
            }
        }

        private LineMetrics GetLine(int index)
        {
            return (index == 0) ? _firstLine : _subsequentLines[index - 1];
        }

        private LineMetrics UpdateLine(int index, LineMetrics metrics, double start, double width)
        {
            metrics = new LineMetrics(metrics, start, width);

            if (index == 0)
            {
                _firstLine = metrics;
            }
            else
            {
                _subsequentLines[index - 1] = metrics;
            }

            return metrics;
        }

        // ------------------------------------------------------------------
        // SetFlags is used to set or unset one or multiple flags.
        // ------------------------------------------------------------------
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        // ------------------------------------------------------------------
        // CheckFlags returns true if all of passed flags in the bitmask are set.
        // ------------------------------------------------------------------
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        // ------------------------------------------------------------------
        // Ensures none of our public (or textview) methods can be called during measure/arrange/content change.
        // ------------------------------------------------------------------
        private void VerifyReentrancy()
        {
            if(CheckFlags(Flags.MeasureInProgress))
            {
                throw new InvalidOperationException(/*SR.Get(SRID.MeasureReentrancyInvalid)*/);
            }

            if(CheckFlags(Flags.ArrangeInProgress))
            {
                throw new InvalidOperationException(/*SR.Get(SRID.ArrangeReentrancyInvalid)*/);
            }

            if(CheckFlags(Flags.ContentChangeInProgress))
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextContainerChangingReentrancyInvalid)*/);
            }
        }

        /// <summary>
        /// Returns index of the line that starts at the given dcp. Returns -1 if
        /// no line or the line metrics collection starts at the given dcp
        /// </summary>
        /// <param name="dcpLine">
        /// Start dcp of required line
        /// </param>
        private int GetLineIndexFromDcp(int dcpLine)
        {
            Invariant.Assert(dcpLine >= 0);
            int lineIndex = 0;
            int lineStartOffset = 0;

            int lineCount = LineCount;
            while (lineIndex < lineCount)
            {
Debug.Assert(lineCount == LineCount);
                if (lineStartOffset == dcpLine)
                {
                    // Found line that starts at given dcp
                    return lineIndex;
                }
                else
                {
                    lineStartOffset += GetLine(lineIndex).Length;
                    ++lineIndex;
                }
            }

            // No line found starting at this position. Return -1.
            // We should never hit this code
            Invariant.Assert(false, "Dcp passed is not at start of any line in NewTextBlock");
            return -1;
        }

        // ------------------------------------------------------------------
        // IContentHost Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Searches for an element in the _complexContent.TextContainer. If the element is found, returns the
        /// position at which it is found. Otherwise returns null.
        /// </summary>
        /// <param name="e">
        /// Element to be found.
        /// </param>
        /// <remarks>
        /// We assume that this function is called from within text if the caller knows that _complexContent exists
        /// and contains a TextContainer. Hence we assert for this condition within the function
        /// </remarks>
        private TextPointer FindElementPosition(IInputElement e)
        {
            // Parameter validation
            Debug.Assert(e != null);

            // Validate that this function is only called when a TextContainer exists as complex content
            Debug.Assert(_complexContent.TextContainer is TextContainer);

            TextPointer position;

            // If e is a TextElement we can optimize by checking its TextContainer
            if (e is TextElement)
            {
                if ((e as TextElement).TextContainer == _complexContent.TextContainer)
                {
                    // Element found
                    position = new TextPointer((e as TextElement).ElementStart);
                    return position;
                }
            }

            // Else: search for e in the complex content
            position = new TextPointer((TextPointer)_complexContent.TextContainer.Start);
            while (position.CompareTo((TextPointer)_complexContent.TextContainer.End) < 0)
            {
                // Search each position in _complexContent.TextContainer for the element
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.EmbeddedElement:
                        IAvaloniaObject embeddedObject = position.GetAdjacentElement(LogicalDirection.Forward);
                        if (embeddedObject is StyledElement || embeddedObject is Control)
                        {
                            if (embeddedObject == e as StyledElement || embeddedObject == e as Control)
                            {
                                return position;
                            }
                        }
                        break;
                    default:
                          break;
                }
                position.MoveByOffset(+1);
            }

            // Reached end of complex content without finding the element
            return null;
        }

        /// <summary>
        /// Called when the child's BaselineOffset value changes.
        /// </summary>
        internal void OnChildBaselineOffsetChanged(IAvaloniaObject source)
        {
            // Ignore this notification, if currently in the measure process.
            if (!CheckFlags(Flags.MeasureInProgress))
            {
                // BaselineOffset,  may affect the
                // size. Hence invalidate measure.
                // There is no need to invalidate TextRunCache, since TextFormatter
                // regets inline object information even if TextRunCache is clean.
                InvalidateMeasure();
                InvalidateVisual(); //ensure re-rendering
            }
        }

        /// <summary>
        /// Property invalidator for baseline offset
        /// </summary>
        /// <param name="d">Dependency Object that the property value is being changed on.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnBaselineOffsetChanged(IAvaloniaObject d, bool before)
        {
            //Set up our baseline changed event

            //fire event!
            var te = d.GetValue(TextElement.ContainerTextElementProperty);

            if (te != null)
            {
                IAvaloniaObject parent = te.TextContainer.Parent;
                NewTextBlock tb = parent as NewTextBlock;
                if (tb != null)
                {
                    tb.OnChildBaselineOffsetChanged(d);
                }
                else
                {
                    // TODO FlowDocument fd = parent as FlowDocument;
                    // TODO if (fd != null && d is UIElement)
                    // TODO {
                    // TODO     fd.OnChildDesiredSizeChanged((UIElement)d);
                    // TODO }
                }
            }
        }

        // ------------------------------------------------------------------
        // Setup event handlers.
        // Deferred until the first measure.
        // ------------------------------------------------------------------
        private void InitializeTextContainerListeners()
        {
            _complexContent.TextContainer.Changing += new EventHandler(OnTextContainerChanging);
            _complexContent.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);
            _complexContent.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);
        }

        // ------------------------------------------------------------------
        // Clears out line metrics array, disposes as appropriate
        // ------------------------------------------------------------------
        private void ClearLineMetrics()
        {
            if (CheckFlags(Flags.HasFirstLine))
            {
                if (_subsequentLines != null)
                {
                    int subsequentLineCount = _subsequentLines.Count;
                    for (int i = 0; i < subsequentLineCount; i++)
                    {
                        _subsequentLines[i].Dispose(false);
                    }

                    _subsequentLines = null;
                }

                _firstLine = _firstLine.Dispose(true);
                SetFlags(false, Flags.HasFirstLine);
            }
        }

        private static bool IsValidTextTrimming(object o)
        {
            TextTrimming value = (TextTrimming)o;
            return value == TextTrimming.CharacterEllipsis
                || value == TextTrimming.None
                || value == TextTrimming.WordEllipsis;
        }

        private static bool IsValidTextWrap(object o)
        {
            TextWrapping value = (TextWrapping)o;
            return value == TextWrapping.Wrap
                || value == TextWrapping.NoWrap
                || value == TextWrapping.WrapWithOverflow;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //------------------------------------------------------------------
        // Simple content.
        //-------------------------------------------------------------------
        private TextBlockCache _textBlockCache;

        //------------------------------------------------------------------
        // Simple content.
        //-------------------------------------------------------------------
        private string _contentCache;

        //-------------------------------------------------------------------
        // Complex content.
        //-------------------------------------------------------------------
        private ComplexContent _complexContent;

        //-------------------------------------------------------------------
        // This size was the most recent constraint passed to MeasureOverride.
        // this.PreviousConstraintcan not be used because it can be affected
        // by Margin, Width/Min/MaxWidth propreties and ClipToBounds...
        //-------------------------------------------------------------------
        private Size _referenceSize;

        //-------------------------------------------------------------------
        // This size was a result returned by MeasureOverride last time.
        // this.DesiredSize can not be used because it can be affected by Margin,
        // Width/Min/MaxWidth propreties and ClipToBounds...
        //-------------------------------------------------------------------
        private Size _previousDesiredSize;

        //-------------------------------------------------------------------
        // Distance from the top of the Element to its baseline.
        //-------------------------------------------------------------------
        private double _baselineOffset;

        //-------------------------------------------------------------------
        // Collection of metrics of each line. For performance reasons, we
        // have separated out the first line from the array, since a single
        // line of text is a very common usage.  The LineCount, GetLine,
        // and UpdateLine members are used to simplify access to this
        // divided data structure.
        //-------------------------------------------------------------------
        private LineMetrics _firstLine;
        private List<LineMetrics> _subsequentLines;

        //-------------------------------------------------------------------
        // Flags reflecting various aspects of NewTextBlock's state.
        //-------------------------------------------------------------------
        private Flags _flags;
        [System.Flags]
        private enum Flags
        {
            FormattedOnce           = 0x1,      // Element has been formatted at least once.
            MeasureInProgress       = 0x2,      // Measure is in progress.
            TreeInReadOnlyMode      = 0x4,      // Tree (content) is in read only mode.
            RequiresAlignment       = 0x8,      // Content requires alignment process.
            ContentChangeInProgress = 0x10,     // Content change is in progress
                                                //(it has been started, but is is not completed yet).
            IsContentPresenterContainer = 0x20, // Is this Text control being used by a ContentPresenter to host its content
            HasParagraphEllipses    = 0x40,     // Has paragraph ellipses
            PendingTextContainerEventInit = 0x80, // Needs TextContainer event hookup on next Measure call.
            ArrangeInProgress       = 0x100,      // Arrange is in progress.
            IsTypographySet         = 0x200,      // Typography properties are not at default values
            TextContentChanging     = 0x400,    // TextProperty update in progress.
            IsHyphenatorSet         = 0x800,   // used to indicate when HyphenatorField has been set
            HasFirstLine            = 0x1000,
        }

        #endregion Private Fields
        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        //-------------------------------------------------------------------
        // Represents complex content.
        //-------------------------------------------------------------------
        private class ComplexContent
        {
            //---------------------------------------------------------------
            // Ctor
            //---------------------------------------------------------------
            internal ComplexContent(NewTextBlock owner, ITextContainer textContainer, bool foreignTextContianer, string content)
            {
                // Paramaters validation
                Debug.Assert(owner != null);
                Debug.Assert(textContainer != null);

                VisualChildren = new AvaloniaList<IVisual>();

                // Store TextContainer that contains content of the element.
                TextContainer = textContainer;
                ForeignTextContainer = foreignTextContianer;

                // Add content
                if (content != null && content.Length > 0)
                {
                    NewTextBlock.InsertTextRun(this.TextContainer.End, content, /*whitespacesIgnorable:*/false);
                }

                // Create TextView associated with TextContainer.
                this.TextView = new TextParagraphView(owner, TextContainer);

                // Hookup TextContainer to TextView.
                this.TextContainer.TextView = this.TextView;
            }

            //---------------------------------------------------------------
            // Detach event handlers.
            //---------------------------------------------------------------
            internal void Detach(NewTextBlock owner)
            {
                this.Highlights.Changed -= new HighlightChangedEventHandler(owner.OnHighlightChanged);
                this.TextContainer.Changing -= new EventHandler(owner.OnTextContainerChanging);
                this.TextContainer.Change -= new TextContainerChangeEventHandler(owner.OnTextContainerChange);
            }

            //------------------------------------------------------------------
            // Internal Visual Children.
            //-------------------------------------------------------------------
            internal AvaloniaList<IVisual> VisualChildren;

            //---------------------------------------------------------------
            // Highlights associated with TextContainer.
            //---------------------------------------------------------------
            internal Highlights Highlights { get { return this.TextContainer.Highlights; } }

            //---------------------------------------------------------------
            // Text array exposing access to the content.
            //---------------------------------------------------------------
            internal readonly ITextContainer TextContainer;

            //---------------------------------------------------------------
            // Is TextContainer owned by another object?
            //---------------------------------------------------------------
            internal readonly bool ForeignTextContainer;

            //---------------------------------------------------------------
            // TextView object associated with TextContainer.
            //---------------------------------------------------------------
            internal readonly TextParagraphView TextView;

            //---------------------------------------------------------------
            // Collection of inline objects hosted by the NewTextBlock control.
            //---------------------------------------------------------------
            internal ArrayList InlineObjects;
        }

        //-------------------------------------------------------------------
        // Simple content enumerator.
        //-------------------------------------------------------------------
        private class SimpleContentEnumerator : IEnumerator
        {
            //---------------------------------------------------------------
            // Ctor
            //---------------------------------------------------------------
            internal SimpleContentEnumerator(string content)
            {
                _content = content;
                _initialized = false;
                _invalidPosition = false;
            }

            //---------------------------------------------------------------
            // Sets the enumerator to its initial position, which is before
            // the first element in the collection.
            //---------------------------------------------------------------
            void IEnumerator.Reset()
            {
                _initialized = false;
                _invalidPosition = false;
            }

            //---------------------------------------------------------------
            // Advances the enumerator to the next element of the collection.
            //---------------------------------------------------------------
            bool IEnumerator.MoveNext()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    return true;
                }
                else
                {
                    _invalidPosition = true;
                    return false;
                }
            }

            //---------------------------------------------------------------
            // Gets the current element in the collection.
            //---------------------------------------------------------------
            object IEnumerator.Current
            {
                get
                {
                    if (!_initialized || _invalidPosition)
                    {
                        throw new InvalidOperationException();
                    }
                    return _content;
                }
            }

            //---------------------------------------------------------------
            // Content.
            //---------------------------------------------------------------
            private readonly string _content;
            private bool _initialized;
            private bool _invalidPosition;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  Dependency Property Helpers
        //
        //-------------------------------------------------------------------

        #region Dependency Property Helpers

        private static object CoerceBaselineOffset(IAvaloniaObject d, object value)
        {
            NewTextBlock tb = (NewTextBlock) d;

            if(double.IsNaN((double) value))
            {
                return tb._baselineOffset;
            }

            return value;
        }

        //-------------------------------------------------------------------
        // Text helpers
        //-------------------------------------------------------------------

        private void OnTextChanged(string newText)
        {
            if (this.CheckFlags(Flags.TextContentChanging))
            {
                // The update originated in a TextContainer change -- don't update
                // the TextContainer a second time.
                return;
            }

            if (this._complexContent == null)
            {
                this._contentCache = (newText != null) ? newText : String.Empty;
            }
            else
            {
                this.SetFlags(true, Flags.TextContentChanging);
                try
                {
                    bool exceptionThrown = true;

                    Invariant.Assert(this._contentCache == null, "Content cache should be null when complex content exists.");

                    this._complexContent.TextContainer.BeginChange();
                    try
                    {
                        ((TextContainer)this._complexContent.TextContainer).DeleteContentInternal((TextPointer)this._complexContent.TextContainer.Start, (TextPointer)this._complexContent.TextContainer.End);
                        InsertTextRun(this._complexContent.TextContainer.End, newText, /*whitespacesIgnorable:*/true);
                        exceptionThrown = false;
                    }
                    finally
                    {
                        this._complexContent.TextContainer.EndChange();

                        if (exceptionThrown)
                        {
                            this.ClearLineMetrics();
                        }
                    }
                }
                finally
                {
                    this.SetFlags(false, Flags.TextContentChanging);
                }
            }
        }

        #endregion Dependency Property Helpers
    }
}
