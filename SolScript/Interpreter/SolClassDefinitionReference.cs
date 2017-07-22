using System;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     References to a sol class definition. Used during parse time when the class definitions may not been known by name.
    /// </summary>
    public class SolClassDefinitionReference
    {
        /// <summary>
        ///     Creates a new reference from a strong reference.
        /// </summary>
        /// <param name="assembly">The assembly this class is in.</param>
        /// <param name="strong">The reference.</param>
        /// <exception cref="ArgumentNullException"><paramref name="strong" /> is null -or- <paramref name="assembly" /> is null</exception>
        public SolClassDefinitionReference(SolAssembly assembly, SolClassDefinition strong) : this(assembly)
        {
            if (strong == null) {
                throw new ArgumentNullException(nameof(strong));
            }
            m_Strong = strong;
        }

        /// <summary>
        ///     Creates a new reference from a weak reference(= the class name).
        /// </summary>
        /// <param name="assembly">The assembly this class is in.</param>
        /// <param name="weak">The reference.</param>
        /// <exception cref="ArgumentNullException"><paramref name="weak" /> is null -or- <paramref name="assembly" /> is null</exception>
        public SolClassDefinitionReference(SolAssembly assembly, string weak) : this(assembly)
        {
            if (weak == null) {
                throw new ArgumentNullException(nameof(weak));
            }
            m_Weak = weak;
        }

        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null</exception>
        private SolClassDefinitionReference(SolAssembly assembly)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            m_Assembly = assembly;
        }

        // The assembly.
        private readonly SolAssembly m_Assembly;
        // The potential string reference.
        private SolClassDefinition m_Strong;
        // The portential weak reference. (Ignored if we have m_Strong!)
        private string m_Weak;

        /// <summary>
        ///     The class name of the definition. Always valid.
        /// </summary>
        public string Name {
            get {
                if (IsStrong) {
                    return m_Strong.Type;
                }
                return m_Weak;
            }
        }

        /// <summary>
        ///     Is this reference a strong reference? (e.g. has the internal class definition been cached? This property should not
        ///     matter if you plan to use is during runtime as obtaining a class definition reference can only lead to problems
        ///     during parse/compile time.)
        /// </summary>
        public bool IsStrong => m_Strong != null;

        /// <summary>
        ///     Tries to get the actual definition. Validity depends on internal assembly and parser state.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <returns>True if the definition could be obtained, false if not.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Invalid state.
        /// </exception>
        /// <remarks>Requires the <see cref="SolAssembly.AssemblyState.GeneratedClassHulls" /> state.</remarks>
        public bool TryGetDefinition(out SolClassDefinition definition)
        {
            // todo: fix state dependency - its a try method!
            if (IsStrong) {
                definition = m_Strong;
                return true;
            }
            if (m_Assembly.TryGetClass(m_Weak, out definition)) {
                m_Strong = definition;
                m_Weak = null;
                return true;
            }
            return false;
        }
    }
}