using SolScript.Interpreter;

namespace SolScript.Compiler
{
    /// <summary>
    ///     Returned by the compiler validation functions.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        ///     Creates a new validation result.
        /// </summary>
        /// <param name="success">Did the validation succceed?</param>
        /// <param name="type">The type of the statement/expression?</param>
        public ValidationResult(bool success, SolType type)
        {
            Success = success;
            Type = type;
        }

        private static readonly ValidationResult s_Failure = new ValidationResult(false, default(SolType));

        /// <summary>
        ///     Did the validation succceed?
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     The type of the statement/expression?
        /// </summary>
        public SolType Type { get; }

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(Success)}: {Success}, {nameof(Type)}: {Type}";
        }

        #endregion

        /// <summary>
        ///     Returns a validation result that indicates failure.
        /// </summary>
        /// <returns>The validation result.</returns>
        public static ValidationResult Failure()
        {
            return s_Failure;
        }

        /// <summary>
        ///     Converts the result to a boolean for easier branching.
        /// </summary>
        /// <param name="result">The result.</param>
        public static implicit operator bool(ValidationResult result)
        {
            return result.Success;
        }
    }
}