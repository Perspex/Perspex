using System;

using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    [PseudoClasses(":empty")]
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BoxShadow"/> property.
        /// </summary>
        public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
            Border.BoxShadowProperty.AddOwner<ContentPresenter>();
        
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly DirectProperty<ContentPresenter, IControl> ChildProperty =
            AvaloniaProperty.RegisterDirect<ContentPresenter, IControl>(
                nameof(Child),
                o => o.Child);

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="RecognizesAccessKey"/> property
        /// </summary>
        public static readonly DirectProperty<ContentPresenter, bool> RecognizesAccessKeyProperty =
            AvaloniaProperty.RegisterDirect<ContentPresenter, bool>(
                nameof(RecognizesAccessKey),
                cp => cp.RecognizesAccessKey, (cp, value) => cp.RecognizesAccessKey = value);

        private IControl _child;
        private bool _createdChild;
        private IRecyclingDataTemplate _recyclingDataTemplate;
        private readonly BorderRenderHelper _borderRenderer = new BorderRenderHelper();
        private bool _recognizesAccessKey;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            AffectsRender<ContentPresenter>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);
            AffectsArrange<ContentPresenter>(HorizontalContentAlignmentProperty, VerticalContentAlignmentProperty);
            // for ItemsRepeater invalidate measure on a DataContext change; this is for recycled controls to force the Content control to be re-measured
            AffectsMeasure<ContentPresenter>(BorderThicknessProperty, PaddingProperty, DataContextProperty);
            ContentProperty.Changed.AddClassHandler<ContentPresenter>((x, e) => x.ContentChanged(e));
            ContentTemplateProperty.Changed.AddClassHandler<ContentPresenter>((x, e) => x.ContentChanged(e));
            TemplatedParentProperty.Changed.AddClassHandler<ContentPresenter>((x, e) => x.TemplatedParentChanged(e));
        }

        public ContentPresenter()
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public IBrush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the box shadow effect parameters
        /// </summary>
        public BoxShadows BoxShadow
        {
            get => GetValue(BoxShadowProperty);
            set => SetValue(BoxShadowProperty, value);
        }

        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        public IControl Child
        {
            get { return _child; }
            private set { SetAndRaise(ChildProperty, ref _child, value); }
        }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        [DependsOn(nameof(ContentTemplate))]
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the border the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the border of the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the space between the border and the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Determine if <see cref="ContentPresenter"/> should use <see cref="AccessText"/> in its style
        /// </summary>
        public bool RecognizesAccessKey
        {
            get => _recognizesAccessKey;
            set => SetAndRaise(RecognizesAccessKeyProperty, ref _recognizesAccessKey, value);
        }

        /// <summary>
        /// Gets the host content control.
        /// </summary>
        internal IContentPresenterHost Host { get; private set; }

        /// <inheritdoc/>
        public sealed override void ApplyTemplate()
        {
            if (!_createdChild && ((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
        }

        /// <summary>
        /// Updates the <see cref="Child"/> control based on the control's <see cref="Content"/>.
        /// </summary>
        /// <remarks>
        /// Usually the <see cref="Child"/> control is created automatically when 
        /// <see cref="ApplyTemplate"/> is called; however for this to happen, the control needs to
        /// be attached to a logical tree (if the control is not attached to the logical tree, it
        /// is reasonable to expect that the DataTemplates needed for the child are not yet 
        /// available). This method forces the <see cref="Child"/> control's creation at any point, 
        /// and is particularly useful in unit tests.
        /// </remarks>
        public void UpdateChild()
        {
            var content = Content;
            var oldChild = Child;
            var newChild = CreateChild();
            var logicalChildren = Host?.LogicalChildren ?? LogicalChildren;

            // Remove the old child if we're not recycling it.
            if (newChild != oldChild)
            {

                if (oldChild != null)
                {
                    VisualChildren.Remove(oldChild);
                    logicalChildren.Remove(oldChild);
                    ((ISetInheritanceParent)oldChild).SetParent(oldChild.Parent);
                }
            }

            // Set the DataContext if the data isn't a control.
            if (!(content is IControl))
            {
                DataContext = content;
            }
            else
            {
                ClearValue(DataContextProperty);
            }

            // Update the Child.
            if (newChild == null)
            {
                Child = null;
            }
            else if (newChild != oldChild)
            {
                ((ISetInheritanceParent)newChild).SetParent(this);
                Child = newChild;

                if (!logicalChildren.Contains(newChild))
                {
                    logicalChildren.Add(newChild);
                }

                VisualChildren.Add(newChild);
            }

            _createdChild = true;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _recyclingDataTemplate = null;
            _createdChild = false;
            InvalidateMeasure();
        }

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            _borderRenderer.Render(context, Bounds.Size, BorderThickness, CornerRadius, Background, BorderBrush,
                BoxShadow);
        }

        /// <summary>
        /// Creates the child control.
        /// </summary>
        /// <returns>The child control or null.</returns>
        protected virtual IControl CreateChild()
        {
            var content = Content;
            var oldChild = Child;
            var newChild = content as IControl;

            if (content != null && newChild == null)
            {
                var dataTemplate = this.FindDataTemplate(content, ContentTemplate) ?? 
                    (
                        RecognizesAccessKey 
                            ? FuncDataTemplate.Access 
                            : FuncDataTemplate.Default
                    );

                if (dataTemplate is IRecyclingDataTemplate rdt)
                {
                    var toRecycle = rdt == _recyclingDataTemplate ? oldChild : null;
                    newChild = rdt.Build(content, toRecycle);
                    _recyclingDataTemplate = rdt;
                }
                else
                {
                    newChild = dataTemplate.Build(content);
                    _recyclingDataTemplate = null;
                }
            }
            else
            {
                _recyclingDataTemplate = null;
            }

            return newChild;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return ArrangeOverrideImpl(finalSize, new Vector());
        }

        internal Size ArrangeOverrideImpl(Size finalSize, Vector offset)
        {
            if (Child == null) return finalSize;

            var padding = Padding + BorderThickness;
            var horizontalContentAlignment = HorizontalContentAlignment;
            var verticalContentAlignment = VerticalContentAlignment;
            var useLayoutRounding = UseLayoutRounding;
            var availableSize = finalSize;
            var sizeForChild = availableSize;
            var scale = LayoutHelper.GetLayoutScale(this);
            var originX = offset.X;
            var originY = offset.Y;

            if (horizontalContentAlignment != HorizontalAlignment.Stretch)
            {
                sizeForChild = sizeForChild.WithWidth(Math.Min(sizeForChild.Width, DesiredSize.Width));
            }

            if (verticalContentAlignment != VerticalAlignment.Stretch)
            {
                sizeForChild = sizeForChild.WithHeight(Math.Min(sizeForChild.Height, DesiredSize.Height));
            }

            if (useLayoutRounding)
            {
                sizeForChild = LayoutHelper.RoundLayoutSize(sizeForChild, scale, scale);
                availableSize = LayoutHelper.RoundLayoutSize(availableSize, scale, scale);
            }

            switch (horizontalContentAlignment)
            {
                case HorizontalAlignment.Center:
                    originX += (availableSize.Width - sizeForChild.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    originX += availableSize.Width - sizeForChild.Width;
                    break;
            }

            switch (verticalContentAlignment)
            {
                case VerticalAlignment.Center:
                    originY += (availableSize.Height - sizeForChild.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    originY += availableSize.Height - sizeForChild.Height;
                    break;
            }

            if (useLayoutRounding)
            {
                originX = LayoutHelper.RoundLayoutValue(originX, scale);
                originY = LayoutHelper.RoundLayoutValue(originY, scale);
            }

            var boundsForChild =
                new Rect(originX, originY, sizeForChild.Width, sizeForChild.Height).Deflate(padding);

            Child.Arrange(boundsForChild);

            return finalSize;
        }

        /// <summary>
        /// Called when the <see cref="Content"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _createdChild = false;

            if (((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
            else if (Child != null)
            {
                VisualChildren.Remove(Child);
                LogicalChildren.Remove(Child);
                ((ISetInheritanceParent)Child).SetParent(Child.Parent);
                Child = null;
                _recyclingDataTemplate = null;
            }

            UpdatePseudoClasses();
            InvalidateMeasure();
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":empty", Content is null);
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var host = e.NewValue as IContentPresenterHost;
            Host = host?.RegisterContentPresenter(this) == true ? host : null;
        }
    }
}
