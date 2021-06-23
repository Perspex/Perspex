using System;

namespace Avalonia.Media
{
    public readonly struct ImmutableExperimentalAcrylicMaterial : IExperimentalAcrylicMaterial, IEquatable<ImmutableExperimentalAcrylicMaterial>
    {
        public ImmutableExperimentalAcrylicMaterial(IExperimentalAcrylicMaterial brush)
        {
            BackgroundSource = brush.BackgroundSource;
            TintColor = brush.TintColor;
            TintOpacity = brush.TintOpacity;
            FallbackColor = brush.FallbackColor;
            MaterialColor = brush.MaterialColor;
        }

        public AcrylicBackgroundSource BackgroundSource { get; }

        public Color TintColor { get; }

        public Color MaterialColor { get; }

        public double TintOpacity { get; }

        public Color FallbackColor { get; }

        public bool Equals(ImmutableExperimentalAcrylicMaterial other)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return
                TintColor == other.TintColor &&                
                TintOpacity == other.TintOpacity &&
                BackgroundSource == other.BackgroundSource &&
                FallbackColor == other.FallbackColor && MaterialColor == other.MaterialColor;

        }

        public override bool Equals(object obj)
        {
            return obj is ImmutableExperimentalAcrylicMaterial other && Equals(other);
        }

        public Color GetEffectiveTintColor()
        {
            return TintColor;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 23) + TintColor.GetHashCode();                
                hash = (hash * 23) + TintOpacity.GetHashCode();
                hash = (hash * 23) + BackgroundSource.GetHashCode();
                hash = (hash * 23) + FallbackColor.GetHashCode();
                hash = (hash * 23) + MaterialColor.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(ImmutableExperimentalAcrylicMaterial left, ImmutableExperimentalAcrylicMaterial right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImmutableExperimentalAcrylicMaterial left, ImmutableExperimentalAcrylicMaterial right)
        {
            return !left.Equals(right);
        }
    }
}
