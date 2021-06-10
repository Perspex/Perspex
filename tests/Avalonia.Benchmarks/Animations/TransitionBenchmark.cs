using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Layout;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Animations
{
    [MemoryDiagnoser]
    public class TransitionBenchmark
    {
        private readonly AddValueObserver _observer;
        private readonly List<double> _producedValues;
        private readonly Subject<double> _timeProducer;
        private readonly DoubleTransition _transition;

        public TransitionBenchmark()
        {
            _transition = new DoubleTransition
            {
                Duration = TimeSpan.FromMilliseconds(FrameCount), Property = Layoutable.WidthProperty
            };

            _timeProducer = new Subject<double>();
            _producedValues = new List<double>(FrameCount);

            _observer = new AddValueObserver(_producedValues);
        }

        [Params(10, 100)] public int FrameCount { get; set; }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NewTransition()
        {
            var transitionObs = _transition.DoTransition(_timeProducer, 0, 1);

            _producedValues.Clear();

            using var transitionSub = transitionObs.Subscribe(_observer);

            for (int i = 0; i < FrameCount; i++)
            {
                _timeProducer.OnNext(i / 1000d);
            }

            Debug.Assert(_producedValues.Count == FrameCount);
        }

        private class AddValueObserver : IObserver<double>
        {
            private readonly List<double> _values;

            public AddValueObserver(List<double> values)
            {
                _values = values;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(double value)
            {
                _values.Add(value);
            }
        }
    }
}
