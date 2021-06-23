using System;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a name scope.
    /// </summary>
    public interface INameScope
    {
        /// <summary>
        /// Registers an element in the name scope.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <param name="element">The element.</param>
        void Register(string name, object element);

        /// <summary>
        /// Finds a named element in the name scope, waits for the scope to be completely populated before returning null
        /// Returned task is configured to run any continuations synchronously.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element, or null if the name was not found.</returns>
        SynchronousCompletionAsyncResult<object> FindAsync(string name);
        
        /// <summary>
        /// Finds a named element in the name scope, returns immediately, doesn't traverse the name scope stack
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element, or null if the name was not found.</returns>
        object Find(string name);

        /// <summary>
        /// Marks the name scope as completed, no further registrations will be allowed
        /// </summary>
        void Complete();
        
        /// <summary>
        /// Returns whether further registrations are allowed on the scope
        /// </summary>
        bool IsCompleted { get; }


    }
}
