using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Int16"/> properties.
    /// </summary>
    public class Int16Animator : Animator<Int16>
    {
        const double maxVal = (double)Int16.MaxValue;

        /// <inheritdocs/>
        public override Int16 Interpolate(double progress, Int16 oldValue, Int16 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (Int16)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
