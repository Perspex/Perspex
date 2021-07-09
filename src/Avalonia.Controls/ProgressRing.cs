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
    [PseudoClasses(":preserveaspect", ":indeterminate")]
    public class ProgressRing : RangeBase
    {
        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            ProgressBar.IsIndeterminateProperty.AddOwner<ProgressRing>();
        
        public static readonly StyledProperty<bool> PreserveAspectProperty =
            AvaloniaProperty.Register<ProgressRing, bool>(nameof(PreserveAspect), true);
        
        public static readonly StyledProperty<double> ValueAngleProperty = 
            AvaloniaProperty.Register<ProgressRing, double>(nameof(ValueAngle), -90.0);

        static ProgressRing()
        {
            MinimumProperty.Changed.AddClassHandler<ProgressRing>(OnRangePropertiesChanged);
            ValueProperty.Changed.AddClassHandler<ProgressRing>(OnRangePropertiesChanged);
            MaximumProperty.Changed.AddClassHandler<ProgressRing>(OnRangePropertiesChanged);
        }

        public ProgressRing()
        {
            UpdatePseudoClasses(IsIndeterminate, PreserveAspect);
        }

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public bool PreserveAspect
        {
            get => GetValue(PreserveAspectProperty);
            set => SetValue(PreserveAspectProperty, value);
        }

        public double ValueAngle
        {
            get => GetValue(ValueAngleProperty);
            private set => SetValue(ValueAngleProperty, value);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsIndeterminateProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<bool>(), null);
            }
            else if (change.Property == PreserveAspectProperty)
            {
                UpdatePseudoClasses(null, change.NewValue.GetValueOrDefault<bool>());
            }
        }

        private void UpdatePseudoClasses(
            bool? isIndeterminate,
            bool? preserveAspect)
        {
            if (isIndeterminate.HasValue)
            {
                PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
            }

            if (preserveAspect.HasValue)
            {
                PseudoClasses.Set(":preserveaspect", preserveAspect.Value);
            }
        }

        static void OnRangePropertiesChanged(ProgressRing sender, AvaloniaPropertyChangedEventArgs e)
        {
            double min = sender.Minimum;
            double ringVal = sender.Value;
            double max = sender.Maximum;

            if ((e.NewValue != null) && (e.NewValue is double newPropVal))
            {
                if (e.Property == MinimumProperty)
                    min = newPropVal;
                else if (e.Property == ValueProperty)
                    ringVal = newPropVal;
                else if (e.Property == MaximumProperty)
                    max = newPropVal;
            }

            sender.ValueAngle = (((ringVal - min) / (max - min)) * 360.0) - 90;
        }
    }
}
