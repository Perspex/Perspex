using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="UInt64"/> properties.
    /// </summary>
    public class UInt64Animator : Animator<UInt64>
    {
        const double maxVal = (double)UInt64.MaxValue;

        /// <inheritdocs/>
        public override UInt64 Interpolate(double progress, UInt64 oldValue, UInt64 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (UInt64)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
