using System.Collections.Generic;

namespace Avalonia.Styling
{
    public interface IStyleHostExtra : IStyleHost
    {
        List<Style> CanceledStyles { get; }
    }
}
