using PSUtility.Metadata;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     All default meta keys usable to access a <see cref="SolAssembly" />. The key name are equal to their field names.
    ///     Feel free to use new and existing keys to share data between libraries in an isoalted native space.
    /// </summary>
    public static class SolMetaKeys
    {
        /// <summary>
        ///     Used by the marshaller to cache information about the assembly.
        /// </summary>
        internal static readonly MetaKey<SolMarshal.AssemblyCache> SolMarshalAssemblyCache = new MetaKey<SolMarshal.AssemblyCache>(nameof(SolMarshalAssemblyCache));
    }
}