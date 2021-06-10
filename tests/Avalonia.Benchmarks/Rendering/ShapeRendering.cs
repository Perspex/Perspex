using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    [MemoryDiagnoser]
    public class ShapeRendering
    {
        private readonly DrawingContext _drawingContext;
        private readonly Line _lineFill;
        private readonly Line _lineFillAndStroke;
        private readonly Line _lineNoBrushes;
        private readonly Line _lineStroke;

        public ShapeRendering()
        {
            _lineNoBrushes = new Line();
            _lineStroke = new Line { Stroke = new SolidColorBrush() };
            _lineFill = new Line { Fill = new SolidColorBrush() };
            _lineFillAndStroke = new Line { Stroke = new SolidColorBrush(), Fill = new SolidColorBrush() };

            _drawingContext = new DrawingContext(new NullDrawingContextImpl(), true);

            AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(new NullRenderingPlatform());
        }

        [Benchmark]
        public void Render_Line_NoBrushes()
        {
            _lineNoBrushes.Render(_drawingContext);
        }

        [Benchmark]
        public void Render_Line_WithStroke()
        {
            _lineStroke.Render(_drawingContext);
        }

        [Benchmark]
        public void Render_Line_WithFill()
        {
            _lineFill.Render(_drawingContext);
        }

        [Benchmark]
        public void Render_Line_WithFillAndStroke()
        {
            _lineFillAndStroke.Render(_drawingContext);
        }
    }
}
