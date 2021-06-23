namespace Avalonia.Media.Transformation
{
    internal static class InterpolationUtilities
    {
        public static double InterpolateScalars(double from, double to, double progress)
        {
            return from * (1d - progress) + to * progress;
        }

        public static Vector InterpolateVectors(Vector from, Vector to, double progress)
        {
            var x = InterpolateScalars(from.X, to.X, progress);
            var y = InterpolateScalars(from.Y, to.Y, progress);

            return new Vector(x, y);
        }

        public static Matrix ComposeTransform(Matrix.Decomposed decomposed)
        {
            // According to https://www.w3.org/TR/css-transforms-1/#recomposing-to-a-2d-matrix

            return Matrix.CreateTranslation(decomposed.Translate) *
                   Matrix.CreateRotation(decomposed.Angle) *
                   Matrix.CreateSkew(decomposed.Skew.X, decomposed.Skew.Y) *
                   Matrix.CreateScale(decomposed.Scale);
        }

        public static Matrix.Decomposed InterpolateDecomposedTransforms(ref Matrix.Decomposed from, ref Matrix.Decomposed to, double progres)
        {
            Matrix.Decomposed result = default;

            result.Translate = InterpolateVectors(from.Translate, to.Translate, progres);
            result.Scale = InterpolateVectors(from.Scale, to.Scale, progres);
            result.Skew = InterpolateVectors(from.Skew, to.Skew, progres);
            result.Angle = InterpolateScalars(from.Angle, to.Angle, progres);

            return result;
        }
    }
}
