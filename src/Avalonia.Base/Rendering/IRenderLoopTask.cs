using System;
using System.Threading.Tasks;

namespace Avalonia.Rendering
{
    public interface IRenderLoopTask
    {
        bool NeedsUpdate { get; }
        void Update(TimeSpan time);
        void Render();
    }
}
