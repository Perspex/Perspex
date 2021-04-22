using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;

namespace Avalonia.Rendering
{
    public class RendererBase
    {
        private readonly bool _useManualFpsCounting;
        private static int s_fontSize = 18;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _framesThisSecond;
        private int _fps;
        private TimeSpan _lastFpsUpdate;

        public RendererBase(bool useManualFpsCounting = false)
        {
            _useManualFpsCounting = useManualFpsCounting;
        }

        protected void FpsTick() => _framesThisSecond++;

        protected void RenderFps(DrawingContext context, Rect clientRect, int? layerCount)
        {
            var now = _stopwatch.Elapsed;
            var elapsed = now - _lastFpsUpdate;

            if (!_useManualFpsCounting)
                ++_framesThisSecond;

            if (elapsed.TotalSeconds > 1)
            {
                _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
                _framesThisSecond = 0;
                _lastFpsUpdate = now;
            }

            var text = layerCount.HasValue ? $"Layers: {layerCount} FPS: {_fps:000}" : $"FPS: {_fps:000}";

            var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, s_fontSize, Brushes.White);

            var rect = new Rect(clientRect.Right - formattedText.Width, 0, formattedText.Width, formattedText.Height);

            context.DrawRectangle(Brushes.Black, null, rect);

            context.DrawText(formattedText, rect.TopLeft);
        }
    }
}
