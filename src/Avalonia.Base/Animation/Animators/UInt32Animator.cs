using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="UInt32"/> properties.
    /// </summary>
    public class UInt32Animator : Animator<UInt32>
    {
        const double maxVal = (double)UInt32.MaxValue;

        /// <inheritdocs/>
        public override UInt32 Interpolate(double progress, UInt32 oldValue, UInt32 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (UInt32)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
