using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="UInt16"/> properties.
    /// </summary>
    public class UInt16Animator : Animator<UInt16>
    {
        const double maxVal = (double)UInt16.MaxValue;

        /// <inheritdocs/>
        public override UInt16 Interpolate(double progress, UInt16 oldValue, UInt16 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (UInt16)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
