using System;

namespace SolScript.Interpreter {
    /// <summary> Terminators are used to signal the current termination state of a
    ///     Statement or Expression. </summary>
    [Flags]
    public enum Terminators {
        /// <summary> Nothing has been terminated, continue as usual. </summary>
        None = 0,

        /// <summary> The Statement or Expression is forcefully returning. </summary>
        Return = 1,

        /// <summary> The Statement or Expression is breaking out of the context. </summary>
        Break = 2,

        /// <summary> The Statement or Expression is continuing the context. </summary>
        Continue = 4
    }
}