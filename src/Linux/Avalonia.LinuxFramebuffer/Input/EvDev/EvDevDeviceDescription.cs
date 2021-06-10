using System;
using System.Linq;

namespace Avalonia.LinuxFramebuffer.Input.EvDev
{
    public abstract class EvDevDeviceDescription
    {
        protected internal EvDevDeviceDescription()
        {
            
        }
        
        public string Path { get; set; }

        internal static EvDevDeviceDescription ParseFromEnv(string env)
        {
            var formatEx = new ArgumentException(
                "Invalid device format, expected `(path):type=(touchscreen):[calibration=m11,m12,m21,m22,m31,m32]");


            var items = env.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length < 2)
                throw formatEx;
            var path = items[0];
            var dic = items.Skip(1)
                .Select(i => i.Split(new[] { '=' }, 2))
                .ToDictionary(x => x[0], x => x[1]);

            if (!dic.TryGetValue("type", out var type))
                throw formatEx;

            if (type == "touchscreen")
                return EvDevTouchScreenDeviceDescription.ParseFromEnv(path, dic);
            
            throw formatEx;
        }
    }
}
