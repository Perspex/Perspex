using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    public class RendererBase
    {
        private readonly bool _useManualFpsCounting;
        private static int s_fontSize = 18;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _framesThisSecond;
        private int _fps;
        private FormattedText _fpsText;
        private TimeSpan _lastFpsUpdate;

        public RendererBase(bool useManualFpsCounting = false)
        {
            _useManualFpsCounting = useManualFpsCounting;
            _fpsText = new FormattedText
            {
                Typeface = new Typeface(FontFamily.Default),
                FontSize = s_fontSize
            };
        }

        protected void FpsTick() => _framesThisSecond++;

        protected void RenderFps(IDrawingContextImpl context, Rect clientRect, int? layerCount)
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

            if (layerCount.HasValue)
            {
                _fpsText.Text = string.Format("Layers: {0} FPS: {1:000}", layerCount, _fps);
            }
            else
            {
                _fpsText.Text = string.Format("FPS: {0:000}", _fps);
            }

            var size = _fpsText.Bounds.Size;
            var rect = new Rect(clientRect.Right - size.Width, 0, size.Width, size.Height);

            context.Transform = Matrix.Identity;
            context.DrawRectangle(Brushes.Black,null, rect);
            context.DrawText(Brushes.White, rect.TopLeft, _fpsText.PlatformImpl);
        }
    }
}
