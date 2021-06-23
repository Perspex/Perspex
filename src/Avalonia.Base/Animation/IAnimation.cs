using System;
using System.Threading.Tasks;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Apply the animation to the specified control and run it when <paramref name="match" /> produces <c>true</c>.
        /// </summary>
        IDisposable Apply(Animatable control, IClock clock, IObservable<bool> match, Action onComplete = null);

        /// <summary>
        /// Run the animation on the specified control.
        /// </summary>
        Task RunAsync(Animatable control, IClock clock);
    }
}
