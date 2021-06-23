using System;

namespace Avalonia.Media
{
    public class ExperimentalAcrylicMaterial : AvaloniaObject, IMutableExperimentalAcrylicMaterial
    {
        private Color _effectiveTintColor;
        private Color _effectiveLuminosityColor;

        static ExperimentalAcrylicMaterial()
        {
            AffectsRender<ExperimentalAcrylicMaterial>(
                TintColorProperty,
                BackgroundSourceProperty,
                TintOpacityProperty,
                MaterialOpacityProperty,
                PlatformTransparencyCompensationLevelProperty);

            TintColorProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            TintOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            MaterialOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            PlatformTransparencyCompensationLevelProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });
        }

        /// <summary>
        /// Defines the <see cref="TintColor"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> TintColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, Color>(nameof(TintColor));

        /// <summary>
        /// Defines the <see cref="BackgroundSource"/> property.
        /// </summary>
        public static readonly StyledProperty<AcrylicBackgroundSource> BackgroundSourceProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, AcrylicBackgroundSource>(nameof(BackgroundSource));

        /// <summary>
        /// Defines the <see cref="TintOpacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TintOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(TintOpacity), 0.8);

        /// <summary>
        /// Defines the <see cref="MaterialOpacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaterialOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(MaterialOpacity), 0.5);

        /// <summary>
        /// Defines the <see cref="PlatformTransparencyCompensationLevel"/> property.
        /// </summary>
        public static readonly StyledProperty<double> PlatformTransparencyCompensationLevelProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(PlatformTransparencyCompensationLevel), 0.0);

        /// <summary>
        /// Defines the <see cref="FallbackColor"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> FallbackColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, Color>(nameof(FallbackColor));

        /// <inheritdoc/>
        public event EventHandler Invalidated;

        /// <summary>
        /// Gets or Sets the BackgroundSource <seealso cref="AcrylicBackgroundSource"/>.
        /// </summary>
        public AcrylicBackgroundSource BackgroundSource
        {
            get => GetValue(BackgroundSourceProperty);
            set => SetValue(BackgroundSourceProperty, value);
        }

        /// <summary>
        /// Gets or Sets the TintColor.
        /// </summary>
        public Color TintColor
        {
            get => GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        /// <summary>
        /// Gets or Sets the Tint Opacity.
        /// </summary>
        public double TintOpacity
        {
            get => GetValue(TintOpacityProperty);
            set => SetValue(TintOpacityProperty, value);
        }

        /// <summary>
        /// Gets or Sets the Fallback Color.
        /// This is used on rendering plaforms that dont support acrylic.
        /// </summary>
        public Color FallbackColor
        {
            get => GetValue(FallbackColorProperty);
            set => SetValue(FallbackColorProperty, value);
        }

        /// <summary>
        /// Gets or Sets the MaterialOpacity.
        /// This makes the material more or less opaque.
        /// </summary>
        public double MaterialOpacity
        {
            get => GetValue(MaterialOpacityProperty);
            set => SetValue(MaterialOpacityProperty, value);
        }

        /// <summary>
        /// Gets or Sets the PlatformTransparencyCompensationLevel.
        /// This value defines the minimum <see cref="MaterialOpacity"/> that can be used.
        /// It means material opacity is re-scaled from this value to 1.
        /// </summary>
        public double PlatformTransparencyCompensationLevel
        {
            get => GetValue(PlatformTransparencyCompensationLevelProperty);
            set => SetValue(PlatformTransparencyCompensationLevelProperty, value);
        }

        Color IExperimentalAcrylicMaterial.MaterialColor => _effectiveLuminosityColor;

        Color IExperimentalAcrylicMaterial.TintColor => _effectiveTintColor;

        private struct HsvColor
        {
            public float Hue { get; set; }
            public float Saturation { get; set; }
            public float Value { get; set; }
        }

        private static HsvColor RgbToHsv(Color color)
        {
            var r = color.R / 255.0f;
            var g = color.G / 255.0f;
            var b = color.B / 255.0f;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            float h, s, v;
            h = v = max;

            var d = max - min;
            s = max == 0 ? 0 : d / max;

            if (max == min)
            {
                h = 0; // achromatic
            }
            else
            {
                if (max == r)
                {
                    h = (g - b) / d + (g < b ? 6 : 0);
                }
                else if (max == g)
                {
                    h = (b - r) / d + 2;
                }
                else if (max == b)
                {
                    h = (r - g) / d + 4;
                }

                h /= 6;
            }

            return new HsvColor { Hue = h, Saturation = s, Value = v };
        }

        private static Color GetEffectiveTintColor(Color tintColor, double tintOpacity)
        {
            // Update tintColor's alpha with the combined opacity value
            double tintOpacityModifier = GetTintOpacityModifier(tintColor);

            return new Color((byte)(255 * ((255.0 / tintColor.A) * tintOpacity) * tintOpacityModifier), tintColor.R, tintColor.G, tintColor.B);
        }

        private static double GetTintOpacityModifier(Color tintColor)
        {
            // This method supresses the maximum allowable tint opacity depending on the luminosity and saturation of a color by 
            // compressing the range of allowable values - for example, a user-defined value of 100% will be mapped to 45% for pure 
            // white (100% luminosity), 85% for pure black (0% luminosity), and 90% for pure gray (50% luminosity).  The intensity of 
            // the effect increases linearly as luminosity deviates from 50%.  After this effect is calculated, we cancel it out
            // linearly as saturation increases from zero.

            const double midPoint = 0.5; // Mid point of HsvV range that these calculations are based on. This is here for easy tuning.

            const double whiteMaxOpacity = 0.2; // 100% luminosity
            const double midPointMaxOpacity = 0.45; // 50% luminosity
            const double blackMaxOpacity = 0.45; // 0% luminosity

            var hsv = RgbToHsv(tintColor);

            double opacityModifier = midPointMaxOpacity;

            if (hsv.Value != midPoint)
            {
                // Determine maximum suppression amount
                double lowestMaxOpacity = midPointMaxOpacity;
                double maxDeviation = midPoint;

                if (hsv.Value > midPoint)
                {
                    lowestMaxOpacity = whiteMaxOpacity; // At white (100% hsvV)
                    maxDeviation = 1 - maxDeviation;
                }
                else if (hsv.Value < midPoint)
                {
                    lowestMaxOpacity = blackMaxOpacity; // At black (0% hsvV)
                }

                double maxOpacitySuppression = midPointMaxOpacity - lowestMaxOpacity;

                // Determine normalized deviation from the midpoint
                double deviation = Math.Abs(hsv.Value - midPoint);
                double normalizedDeviation = deviation / maxDeviation;

                // If we have saturation, reduce opacity suppression to allow that color to come through more
                if (hsv.Saturation > 0)
                {
                    // Dampen opacity suppression based on how much saturation there is
                    maxOpacitySuppression *= Math.Max(1 - (hsv.Saturation * 2), 0.0);
                }

                double opacitySuppression = maxOpacitySuppression * normalizedDeviation;

                opacityModifier = midPointMaxOpacity - opacitySuppression;
            }

            return opacityModifier;
        }

        private Color GetEffectiveLuminosityColor()
        {
            double? luminosityOpacity = MaterialOpacity;

            return GetLuminosityColor(luminosityOpacity);
        }

        private static byte Trim(double value)
        {
            value = Math.Min(Math.Floor(value * 256), 255);

            if (value < 0)
            {
                return 0;
            }
            else if (value > 255)
            {
                return 255;
            }

            return (byte)value;
        }

        private static float RGBMax(Color color)
        {
            if (color.R > color.G)
                return (color.R > color.B) ? color.R : color.B;
            else
                return (color.G > color.B) ? color.G : color.B;
        }

        private static float RGBMin(Color color)
        {
            if (color.R < color.G)
                return (color.R < color.B) ? color.R : color.B;
            else
                return (color.G < color.B) ? color.G : color.B;
        }

        // The tintColor passed into this method should be the original, unmodified color created using user values for TintColor + TintOpacity
        private Color GetLuminosityColor(double? luminosityOpacity)
        {
            // Calculate the HSL lightness value of the color.
            var max = (float)RGBMax(TintColor) / 255.0f;
            var min = (float)RGBMin(TintColor) / 255.0f;

            var lightness = (max + min) / 2.0;

            lightness = 1 - ((1 - lightness) * luminosityOpacity.Value);

            lightness = 0.13 + (lightness * 0.74);

            var luminosityColor = new Color(255, Trim(lightness), Trim(lightness), Trim(lightness));

            var compensationMultiplier = 1 - PlatformTransparencyCompensationLevel;
            return new Color((byte)(255 * Math.Max(Math.Min(PlatformTransparencyCompensationLevel + (luminosityOpacity.Value * compensationMultiplier), 1.0), 0.0)), luminosityColor.R, luminosityColor.G, luminosityColor.B);
        }

        /// <summary>
        /// Marks a property as affecting the brush's visual representation.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a brush's static constructor, any change to the
        /// property will cause the <see cref="Invalidated"/> event to be raised on the brush.
        /// </remarks>
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : ExperimentalAcrylicMaterial
        {
            static void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                (e.Sender as T)?.RaiseInvalidated(EventArgs.Empty);
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(e => Invalidate(e));
            }
        }

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);

        public IExperimentalAcrylicMaterial ToImmutable()
        {
            return new ImmutableExperimentalAcrylicMaterial(this);
        }
    }
}
