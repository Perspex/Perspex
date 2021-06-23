using System;
using System.Collections.Generic;

namespace Avalonia.Media.Transformation
{
    /// <summary>
    /// Contains a list of <see cref="TransformOperation"/> that represent primitive transforms that will be
    /// applied in declared order.
    /// </summary>
    public sealed class TransformOperations : ITransform
    {
        public static TransformOperations Identity { get; } = new TransformOperations(new List<TransformOperation>());

        private readonly List<TransformOperation> _operations;

        private TransformOperations(List<TransformOperation> operations)
        {
            _operations = operations ?? throw new ArgumentNullException(nameof(operations));

            IsIdentity = CheckIsIdentity();

            Value = ApplyTransforms();
        }

        /// <summary>
        /// Returns whether all operations combined together produce the identity matrix.
        /// </summary>
        public bool IsIdentity { get; }

        public IReadOnlyList<TransformOperation> Operations => _operations;

        public Matrix Value { get; }

        public static TransformOperations Parse(string s)
        {
            return TransformParser.Parse(s);
        }

        public static Builder CreateBuilder(int capacity)
        {
            return new Builder(capacity);
        }

        public static TransformOperations Interpolate(TransformOperations from, TransformOperations to, double progress)
        {
            TransformOperations result = Identity;

            if (!TryInterpolate(from, to, progress, ref result))
            {
                // If the matrices cannot be interpolated, fallback to discrete animation logic.
                // See https://drafts.csswg.org/css-transforms/#matrix-interpolation
                result = progress < 0.5 ? from : to;
            }

            return result;
        }

        private Matrix ApplyTransforms(int startOffset = 0)
        {
            Matrix matrix = Matrix.Identity;

            for (var i = startOffset; i < _operations.Count; i++)
            {
                TransformOperation operation = _operations[i];
                matrix *= operation.Matrix;
            }

            return matrix;
        }

        private bool CheckIsIdentity()
        {
            foreach (TransformOperation operation in _operations)
            {
                if (!operation.IsIdentity)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryInterpolate(TransformOperations from, TransformOperations to, double progress, ref TransformOperations result)
        {
            bool fromIdentity = from.IsIdentity;
            bool toIdentity = to.IsIdentity;

            if (fromIdentity && toIdentity)
            {
                return true;
            }

            int matchingPrefixLength = ComputeMatchingPrefixLength(from, to);
            int fromSize = fromIdentity ? 0 : from._operations.Count;
            int toSize = toIdentity ? 0 : to._operations.Count;
            int numOperations = Math.Max(fromSize, toSize);

            var builder = new Builder(matchingPrefixLength);

            for (int i = 0; i < matchingPrefixLength; i++)
            {
                TransformOperation interpolated = new TransformOperation
                {
                    Type = TransformOperation.OperationType.Identity
                };

                if (!TransformOperation.TryInterpolate(
                    i >= fromSize ? default(TransformOperation?) : from._operations[i],
                    i >= toSize ? default(TransformOperation?) : to._operations[i],
                    progress,
                    ref interpolated))
                {
                    return false;
                }

                builder.Append(interpolated);
            }

            if (matchingPrefixLength < numOperations)
            {
                if (!ComputeDecomposedTransform(from, matchingPrefixLength, out Matrix.Decomposed fromDecomposed) ||
                    !ComputeDecomposedTransform(to, matchingPrefixLength, out Matrix.Decomposed toDecomposed))
                {
                    return false;
                }

                var transform = InterpolationUtilities.InterpolateDecomposedTransforms(ref fromDecomposed, ref toDecomposed, progress);

                builder.AppendMatrix(InterpolationUtilities.ComposeTransform(transform));
            }

            result = builder.Build();

            return true;
        }

        private static bool ComputeDecomposedTransform(TransformOperations operations, int startOffset, out Matrix.Decomposed decomposed)
        {
            Matrix transform = operations.ApplyTransforms(startOffset);

            if (!Matrix.TryDecomposeTransform(transform, out decomposed))
            {
                return false;
            }

            return true;
        }

        private static int ComputeMatchingPrefixLength(TransformOperations from, TransformOperations to)
        {
            int numOperations = Math.Min(from._operations.Count, to._operations.Count);

            for (int i = 0; i < numOperations; i++)
            {
                if (from._operations[i].Type != to._operations[i].Type)
                {
                    return i;
                }
            }

            // If the operations match to the length of the shorter list, then pad its
            // length with the matching identity operations.
            // https://drafts.csswg.org/css-transforms/#transform-function-lists
            return Math.Max(from._operations.Count, to._operations.Count);
        }

        public readonly struct Builder
        {
            private readonly List<TransformOperation> _operations;

            public Builder(int capacity)
            {
                _operations = new List<TransformOperation>(capacity);
            }

            public void AppendTranslate(double x, double y)
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Translate;
                toAdd.Data.Translate.X = x;
                toAdd.Data.Translate.Y = y;

                toAdd.Bake();

                _operations.Add(toAdd);
            }

            public void AppendRotate(double angle)
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Rotate;
                toAdd.Data.Rotate.Angle = angle;

                toAdd.Bake();

                _operations.Add(toAdd);
            }

            public void AppendScale(double x, double y)
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Scale;
                toAdd.Data.Scale.X = x;
                toAdd.Data.Scale.Y = y;

                toAdd.Bake();

                _operations.Add(toAdd);
            }

            public void AppendSkew(double x, double y)
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Skew;
                toAdd.Data.Skew.X = x;
                toAdd.Data.Skew.Y = y;

                toAdd.Bake();

                _operations.Add(toAdd);
            }

            public void AppendMatrix(Matrix matrix)
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Matrix;
                toAdd.Matrix = matrix;

                _operations.Add(toAdd);
            }

            public void AppendIdentity()
            {
                var toAdd = new TransformOperation();

                toAdd.Type = TransformOperation.OperationType.Identity;

                _operations.Add(toAdd);
            }

            public void Append(TransformOperation toAdd)
            {
                _operations.Add(toAdd);
            }

            public TransformOperations Build()
            {
                return new TransformOperations(_operations);
            }
        }
    }
}
