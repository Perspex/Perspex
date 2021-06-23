using System;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Holds the state for a dirty rect rendered when <see cref="IRenderer.DrawDirtyRects"/> is set.
    /// </summary>
    internal class DisplayDirtyRect
    {
        public static readonly TimeSpan TimeToLive = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayDirtyRect"/> class.
        /// </summary>
        /// <param name="rect">The dirt rect.</param>
        public DisplayDirtyRect(Rect rect)
        {
            Rect = rect;
            ResetLifetime();
        }

        /// <summary>
        /// Gets the bounds of the dirty rectangle.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// Gets the time at which the rectangle was made dirty.
        /// </summary>
        public DateTimeOffset Born { get; private set; }

        /// <summary>
        /// Gets the time at which the rectangle should no longer be displayed.
        /// </summary>
        public DateTimeOffset Dies { get; private set; }

        /// <summary>
        /// Gets the opacity at which to display the dirty rectangle.
        /// </summary>
        public double Opacity => (Dies - DateTimeOffset.UtcNow).TotalMilliseconds / TimeToLive.TotalMilliseconds;

        /// <summary>
        /// Resets the rectangle's lifetime.
        /// </summary>
        public void ResetLifetime()
        {
            Born = DateTimeOffset.UtcNow;
            Dies = Born + TimeToLive;
        }
    }
}
