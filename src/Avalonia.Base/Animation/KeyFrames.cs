using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    /// <summary>
    /// A collection of <see cref="KeyFrame"/>s.
    /// </summary>
    public class KeyFrames : AvaloniaList<KeyFrame>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyFrames"/> class.
        /// </summary>
        public KeyFrames()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyFrames"/> class.
        /// </summary>
        /// <param name="items">The initial items in the collection.</param>
        public KeyFrames(IEnumerable<KeyFrame> items)
            : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}