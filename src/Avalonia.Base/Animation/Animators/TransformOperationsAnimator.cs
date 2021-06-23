using System;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    public class TransformOperationsAnimator : Animator<TransformOperations>
    {
        public TransformOperationsAnimator()
        {
            Validate = ValidateTransform;
        }

        public override TransformOperations Interpolate(double progress, TransformOperations oldValue, TransformOperations newValue)
        {
            var oldTransform = EnsureOperations(oldValue);
            var newTransform = EnsureOperations(newValue);

            return TransformOperations.Interpolate(oldTransform, newTransform, progress);
        }

        internal static TransformOperations EnsureOperations(ITransform value)
        {
            return value as TransformOperations ?? TransformOperations.Identity;
        }

        private void ValidateTransform(AnimatorKeyFrame kf)
        {
            if (!(kf.Value is TransformOperations))
            {
                throw new InvalidOperationException($"All keyframes must be of type {typeof(TransformOperations)}.");
            }
        }
    }
}
