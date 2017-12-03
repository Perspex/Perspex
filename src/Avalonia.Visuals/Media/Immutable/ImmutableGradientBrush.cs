﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush that draws with a gradient.
    /// </summary>
    public abstract class ImmutableGradientBrush : IGradientBrush, IImmutableBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGradientBrush"/> class.
        /// </summary>
        /// <param name="gradientStops">The gradient stops.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="spreadMethod">The spread method.</param>
        protected ImmutableGradientBrush(
            IList<GradientStop> gradientStops,
            double opacity,
            GradientSpreadMethod spreadMethod)
        {
            GradientStops = gradientStops;
            Opacity = opacity;
            SpreadMethod = spreadMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGradientBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        protected ImmutableGradientBrush(IGradientBrush source)
            : this(source.GradientStops.ToList(), source.Opacity, source.SpreadMethod)
        {

        }

        /// <inheritdoc/>
        public IList<GradientStop> GradientStops { get; }

        /// <inheritdoc/>
        public double Opacity { get; }

        /// <inheritdoc/>
        public GradientSpreadMethod SpreadMethod { get; }

        /// <inheritdoc/>
        public abstract IImmutableBrush WithOpacity(double opacity);
    }
}
