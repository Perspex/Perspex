namespace Avalonia.Automation
{
    /// <summary>
    /// Describes the notification characteristics of a particular live region
    /// </summary>
    public enum AutomationLiveSetting
    {
        /// <summary>
        /// The element does not send notifications if the content of the live region has changed.
        /// </summary>
        Off = 0,

        /// <summary>
        /// The element sends non-interruptive notifications if the content of the live region has
        /// changed. With this setting, UI Automation clients and assistive technologies are expected 
        /// to not interrupt the user to inform of changes to the live region.
        /// </summary>
        Polite = 1,

        /// <summary>
        /// The element sends interruptive notifications if the content of the live region has changed. 
        /// With this setting, UI Automation clients and assistive technologies are expected to interrupt 
        /// the user to inform of changes to the live region.
        /// </summary>
        Assertive = 2,
    }
}

