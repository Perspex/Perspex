using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using static Avalonia.LinuxFramebuffer.NativeUnsafeMethods;

namespace Avalonia.LinuxFramebuffer.Input.EvDev
{
    public class EvDevBackend : IInputBackend
    {
        private readonly EvDevDeviceDescription[] _deviceDescriptions;
        private readonly List<EvDevDeviceHandler> _handlers = new List<EvDevDeviceHandler>();
        private int _epoll;
        private Queue<RawInputEventArgs> _inputQueue = new Queue<RawInputEventArgs>();
        private bool _isQueueHandlerTriggered;
        private object _lock = new object();
        private Action<RawInputEventArgs> _onInput;
        private IInputRoot _inputRoot;

        public EvDevBackend(EvDevDeviceDescription[] devices)
        {
            _deviceDescriptions = devices;
        }
        
        unsafe void InputThread()
        {
            const int MaxEvents = 16;
            var events = stackalloc epoll_event[MaxEvents];
            while (true)
            {
                var eventCount = Math.Min(MaxEvents, epoll_wait(_epoll, events, MaxEvents, 1000));
                for (var c = 0; c < eventCount; c++)
                {
                    try
                    {
                        var ev = events[c];
                        var handler = _handlers[(int)ev.data.u32];
                        handler.HandleEvents();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
        }

        private void OnRawEvent(RawInputEventArgs obj)
        {
            lock (_lock)
            {
                _inputQueue.Enqueue(obj);
                TriggerQueueHandler();
            }
                
        }
        
        void TriggerQueueHandler()
        {
            if (_isQueueHandlerTriggered)
                return;
            _isQueueHandlerTriggered = true;
            Dispatcher.UIThread.Post(InputQueueHandler, DispatcherPriority.Input);

        }
        
        void InputQueueHandler()
        {
            RawInputEventArgs ev;
            lock (_lock)
            {
                _isQueueHandlerTriggered = false;
                if(_inputQueue.Count == 0)
                    return;
                ev = _inputQueue.Dequeue();
            }

            _onInput?.Invoke(ev);

            lock (_lock)
            {
                if (_inputQueue.Count > 0)
                    TriggerQueueHandler();
            }
        }
        
        public void Initialize(IScreenInfoProvider info, Action<RawInputEventArgs> onInput)
        {
            _onInput = onInput;
            _epoll = epoll_create1(0);
            for (var c = 0; c < _deviceDescriptions.Length; c++)
            {
                var description = _deviceDescriptions[c];
                var dev = EvDevDevice.Open(description.Path);
                EvDevDeviceHandler handler;
                if (description is EvDevTouchScreenDeviceDescription touch)
                    handler = new EvDevSingleTouchScreen(dev, touch, info) { InputRoot = _inputRoot };
                else
                    throw new Exception("Unknown device description type " + description.GetType().FullName);

                handler.OnEvent += OnRawEvent;
                _handlers.Add(handler);

                var ev = new epoll_event { events = EPOLLIN, data = { u32 = (uint)c } };
                epoll_ctl(_epoll, EPOLL_CTL_ADD, dev.Fd, ref ev);
            }

            new Thread(InputThread) { IsBackground = true }.Start();
        }

        public void SetInputRoot(IInputRoot root)
        {
            _inputRoot = root;
            foreach (var h in _handlers)
                h.InputRoot = root;
        }


        public static EvDevBackend CreateFromEnvironment()
        {
            var env = Environment.GetEnvironmentVariables();
            var deviceDescriptions = new List<EvDevDeviceDescription>();
            foreach (string key in env.Keys)
            {
                if (key.StartsWith("AVALONIA_EVDEV_DEVICE_"))
                {
                    var value = (string)env[key];
                    deviceDescriptions.Add(EvDevDeviceDescription.ParseFromEnv(value));
                }
            }

            if (deviceDescriptions.Count == 0)
                throw new Exception(
                    "No device device description found, specify devices by adding AVALONIA_EVDEV_DEVICE_{name} environment variables");

            return new EvDevBackend(deviceDescriptions.ToArray());
        }
    }
}
