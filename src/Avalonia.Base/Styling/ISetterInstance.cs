#nullable enable

using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a setter that has been instanced on a control.
    /// </summary>
    public interface ISetterInstance : IDisposable
    {
        /// <summary>
        /// Starts the setter instance.
        /// </summary>
        /// <param name="hasActivator">Whether the parent style has an activator.</param>
        /// <remarks>
        /// If <paramref name="hasActivator"/> is false then the setter should be immediately
        /// applied and <see cref="Activate"/> and <see cref="Deactivate"/> should not be called.
        /// If true, then bindings etc should be initiated but not produce a value until
        /// <see cref="Activate"/> called.
        /// </remarks>
        public void Start(bool hasActivator);

        /// <summary>
        /// Activates the setter.
        /// </summary>
        /// <remarks>
        /// Should only be called if hasActivator was true when <see cref="Start(bool)"/> was called.
        /// </remarks>
        public void Activate();

        /// <summary>
        /// Deactivates the setter.
        /// </summary>
        /// <remarks>
        /// Should only be called if hasActivator was true when <see cref="Start(bool)"/> was called.
        /// </remarks>
        public void Deactivate();
    }
}
