using System.Runtime.InteropServices;

namespace Avalonia.Media.Transformation
{
    /// <summary>
    /// Represents a single primitive transform (like translation, rotation, scale, etc.).
    /// </summary>
    public struct TransformOperation
    {
        public OperationType Type;
        public Matrix Matrix;
        public DataLayout Data;

        public enum OperationType
        {
            Translate,
            Rotate,
            Scale,
            Skew,
            Matrix,
            Identity
        }

        /// <summary>
        /// Returns whether operation produces the identity matrix.
        /// </summary>
        public bool IsIdentity => Matrix.IsIdentity;

        /// <summary>
        /// Bakes this operation to a transform matrix.
        /// </summary>
        public void Bake()
        {
            Matrix = Matrix.Identity;

            switch (Type)
            {
                case OperationType.Translate:
                {
                    Matrix = Matrix.CreateTranslation(Data.Translate.X, Data.Translate.Y);

                    break;
                }
                case OperationType.Rotate:
                {
                    Matrix = Matrix.CreateRotation(Data.Rotate.Angle);

                    break;
                }
                case OperationType.Scale:
                {
                    Matrix = Matrix.CreateScale(Data.Scale.X, Data.Scale.Y);

                    break;
                }
                case OperationType.Skew:
                {
                    Matrix = Matrix.CreateSkew(Data.Skew.X, Data.Skew.Y);

                    break;
                }
            }
        }

        /// <summary>
        /// Returns new identity transform operation.
        /// </summary>
        public static TransformOperation Identity =>
            new TransformOperation { Matrix = Matrix.Identity, Type = OperationType.Identity };

        /// <summary>
        /// Attempts to interpolate between two transform operations.
        /// </summary>
        /// <param name="from">Source operation.</param>
        /// <param name="to">Target operation.</param>
        /// <param name="progress">Interpolation progress.</param>
        /// <param name="result">Interpolation result that will be filled in when operation was successful.</param>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/css-transforms-1/#interpolation-of-transform-functions.
        /// </remarks>
        public static bool TryInterpolate(TransformOperation? from, TransformOperation? to, double progress,
            ref TransformOperation result)
        {
            bool fromIdentity = IsOperationIdentity(ref from);
            bool toIdentity = IsOperationIdentity(ref to);

            if (fromIdentity && toIdentity)
            {
                return true;
            }

            // ReSharper disable PossibleInvalidOperationException
            TransformOperation fromValue = fromIdentity ? Identity : from.Value;
            TransformOperation toValue = toIdentity ? Identity : to.Value;
            // ReSharper restore PossibleInvalidOperationException

            var interpolationType = toIdentity ? fromValue.Type : toValue.Type;

            result.Type = interpolationType;

            switch (interpolationType)
            {
                case OperationType.Translate:
                {
                    double fromX = fromIdentity ? 0 : fromValue.Data.Translate.X;
                    double fromY = fromIdentity ? 0 : fromValue.Data.Translate.Y;

                    double toX = toIdentity ? 0 : toValue.Data.Translate.X;
                    double toY = toIdentity ? 0 : toValue.Data.Translate.Y;

                    result.Data.Translate.X = InterpolationUtilities.InterpolateScalars(fromX, toX, progress);
                    result.Data.Translate.Y = InterpolationUtilities.InterpolateScalars(fromY, toY, progress);

                    result.Bake();

                    break;
                }
                case OperationType.Rotate:
                {
                    double fromAngle = fromIdentity ? 0 : fromValue.Data.Rotate.Angle;

                    double toAngle = toIdentity ? 0 : toValue.Data.Rotate.Angle;

                    result.Data.Rotate.Angle = InterpolationUtilities.InterpolateScalars(fromAngle, toAngle, progress);

                    result.Bake();

                    break;
                }
                case OperationType.Scale:
                {
                    double fromX = fromIdentity ? 1 : fromValue.Data.Scale.X;
                    double fromY = fromIdentity ? 1 : fromValue.Data.Scale.Y;

                    double toX = toIdentity ? 1 : toValue.Data.Scale.X;
                    double toY = toIdentity ? 1 : toValue.Data.Scale.Y;

                    result.Data.Scale.X = InterpolationUtilities.InterpolateScalars(fromX, toX, progress);
                    result.Data.Scale.Y = InterpolationUtilities.InterpolateScalars(fromY, toY, progress);

                    result.Bake();

                    break;
                }
                case OperationType.Skew:
                {
                    double fromX = fromIdentity ? 0 : fromValue.Data.Skew.X;
                    double fromY = fromIdentity ? 0 : fromValue.Data.Skew.Y;

                    double toX = toIdentity ? 0 : toValue.Data.Skew.X;
                    double toY = toIdentity ? 0 : toValue.Data.Skew.Y;

                    result.Data.Skew.X = InterpolationUtilities.InterpolateScalars(fromX, toX, progress);
                    result.Data.Skew.Y = InterpolationUtilities.InterpolateScalars(fromY, toY, progress);

                    result.Bake();

                    break;
                }
                case OperationType.Matrix:
                {
                    var fromMatrix = fromIdentity ? Matrix.Identity : fromValue.Matrix;
                    var toMatrix = toIdentity ? Matrix.Identity : toValue.Matrix;

                    if (!Matrix.TryDecomposeTransform(fromMatrix, out Matrix.Decomposed fromDecomposed) ||
                        !Matrix.TryDecomposeTransform(toMatrix, out Matrix.Decomposed toDecomposed))
                    {
                        return false;
                    }

                    var interpolated =
                        InterpolationUtilities.InterpolateDecomposedTransforms(
                            ref fromDecomposed, ref toDecomposed,
                            progress);

                    result.Matrix = InterpolationUtilities.ComposeTransform(interpolated);

                    break;
                }
                case OperationType.Identity:
                {
                    // Do nothing.
                    break;
                }
            }

            return true;
        }

        private static bool IsOperationIdentity(ref TransformOperation? operation)
        {
            return !operation.HasValue || operation.Value.IsIdentity;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DataLayout
        {
            [FieldOffset(0)] public SkewLayout Skew;

            [FieldOffset(0)] public ScaleLayout Scale;

            [FieldOffset(0)] public TranslateLayout Translate;

            [FieldOffset(0)] public RotateLayout Rotate;

            public struct SkewLayout
            {
                public double X;
                public double Y;
            }

            public struct ScaleLayout
            {
                public double X;
                public double Y;
            }

            public struct TranslateLayout
            {
                public double X;
                public double Y;
            }

            public struct RotateLayout
            {
                public double Angle;
            }
        }
    }
}
