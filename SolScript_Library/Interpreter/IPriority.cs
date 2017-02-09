namespace SolScript.Interpreter
{
    /// <summary>
    ///     This interface is used by all classes having a priority of some sort. It is used to support sorting collections by
    ///     priority using the <see cref="PriorityComparer" />.
    /// </summary>
    public interface IPriority
    {
        /// <summary>
        ///     The priority. This value must be a constant value.
        /// </summary>
        int Priority { get; }
    }
}