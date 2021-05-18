using System;
using Avalonia.Logging;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.OpenGL.Egl
{
    public class EglPlatformOpenGlInterface : IPlatformOpenGlInterface
    {
        public EglDisplay Display { get; }
        
        public bool CanCreateContexts => true;
        
        public bool CanShareContexts => Display.SupportsSharing;
        
        public EglContext PrimaryEglContext { get; }
        
        public IGlContext PrimaryContext => PrimaryEglContext;
        
        public EglPlatformOpenGlInterface(EglDisplay display)
        {
            Display = display;
            PrimaryEglContext = display.CreateContext(null);
        }
        
        public static void TryInitialize()
        {
            var feature = TryCreate();
            if (feature != null)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(feature);
        }
        
        public static EglPlatformOpenGlInterface TryCreate() => TryCreate(() => new EglDisplay());
        public static EglPlatformOpenGlInterface TryCreate(Func<EglDisplay> displayFactory)
        {
            try
            {
                return new EglPlatformOpenGlInterface(displayFactory());
            }
            catch(Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(null, "Unable to initialize EGL-based rendering: {0}", e);
                return null;
            }
        }

        public IGlContext CreateContext() => Display.CreateContext(null);
        public IGlContext CreateSharedContext() => Display.CreateContext(PrimaryEglContext);
        

        public EglSurface CreateWindowSurface(IntPtr window)
        {
            using (PrimaryContext.MakeCurrent())
            {
                var s = Display.EglInterface.CreateWindowSurface(Display.Handle, Display.Config, window,
                    new[] { EGL_NONE, EGL_NONE });
                if (s == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglCreateWindowSurface", Display.EglInterface);
                return new EglSurface(Display, PrimaryEglContext, s);
            }
        }
        
        public EglSurface CreatePBufferFromClientBuffer (int bufferType, IntPtr handle, int[] attribs)
        {
            using (PrimaryContext.MakeCurrent())
            {
                var s = Display.EglInterface.CreatePbufferFromClientBuffer(Display.Handle, bufferType, handle,
                    Display.Config, attribs);

                if (s == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglCreatePbufferFromClientBuffer", Display.EglInterface);
                return new EglSurface(Display, PrimaryEglContext, s);
            }
        }
    }
}
