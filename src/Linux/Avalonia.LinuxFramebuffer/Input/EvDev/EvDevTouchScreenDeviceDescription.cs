using System.Collections.Generic;

namespace Avalonia.LinuxFramebuffer.Input.EvDev
{
    public sealed class EvDevTouchScreenDeviceDescription : EvDevDeviceDescription
    {
        public Matrix CalibrationMatrix { get; set; } = Matrix.Identity;

        internal static EvDevTouchScreenDeviceDescription ParseFromEnv(string path, Dictionary<string, string> options)
        {
            var calibrationMatrix = Matrix.Identity;
            if (options.TryGetValue("calibration", out var calibration))
                calibrationMatrix = Matrix.Parse(calibration);
            
            return new EvDevTouchScreenDeviceDescription { Path = path, CalibrationMatrix = calibrationMatrix };
        }
    }
}
