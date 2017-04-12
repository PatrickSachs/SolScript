using JetBrains.Annotations;

namespace PSUtility.Strings
{
    /// <summary>
    ///     Extension & Utilty methods related to strings.
    /// </summary>
    [PublicAPI]
    public static class StringUtility
    {
        /// <summary>
        ///     Indicates whether a specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>
        ///     <see langword="true" /> if the value parameter is <see langword="null" /> or <see cref="string.Empty" />, or
        ///     if <paramref name="value" /> consists exclusively of white-space characters.
        /// </returns>
        /// <remarks>
        ///     See also: https://msdn.microsoft.com/en-us/library/system.string.isnullorwhitespace.aspx/
        /// </remarks>
        [Pure]
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) {
                return true;
            }
            foreach (char c in value) {
                if (!char.IsWhiteSpace(c)) {
                    return false;
                }
            }
            return true;
        }
    }
}