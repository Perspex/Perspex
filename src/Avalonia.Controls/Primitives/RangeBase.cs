using System;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that display a value within a range.
    /// </summary>
    public abstract class RangeBase : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> MinimumProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Minimum),
                o => o.Minimum,
                (o, v) => o.Minimum = v);

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> MaximumProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Maximum),
                o => o.Maximum,
                (o, v) => o.Maximum = v);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SmallChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SmallChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(SmallChange), 1);

        /// <summary>
        /// Defines the <see cref="LargeChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LargeChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(LargeChange), 10);

        private double _minimum;
        private double _maximum = 100.0;
        private double _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBase"/> class.
        /// </summary>
        public RangeBase()
        {
        }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public double Minimum
        {
            get
            {
                return _minimum;
            }

            set
            {
                if (!ValidateDouble(value))
                {
                    return;
                }

                if (IsInitialized)
                {
                    SetAndRaise(MinimumProperty, ref _minimum, value);
                    Maximum = ValidateMaximum(Maximum);
                    Value = ValidateValue(Value);
                }
                else
                {
                    SetAndRaise(MinimumProperty, ref _minimum, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public double Maximum
        {
            get
            {
                return _maximum;
            }

            set
            {
                if (!ValidateDouble(value))
                {
                    return;
                }

                if (IsInitialized)
                {
                    value = ValidateMaximum(value);
                    SetAndRaise(MaximumProperty, ref _maximum, value);
                    Value = ValidateValue(Value);
                }
                else
                {
                    SetAndRaise(MaximumProperty, ref _maximum, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (!ValidateDouble(value))
                {
                    return;
                }

                if (IsInitialized)
                {
                    value = ValidateValue(value);
                    SetAndRaise(ValueProperty, ref _value, value);
                }
                else
                {
                    SetAndRaise(ValueProperty, ref _value, value);
                }
            }
        }

        public double SmallChange
        {
            get => GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public double LargeChange
        {
            get => GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Maximum = ValidateMaximum(Maximum);
            Value = ValidateValue(Value);
        }

        /// <summary>
        /// Checks if the double value is not infinity nor NaN.
        /// </summary>
        /// <param name="value">The value.</param>
        private static bool ValidateDouble(double value)
        {
            return !double.IsInfinity(value) || !double.IsNaN(value);
        }

        /// <summary>
        /// Validates/coerces the <see cref="Maximum"/> property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double ValidateMaximum(double value)
        {
            return Math.Max(value, Minimum);
        }

        /// <summary>
        /// Validates/coerces the <see cref="Value"/> property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double ValidateValue(double value)
        {
            return MathUtilities.Clamp(value, Minimum, Maximum);
        }
    }
}
