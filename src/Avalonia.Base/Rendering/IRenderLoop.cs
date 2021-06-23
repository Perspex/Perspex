namespace Avalonia.Rendering
{
    /// <summary>
    /// The application render loop.
    /// </summary>
    /// <remarks>
    /// The render loop is responsible for advancing the animation timer and updating the scene
    /// graph for visible windows.
    /// </remarks>
    public interface IRenderLoop
    {
        /// <summary>
        /// Adds an update task.
        /// </summary>
        /// <param name="i">The update task.</param>
        /// <remarks>
        /// Registered update tasks will be polled on each tick of the render loop after the
        /// animation timer has been pulsed.
        /// </remarks>
        void Add(IRenderLoopTask i);

        /// <summary>
        /// Removes an update task.
        /// </summary>
        /// <param name="i">The update task.</param>
        void Remove(IRenderLoopTask i);
    }
}
