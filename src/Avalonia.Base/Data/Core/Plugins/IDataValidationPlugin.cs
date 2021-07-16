using System;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Defines how data validation is observed by an <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IDataValidationPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle data validation on the specified object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="memberName">The name of the member to validate.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        bool Match(WeakReference<object> reference, string memberName);

        /// <summary>
        /// Starts monitoring the data validation state of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="inner">The inner property accessor used to access the property.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        IPropertyAccessor Start(WeakReference<object> reference,
            string propertyName,
            IPropertyAccessor inner);
    }
}
