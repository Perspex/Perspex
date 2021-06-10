using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input.EvDev
{
    internal class EvDevSingleTouchScreen : EvDevDeviceHandler
    {
        private readonly IScreenInfoProvider _screenInfo;
        private readonly int _width, _height;
        private readonly Matrix _calibration;
        private input_absinfo _axisX;
        private input_absinfo _axisY;
        private TouchDevice _device = new TouchDevice();

        private int _currentX, _currentY;
        private bool _hasMovement;
        private bool? _pressAction;

        public EvDevSingleTouchScreen(EvDevDevice device, EvDevTouchScreenDeviceDescription description,
            IScreenInfoProvider screenInfo) : base(device)
        {
            if (device.AbsX == null || device.AbsY == null)
                throw new ArgumentException("Device is not a touchscreen");
            _screenInfo = screenInfo;

            _calibration = description.CalibrationMatrix;
            _axisX = device.AbsX.Value;
            _axisY = device.AbsY.Value;
        }

        protected override void HandleEvent(input_event ev)
        {
            if (ev.Type == EvType.EV_ABS)
            {
                if (ev.Axis == AbsAxis.ABS_X)
                {
                    _currentX = ev.value;
                    _hasMovement = true;
                }

                if (ev.Axis == AbsAxis.ABS_Y)
                {
                    _currentY = ev.value;
                    _hasMovement = true;
                }
            }

            if (ev.Type == EvType.EV_KEY)
            {
                if (ev.Key == EvKey.BTN_TOUCH)
                {
                    _pressAction = ev.value != 0;
                }
            }
            
            if (ev.Type == EvType.EV_SYN)
            {
                if (_pressAction != null)
                    RaiseEvent(_pressAction == true ? RawPointerEventType.TouchBegin : RawPointerEventType.TouchEnd,
                        ev.Timestamp);
                else if(_hasMovement)
                    RaiseEvent(RawPointerEventType.TouchUpdate, ev.Timestamp);
                _hasMovement = false;
                _pressAction = null;
            }
        }

        void RaiseEvent(RawPointerEventType type, ulong timestamp)
        {
            var point = new Point(_currentX, _currentY);

            var touchWidth = _axisX.maximum - _axisX.minimum;
            var touchHeight = _axisY.maximum - _axisY.minimum;

            var screenSize = _screenInfo.ScaledSize;
            
            // Normalize to 0-(max-min)
            point -= new Point(_axisX.minimum, _axisY.minimum);

            // Apply calibration matrix
            point *= _calibration;
            
            // Transform to display pixel grid 
            point = new Point(point.X * screenSize.Width / touchWidth, point.Y * screenSize.Height / touchHeight);
            
            RaiseEvent(new RawTouchEventArgs(_device, timestamp, InputRoot,
                type, point, RawInputModifiers.None, 1));
        }
    }

    internal abstract class EvDevDeviceHandler
    {
        public event Action<RawInputEventArgs> OnEvent;
        public EvDevDeviceHandler(EvDevDevice device)
        {
            Device = device;
        }

        public EvDevDevice Device { get; }
        public IInputRoot InputRoot { get; set; }

        public void HandleEvents()
        {
            input_event? ev;
            while ((ev = Device.NextEvent()) != null) 
                HandleEvent(ev.Value);
        }

        protected void RaiseEvent(RawInputEventArgs ev) => OnEvent?.Invoke(ev);

        protected abstract void HandleEvent(input_event ev);
    }
}
