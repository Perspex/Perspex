using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Int32"/> properties.
    /// </summary>
    public class Int32Animator : Animator<Int32>
    {
        const double maxVal = (double)Int32.MaxValue;

        /// <inheritdocs/>
        public override Int32 Interpolate(double progress, Int32 oldValue, Int32 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (Int32)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
