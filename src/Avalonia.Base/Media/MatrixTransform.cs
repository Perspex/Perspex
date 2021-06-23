using System;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Transforms an <see cref="IVisual"/> according to a <see cref="Matrix"/>.
    /// </summary>
    public class MatrixTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="Matrix"/> property.
        /// </summary>
        public static readonly StyledProperty<Matrix> MatrixProperty =
            AvaloniaProperty.Register<MatrixTransform, Matrix>(nameof(Matrix), Matrix.Identity);

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixTransform"/> class.
        /// </summary>
        public MatrixTransform()
        {
            this.GetObservable(MatrixProperty).Subscribe(_ => RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixTransform"/> class.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        public MatrixTransform(Matrix matrix)
            : this()
        {
            Matrix = matrix;
        }

        /// <summary>
        /// Gets or sets the matrix.
        /// </summary>
        public Matrix Matrix
        {
            get { return GetValue(MatrixProperty); }
            set { SetValue(MatrixProperty, value); }
        }

        /// <summary>
        /// Gets the matrix.
        /// </summary>
        public override Matrix Value => Matrix;
    }
}
