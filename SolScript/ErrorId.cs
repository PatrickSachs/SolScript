namespace SolScript
{
    /// <summary>
    ///     All SolScript error Ids.
    /// </summary>
    public enum ErrorId
    {
        /// <summary>
        ///     Default value. No id.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Could not resolve error id.
        /// </summary>
        InternalFailedToResolve = 1,

        /// <summary>
        ///     This error marks that something has been done to prevent corruption of the assembly.
        /// </summary>
        InternalSecurityMeasure = 2,

        /// <summary>
        ///     You did it wrong John.
        /// </summary>
        SyntaxError = 3,

        /// <summary>
        ///     The interpreter failed to ... well interpret something.
        /// </summary>
        InterpreterError = 4,

        /// <summary>
        ///     Failed to register a class.
        /// </summary>
        ClassRegistry = 5,

        /// <summary>
        ///     Failed to compile something.
        /// </summary>
        CompilerError = 6,

        /// <summary>
        ///     An invalid inheritance has been detected.
        /// </summary>
        InvalidInheritance = 7,

        /// <summary>
        ///     Failed to build a field definition for a class.
        /// </summary>
        ClassFieldRegistry = 8,

        /// <summary>
        ///     Failed to build a function definition for a class.
        /// </summary>
        ClassFunctionRegistry = 9,

        /// <summary>
        ///     The used type is not a valid annotation type.
        /// </summary>
        InvalidAnnotationType = 10,

        /// <summary>
        ///     Failed to build a field definition for a global.
        /// </summary>
        GlobalFieldRegistry = 11,

        /// <summary>
        ///     Failed to build a function definition for a global.
        /// </summary>
        GlobalFunctionRegistry = 12
    }
}