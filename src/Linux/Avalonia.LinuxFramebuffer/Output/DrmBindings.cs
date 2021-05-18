using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static Avalonia.LinuxFramebuffer.NativeUnsafeMethods;
using static Avalonia.LinuxFramebuffer.Output.LibDrm;

namespace Avalonia.LinuxFramebuffer.Output
{
    public unsafe class DrmConnector
    {
        private static string[] KnownConnectorTypes =
        {
            "None", "VGA", "DVI-I", "DVI-D", "DVI-A", "Composite", "S-Video", "LVDS", "Component", "DIN",
            "DisplayPort", "HDMI-A", "HDMI-B", "TV", "eDP", "Virtual", "DSI"
        };

        public DrmModeConnection Connection { get; }

        public bool IsConnected => Connection == DrmModeConnection.DRM_MODE_CONNECTED;
        
        public uint Id { get; }
        public string Name { get; }
        public Size SizeMm { get; }
        public DrmModeSubPixel SubPixel { get; }
        internal uint EncoderId { get; }
        internal List<uint> EncoderIds { get; } = new List<uint>();
        public List<DrmModeInfo> Modes { get; } = new List<DrmModeInfo>();
        internal DrmConnector(drmModeConnector* conn)
        {
            Connection = conn->connection;
            Id = conn->connector_id;
            SizeMm = new Size(conn->mmWidth, conn->mmHeight);
            SubPixel = conn->subpixel;
            for (var c = 0; c < conn->count_encoders;c++) 
                EncoderIds.Add(conn->encoders[c]);
            EncoderId = conn->encoder_id;
            for(var c=0; c<conn->count_modes; c++)
                Modes.Add(new DrmModeInfo(ref conn->modes[c]));

            if (conn->connector_type > KnownConnectorTypes.Length - 1)
                Name = $"Unknown({conn->connector_type})-{conn->connector_type_id}";
            else
                Name = KnownConnectorTypes[conn->connector_type] + "-" + conn->connector_type_id;
        }
    }

    public unsafe class DrmModeInfo
    {
        internal drmModeModeInfo Mode;

        internal DrmModeInfo(ref drmModeModeInfo info)
        {
            Mode = info;
            fixed (void* pName = info.name)
                Name = Marshal.PtrToStringAnsi(new IntPtr(pName));
        }

        public PixelSize Resolution => new PixelSize(Mode.hdisplay, Mode.vdisplay);
        public bool IsPreferred => Mode.type.HasAllFlags(DrmModeType.DRM_MODE_TYPE_PREFERRED);

        public string Name { get; }
    }

    unsafe class DrmEncoder
    {
        public drmModeEncoder Encoder { get; }
        public List<drmModeCrtc> PossibleCrtcs { get; } = new List<drmModeCrtc>();

        public DrmEncoder(drmModeEncoder encoder, drmModeCrtc[] crtcs)
        {
            Encoder = encoder;
            for (var c = 0; c < crtcs.Length; c++)
            {
                var bit = 1 << c;
                if ((encoder.possible_crtcs & bit) != 0)
                    PossibleCrtcs.Add(crtcs[c]);
            }
        }
    }
    
    
    
    public unsafe class DrmResources
    {
        public List<DrmConnector> Connectors { get; }= new List<DrmConnector>();
        internal Dictionary<uint, DrmEncoder> Encoders { get; } = new Dictionary<uint, DrmEncoder>();
        public DrmResources(int fd)
        {
            var res = drmModeGetResources(fd);
            if (res == null)
                throw new Win32Exception("drmModeGetResources failed");
            
            var crtcs = new drmModeCrtc[res->count_crtcs];
            for (var c = 0; c < res->count_crtcs; c++)
            {
                var crtc = drmModeGetCrtc(fd, res->crtcs[c]);
                crtcs[c] = *crtc;
                drmModeFreeCrtc(crtc);
            }
            
            for (var c = 0; c < res->count_encoders; c++)
            {
                var enc = drmModeGetEncoder(fd, res->encoders[c]);
                Encoders[res->encoders[c]] = new DrmEncoder(*enc, crtcs);
                drmModeFreeEncoder(enc);
            }
            
            for (var c = 0; c < res->count_connectors; c++)
            {
                var conn = drmModeGetConnector(fd, res->connectors[c]);
                Connectors.Add(new DrmConnector(conn));
                drmModeFreeConnector(conn);
            }


        }

        public void Dump()
        {
            void Print(int off, string s)
            {
                for (var c = 0; c < off; c++)
                    Console.Write("    ");
                Console.WriteLine(s);
            }
            Print(0, "Connectors");
            foreach (var conn in Connectors)
            {
                Print(1, $"{conn.Name}:");
                Print(2, $"Id: {conn.Id}");
                Print(2, $"Size: {conn.SizeMm} mm");
                Print(2, $"Encoder id: {conn.EncoderId}");
                Print(2, "Modes");
                foreach (var m in conn.Modes)
                    Print(3, $"{m.Name} {(m.IsPreferred ? "PREFERRED" : "")}");


            }
        }
    }
    
    public class DrmCard : IDisposable
    {
        private bool _disposed;
        
        public DrmCard(string path = null)
        {
            Path = path ?? "/dev/dri/card0";
            
            Fd = open(Path, 2, 0);
            if (Fd == -1)
                throw new Win32Exception("Couldn't open " + Path);

            GbmDevice = new GbmDevice(this);
        }

        ~DrmCard() => Dispose(false);

        public int Fd { get; private set; }

        public GbmDevice GbmDevice { get; }

        public string Path { get; }

        public DrmResources GetResources() => new DrmResources(Fd);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        { 
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                GbmDevice.Dispose();
            }

            ReleaseUnmanagedResources();
            _disposed = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (Fd != -1)
            {
                close(Fd);
                Fd = -1;
            }
        }
    }
}
