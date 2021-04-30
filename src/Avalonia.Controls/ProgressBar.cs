using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    [PseudoClasses(":vertical", ":horizontal", ":indeterminate")]
    public class ProgressBar : RangeBase
    {
        public class ProgressBarTemplateProperties : AvaloniaObject
        {
            private double _container2Width;
            private double _containerWidth;
            private double _containerAnimationStartPosition;
            private double _containerAnimationEndPosition;
            private double _container2AnimationStartPosition;
            private double _container2AnimationEndPosition;

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerAnimationStartPositionProperty =
           AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
               nameof(ContainerAnimationStartPosition),
               p => p.ContainerAnimationStartPosition,
               (p, o) => p.ContainerAnimationStartPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerAnimationEndPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(ContainerAnimationEndPosition),
                    p => p.ContainerAnimationEndPosition,
                    (p, o) => p.ContainerAnimationEndPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2AnimationStartPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2AnimationStartPosition),
                    p => p.Container2AnimationStartPosition,
                    (p, o) => p.Container2AnimationStartPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2AnimationEndPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2AnimationEndPosition),
                    p => p.Container2AnimationEndPosition,
                    (p, o) => p.Container2AnimationEndPosition = o);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2WidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2Width),
                    p => p.Container2Width,
                    (p, o) => p.Container2Width = o);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerWidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(ContainerWidth),
                    p => p.ContainerWidth,
                    (p, o) => p.ContainerWidth = o);

            public double ContainerAnimationStartPosition
            {
                get => _containerAnimationStartPosition;
                set => SetAndRaise(ContainerAnimationStartPositionProperty, ref _containerAnimationStartPosition, value);
            }

            public double ContainerAnimationEndPosition
            {
                get => _containerAnimationEndPosition;
                set => SetAndRaise(ContainerAnimationEndPositionProperty, ref _containerAnimationEndPosition, value);
            }

            public double Container2AnimationStartPosition
            {
                get => _container2AnimationStartPosition;
                set => SetAndRaise(Container2AnimationStartPositionProperty, ref _container2AnimationStartPosition, value);
            }

            public double Container2Width
            {
                get => _container2Width;
                set => SetAndRaise(Container2WidthProperty, ref _container2Width, value);
            }

            public double ContainerWidth
            {
                get => _containerWidth;
                set => SetAndRaise(ContainerWidthProperty, ref _containerWidth, value);
            }

            public double Container2AnimationEndPosition
            {
                get => _container2AnimationEndPosition;
                set => SetAndRaise(Container2AnimationEndPositionProperty, ref _container2AnimationEndPosition, value);
            }
        }

        private double _indeterminateStartingOffset;
        private double _indeterminateEndingOffset;
        private Border _indicator;

        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        public static readonly StyledProperty<bool> ShowProgressTextProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(ShowProgressText));

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ProgressBar, Orientation>(nameof(Orientation), Orientation.Horizontal);

        [Obsolete("To be removed when Avalonia.Themes.Default is discontinued.")]
        public static readonly DirectProperty<ProgressBar, double> IndeterminateStartingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateStartingOffset),
                p => p.IndeterminateStartingOffset,
                (p, o) => p.IndeterminateStartingOffset = o);

        [Obsolete("To be removed when Avalonia.Themes.Default is discontinued.")]
        public static readonly DirectProperty<ProgressBar, double> IndeterminateEndingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateEndingOffset),
                p => p.IndeterminateEndingOffset,
                (p, o) => p.IndeterminateEndingOffset = o);

        [Obsolete("To be removed when Avalonia.Themes.Default is discontinued.")]
        public double IndeterminateStartingOffset
        {
            get => _indeterminateStartingOffset;
            set => SetAndRaise(IndeterminateStartingOffsetProperty, ref _indeterminateStartingOffset, value);
        }

        [Obsolete("To be removed when Avalonia.Themes.Default is discontinued.")]
        public double IndeterminateEndingOffset
        {
            get => _indeterminateEndingOffset;
            set => SetAndRaise(IndeterminateEndingOffsetProperty, ref _indeterminateEndingOffset, value);
        }

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            MinimumProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            MaximumProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
        }

        public ProgressBar()
        {
            UpdatePseudoClasses(IsIndeterminate, Orientation);
        }

        public ProgressBarTemplateProperties TemplateProperties { get; } = new ProgressBarTemplateProperties();

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public bool ShowProgressText
        {
            get => GetValue(ShowProgressTextProperty);
            set => SetValue(ShowProgressTextProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateIndicator(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsIndeterminateProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<bool>(), null);
            }
            else if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(null, change.NewValue.GetValueOrDefault<Orientation>());
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            UpdateIndicator(Bounds.Size);
        }

        private void UpdateIndicator(Size bounds)
        {
            if (_indicator != null)
            {
                if (IsIndeterminate)
                {
                    // Pulled from ModernWPF.

                    var dim = Orientation == Orientation.Horizontal ? bounds.Width : bounds.Height;
                    var barIndicatorWidth = dim * 0.4; // Indicator width at 40% of ProgressBar
                    var barIndicatorWidth2 = dim * 0.6; // Indicator width at 60% of ProgressBar

                    TemplateProperties.ContainerWidth = barIndicatorWidth;
                    TemplateProperties.Container2Width = barIndicatorWidth2;

                    TemplateProperties.ContainerAnimationStartPosition = barIndicatorWidth * -1.8; // Position at -180%
                    TemplateProperties.ContainerAnimationEndPosition = barIndicatorWidth * 3.0; // Position at 300%

                    TemplateProperties.Container2AnimationStartPosition = barIndicatorWidth2 * -1.5; // Position at -150%
                    TemplateProperties.Container2AnimationEndPosition = barIndicatorWidth2 * 1.66; // Position at 166%

                    // Remove these properties when we switch to fluent as default and removed the old one.
#pragma warning disable CS0618 // Type or member is obsolete
                    IndeterminateStartingOffset = -dim;
                    IndeterminateEndingOffset = dim;
#pragma warning restore CS0618 // Type or member is obsolete

                    var padding = Padding;
                    var rectangle = new RectangleGeometry(
                        new Rect(
                            padding.Left,
                            padding.Top,
                            bounds.Width - (padding.Right + padding.Left),
                            bounds.Height - (padding.Bottom + padding.Top)
                            ));
                }
                else
                {
                    double percent = Maximum == Minimum ? 1.0 : (Value - Minimum) / (Maximum - Minimum);

                    if (Orientation == Orientation.Horizontal)
                        _indicator.Width = bounds.Width * percent;
                    else
                        _indicator.Height = bounds.Height * percent;
                }
            }
        }

        private void UpdateIndicatorWhenPropChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }

        private void UpdatePseudoClasses(
            bool? isIndeterminate,
            Orientation? o)
        {
            if (isIndeterminate.HasValue)
            {
                PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
            }

            if (o.HasValue)
            {
                PseudoClasses.Set(":vertical", o == Orientation.Vertical);
                PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
            }
        }
    }
}
