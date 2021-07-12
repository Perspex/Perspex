using System;
using System.ComponentModel;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using JetBrains.Annotations;

namespace Avalonia.Controls.Embedding
{
    public class ManagedCursorImpl : ICursorImpl
    {
        public ManagedCursorImpl(StandardCursorType type, IBitmapImpl bitmapImpl = null)
        {
            Type = type;
            Bitmap = bitmapImpl;
        }
            
        public IBitmapImpl Bitmap { get; }
            
        public StandardCursorType Type { get; }
            
        public void Dispose() { }
    }
    
    public class ManagedCursorFactory : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new ManagedCursorImpl(cursorType);
        
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new ManagedCursorImpl(StandardCursorType.None, cursor);

        
    }

    public class EmbeddableControlRoot : TopLevel, IStyleable, IFocusScope, IDisposable
    {
        public EmbeddableControlRoot(ITopLevelImpl impl) : base(impl)
        {
            new ManagedPointer(this);
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableWindow())
        {
            new ManagedPointer(this);
        }

        protected bool EnforceClientSize { get; set; } = true;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass();
        }

        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            if (EnforceClientSize)
                availableSize = PlatformImpl?.ClientSize ?? default(Size);
            var rv = base.MeasureOverride(availableSize);
            if (EnforceClientSize)
                return availableSize;
            return rv;
        }

        Type IStyleable.StyleKey => typeof(EmbeddableControlRoot);
        public void Dispose() => PlatformImpl?.Dispose();
    }
}
