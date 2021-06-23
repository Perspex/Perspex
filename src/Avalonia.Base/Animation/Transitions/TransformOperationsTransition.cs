using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;
using Avalonia.Media.Transformation;

#nullable enable

namespace Avalonia.Animation
{
    public class TransformOperationsTransition : Transition<ITransform>
    {
        private static readonly TransformOperationsAnimator s_operationsAnimator = new TransformOperationsAnimator();

        public override IObservable<ITransform> DoTransition(
            IObservable<double> progress,
            ITransform oldValue,
            ITransform newValue)
        {
            var oldTransform = TransformOperationsAnimator.EnsureOperations(oldValue);
            var newTransform = TransformOperationsAnimator.EnsureOperations(newValue);

            return new AnimatorTransitionObservable<TransformOperations, TransformOperationsAnimator>(
                s_operationsAnimator, progress, Easing, oldTransform, newTransform);
        }
    }
}
