namespace Avalonia.Animation
{
    /// <summary>
    /// Determines the playback direction of an animation.
    /// </summary>
    public enum PlaybackDirection
    {
        /// <summary>
        /// The animation is played normally.
        /// </summary>
        Normal,

        /// <summary>
        /// The animation is played in reverse direction.
        /// </summary>
        Reverse,

        /// <summary>
        /// The animation is played forwards first, then backwards.
        /// </summary>
        Alternate,

        /// <summary>
        /// The animation is played backwards first, then forwards.
        /// </summary>
        AlternateReverse
    }
}
