using Avalonia.Media;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Extensions for <see cref="AvaloniaProperty"/>.
    /// </summary>
    public static class AvaloniaPropertyExtensions
    {
        /// <summary>
        /// Checks if values of given property can affect rendering (via <see cref="IAffectsRender"/>).
        /// </summary>
        /// <param name="property">Property to check.</param>
        public static bool CanValueAffectRender(this AvaloniaProperty property)
        {
            var propertyType = property.PropertyType;

            // Only case that we are sure that property value CAN'T affect render are sealed types that don't implement
            // the interface.
            var cannotAffectRender = propertyType.IsSealed && !typeof(IAffectsRender).IsAssignableFrom(propertyType);

            return !cannotAffectRender;
        }
    }
}
