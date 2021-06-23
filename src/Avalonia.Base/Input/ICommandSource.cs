using System.Windows.Input;

namespace Avalonia.Input
{
    ///<summary>
    /// An interface for classes that know how to invoke a Command.
    ///</summary>
    public interface ICommandSource
    {
        /// <summary>
        /// The command that will be executed when the class is "invoked."
        /// Classes that implement this interface should enable or disable based on the command's CanExecute return value.
        /// The property may be implemented as read-write if desired.
        /// </summary>
        ICommand Command { get; }

        /// <summary>
        /// The parameter that will be passed to the command when executing the command.
        /// The property may be implemented as read-write if desired.
        /// </summary>
        object CommandParameter { get; }


        /// <summary>
        /// Bor the bheavior CanExecuteChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CanExecuteChanged(object sender, System.EventArgs e);

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are enabled.
        /// </summary>
        bool IsEffectivelyEnabled { get; }
    }
}
