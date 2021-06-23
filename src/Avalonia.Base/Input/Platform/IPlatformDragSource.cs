using System.Threading.Tasks;

namespace Avalonia.Input.Platform
{
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects);
    }
}
