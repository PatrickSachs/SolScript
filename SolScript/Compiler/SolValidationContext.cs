using System.Collections.Generic;
using PSUtility.Enumerables;
using SolScript.Interpreter;

namespace SolScript.Compiler
{
    public class SolValidationContext
    {
        public SolValidationContext(SolErrorCollection.Adder errors)
        {
            Errors = errors;
        }

        public Stack<Chunk> Chunks { get; } = new Stack<Chunk>();

        public SolErrorCollection.Adder Errors { get; }
        public SolClassDefinition InClassDefinition { get; set; }
        public SolFieldDefinition InFieldDefinition { get; set; }
        public SolFunctionDefinition InFunctionDefinition { get; set; }

        /// <summary>
        ///     Checks if one of the stacked chunks has the given variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>true if the variable exists, false if not.</returns>
        public bool HasChunkVariable(string name)
        {
            foreach (Chunk chunk in Chunks) {
                if (chunk.HasVariable(name)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Tries to get the type currently associated with a given variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="type">The variable type out value.</param>
        /// <returns>true if the variable exists, false if not.</returns>
        public bool TryGetChunkVariable(string name, out SolType type)
        {
            foreach (Chunk chunk in Chunks) {
                if (chunk.TryGetVariable(name, out type)) {
                    return true;
                }
            }
            type = default(SolType);
            return false;
        }

        #region Nested type: Chunk

        public class Chunk
        {
            public Chunk(SolChunk chunk)
            {
                SolChunk = chunk;
            }

            private readonly PSDictionary<string, SolType> m_Variables = new PSDictionary<string, SolType>();

            /// <summary>
            ///     The chunk the variables are related to.
            /// </summary>
            public SolChunk SolChunk { get; }

            public ReadOnlyDictionary<string, SolType> AsReadOnly() => m_Variables.AsReadOnly();

            public bool HasVariable(string name)
            {
                return m_Variables.ContainsKey(name);
            }

            public bool TryGetVariable(string name, out SolType type)
            {
                return m_Variables.TryGetValue(name, out type);
            }

            public void AddVariable(string name, SolType type)
            {
                m_Variables.Add(name, type);
            }
        }

        #endregion
    }
}