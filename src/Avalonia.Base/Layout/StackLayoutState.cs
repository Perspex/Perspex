// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents the state of a StackLayout.
    /// </summary>
    public class StackLayoutState
    {
        private const int BufferSize = 100;
        private readonly List<double> _estimationBuffer = new List<double>();

        internal FlowLayoutAlgorithm FlowAlgorithm { get; } = new FlowLayoutAlgorithm();
        internal double MaxArrangeBounds { get; private set; }
        internal int TotalElementsMeasured { get; private set; }
        internal double TotalElementSize { get; private set; }

        internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
        {
            FlowAlgorithm.InitializeForContext(context, callbacks);

            if (_estimationBuffer.Count == 0)
            {
                _estimationBuffer.AddRange(Enumerable.Repeat(0.0, BufferSize));
            }

            context.LayoutState = this;
        }

        internal void UninitializeForContext(VirtualizingLayoutContext context)
        {
            FlowAlgorithm.UninitializeForContext(context);
        }

        internal void OnElementMeasured(int elementIndex, double majorSize, double minorSize)
        {
            int estimationBufferIndex = elementIndex % _estimationBuffer.Count;
            bool alreadyMeasured = _estimationBuffer[estimationBufferIndex] != 0;

            if (!alreadyMeasured)
            {
                TotalElementsMeasured++;
            }

            TotalElementSize -= _estimationBuffer[estimationBufferIndex];
            TotalElementSize += majorSize;
            _estimationBuffer[estimationBufferIndex] = majorSize;

            MaxArrangeBounds = Math.Max(MaxArrangeBounds, minorSize);
        }

        internal void OnMeasureStart() => MaxArrangeBounds = 0;
    }
}
