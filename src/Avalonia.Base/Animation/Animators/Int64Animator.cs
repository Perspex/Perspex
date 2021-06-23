using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Int64"/> properties.
    /// </summary>
    public class Int64Animator : Animator<Int64>
    {
        const double maxVal = (double)Int64.MaxValue;

        /// <inheritdocs/>
        public override Int64 Interpolate(double progress, Int64 oldValue, Int64 newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (Int64)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
