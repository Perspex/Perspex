using System;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A lookless control whose visual appearance is defined by its <see cref="Template"/>.
    /// </summary>
    public class TemplatedControl : Control, ITemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="Template"/> property.
        /// </summary>
        public static readonly StyledProperty<IControlTemplate> TemplateProperty =
            AvaloniaProperty.Register<TemplatedControl, IControlTemplate>(nameof(Template));

        /// <summary>
        /// Defines the IsTemplateFocusTarget attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsTemplateFocusTargetProperty =
            AvaloniaProperty.RegisterAttached<TemplatedControl, Control, bool>("IsTemplateFocusTarget");

        /// <summary>
        /// Defines the <see cref="TemplateApplied"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<TemplateAppliedEventArgs> TemplateAppliedEvent =
            RoutedEvent.Register<TemplatedControl, TemplateAppliedEventArgs>(
                "TemplateApplied", 
                RoutingStrategies.Direct);

        private IControlTemplate _appliedTemplate;

        /// <summary>
        /// Initializes static members of the <see cref="TemplatedControl"/> class.
        /// </summary>
        static TemplatedControl()
        {
            ClipToBoundsProperty.OverrideDefaultValue<TemplatedControl>(true);
            TemplateProperty.Changed.AddClassHandler<TemplatedControl>((x, e) => x.OnTemplateChanged(e));
        }

        /// <summary>
        /// Raised when the control's template is applied.
        /// </summary>
        public event EventHandler<TemplateAppliedEventArgs> TemplateApplied
        {
            add { AddHandler(TemplateAppliedEvent, value); }
            remove { RemoveHandler(TemplateAppliedEvent, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's border.
        /// </summary>
        public IBrush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the control's border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight used to draw the control's text.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's text and other foreground elements.
        /// </summary>
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the padding placed between the border of the control and its content.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the template that defines the control's appearance.
        /// </summary>
        public IControlTemplate Template
        {
            get { return GetValue(TemplateProperty); }
            set { SetValue(TemplateProperty, value); }
        }

        /// <summary>
        /// Gets the value of the IsTemplateFocusTargetProperty attached property on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The property value.</returns>
        /// <see cref="SetIsTemplateFocusTarget(Control, bool)"/>
        public static bool GetIsTemplateFocusTarget(Control control)
        {
            return control.GetValue(IsTemplateFocusTargetProperty);
        }

        /// <summary>
        /// Sets the value of the IsTemplateFocusTargetProperty attached property on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value.</param>
        /// <remarks>
        /// When a control is navigated to using the keyboard, a focus adorner is shown - usually
        /// around the control itself. However if the TemplatedControl.IsTemplateFocusTarget 
        /// attached property is set to true on an element in the control template, then the focus
        /// adorner will be shown around that control instead.
        /// </remarks>
        public static void SetIsTemplateFocusTarget(Control control, bool value)
        {
            control.SetValue(IsTemplateFocusTargetProperty, value);
        }

        /// <inheritdoc/>
        public sealed override void ApplyTemplate()
        {
            var template = Template;
            var logical = (ILogical)this;

            // Apply the template if it is not the same as the template already applied - except
            // for in the case that the template is null and we're not attached to the logical 
            // tree. In that case, the template has probably been cleared because the style setting
            // the template has been detached, so we want to wait until it's re-attached to the 
            // logical tree as if it's re-attached to the same tree the template will be the same
            // and we don't need to do anything.
            if (_appliedTemplate != template && (template != null || logical.IsAttachedToLogicalTree))
            {
                if (VisualChildren.Count > 0)
                {
                    foreach (var child in this.GetTemplateChildren())
                    {
                        child.SetValue(TemplatedParentProperty, null);
                        ((ISetLogicalParent)child).SetParent(null);
                    }

                    VisualChildren.Clear();
                }

                if (template != null)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(this, "Creating control template");

                    var (child, nameScope) = template.Build(this);
                    ApplyTemplatedParent(child);
                    ((ISetLogicalParent)child).SetParent(this);
                    VisualChildren.Add(child);
                    
                    // Existing code kinda expect to see a NameScope even if it's empty
                    if (nameScope == null)
                        nameScope = new NameScope();

                    var e = new TemplateAppliedEventArgs(nameScope);
                    OnApplyTemplate(e);
#pragma warning disable CS0618 // Type or member is obsolete
                    OnTemplateApplied(e);
#pragma warning restore CS0618 // Type or member is obsolete
                    RaiseEvent(e);
                }

                _appliedTemplate = template;
            }
        }

        /// <inheritdoc/>
        protected override IControl GetTemplateFocusTarget()
        {
            foreach (Control child in this.GetTemplateChildren())
            {
                if (GetIsTemplateFocusTarget(child))
                {
                    return child;
                }
            }

            return this;
        }

        protected sealed override void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
        {
            var count = VisualChildren.Count;

            for (var i = 0; i < count; ++i)
            {
                if (VisualChildren[i] is ILogical logical)
                {
                    logical.NotifyResourcesChanged(e);
                }
            }

            base.NotifyChildResourcesChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (VisualChildren.Count > 0)
            {
                ((ILogical)VisualChildren[0]).NotifyAttachedToLogicalTree(e);
            }

            base.OnAttachedToLogicalTree(e);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (VisualChildren.Count > 0)
            {
                ((ILogical)VisualChildren[0]).NotifyDetachedFromLogicalTree(e);
            }

            base.OnDetachedFromLogicalTree(e);
        }

        protected virtual void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the control's template is applied.
        /// </summary>
        /// <param name="e">The event args.</param>
        [Obsolete("Use OnApplyTemplate")]
        protected virtual void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the <see cref="Template"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
        }

        /// <summary>
        /// Sets the TemplatedParent property for the created template children.
        /// </summary>
        /// <param name="control">The control.</param>
        private void ApplyTemplatedParent(IControl control)
        {
            control.SetValue(TemplatedParentProperty, this);

            var children = control.LogicalChildren;
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                if (children[i] is IControl child)
                {
                    ApplyTemplatedParent(child);
                }
            }
        }
    }
}
