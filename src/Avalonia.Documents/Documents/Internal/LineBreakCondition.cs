namespace Avalonia.Documents.Internal
{

    /// <summary>
    /// Breaking condition around inline object
    /// </summary>
    /// <remarks>
    ///                   | BreakDesired | BreakPossible | BreakRestrained | BreakAlways |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakDesired     |     TRUE     |     TRUE      |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakPossible    |     TRUE     |     FALSE     |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakRestrained  |     FALSE    |     FALSE     |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakAlways      |     TRUE     |     TRUE      |      TRUE       |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    /// </remarks>
    public enum LineBreakCondition
    {
        /// <summary>
        /// Break if not prohibited by other
        /// </summary>
        BreakDesired,

        /// <summary>
        /// Break if allowed by other
        /// </summary>
        BreakPossible,

        /// <summary>
        /// Break prohibited always
        /// </summary>
        BreakRestrained,

        /// <summary>
        /// Break allowed always
        /// </summary>
        BreakAlways
    }
}
