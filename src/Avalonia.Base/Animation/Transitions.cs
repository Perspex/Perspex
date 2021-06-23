using System;
using Avalonia.Collections;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// A collection of <see cref="ITransition"/> definitions.
    /// </summary>
    public class Transitions : AvaloniaList<ITransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transitions"/> class.
        /// </summary>
        public Transitions()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validate = ValidateTransition;
        }

        private void ValidateTransition(ITransition obj)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (obj.Property.IsDirect)
            {
                throw new InvalidOperationException("Cannot animate a direct property.");
            }
        }
    }
}
