using System;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Properties;

namespace SolScript.Compiler
{
    /// <summary>
    ///     The compilation context holds data relevant for the compilation of an assembly.
    /// </summary>
    public class SolCompliationContext
    {
        private readonly PSDictionary<string, uint> m_FileIndices = new PSDictionary<string, uint>();
        private uint m_NextFileIndex = 1;

        /// <summary>
        ///     A read only dictionary of all files and their mapped indices.
        /// </summary>
        public ReadOnlyDictionary<string, uint> FileIndices => m_FileIndices.AsReadOnly();

        /// <summary>
        ///     The current state of the compilation.
        /// </summary>
        public SolCompilationState State { get; set; }

        /// <summary>
        ///     Registers a file in the file index lookup.
        /// </summary>
        /// <param name="fileName">THe file name.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="fileName" /> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Only allowed in <see cref="SolCompilationState.Preparing" />
        ///     <see cref="State" />.
        /// </exception>
        public void RegisterFile([NotNull] string fileName)
        {
            if (fileName == null) {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (State != SolCompilationState.Preparing) {
                throw new InvalidOperationException(CompilerResources.Err_InvalidCompilerState.FormatWith(State, SolCompilationState.Preparing));
            }
            if (!m_FileIndices.ContainsKey(fileName)) {
                m_FileIndices.Add(fileName, m_NextFileIndex++);
            }
        }

        /// <summary>
        ///     Gets the file index of the given file name.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file index.</returns>
        /// <remarks>The file index 0 is used for invalid file names.</remarks>
        public uint FileIndexOf(string fileName)
        {
            uint index;
            if (!m_FileIndices.TryGetValue(fileName, out index)) {
                return 0;
            }
            return index;
        }
    }
}