using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     The type builder is used to create an assembly independent type for usable in various other builders.
    /// </summary>
    public abstract class SolTypeBuilder
    {
        internal SolTypeBuilder() {}

        /// <summary>
        ///     The type will be inferred from whatever the given native type is know as.
        /// </summary>
        /// <param name="type">The native type.</param>
        /// <param name="canBeNil">Can the type be nil?</param>
        /// <returns>The type builder.</returns>
        public static SolTypeBuilder Native(Type type, bool canBeNil = true)
        {
            return new NativeImpl(type, canBeNil);
        }

        /// <summary>
        ///     The type will used a fixed SolType for every assembly. This is only recommended if you e.g. use primitives or other
        ///     values guaranteed to be the same between every assembly.
        /// </summary>
        /// <param name="type">The native type.</param>
        /// <returns>The type builder.</returns>
        public static SolTypeBuilder Fixed(SolType type)
        {
            return new FixedImpl(type);
        }

        /// <summary>
        ///     Gets the <see cref="SolType"/> for the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="SolAssembly"/>.</param>
        /// <returns>The <see cref="SolType"/> represented by this builder for the given <paramref name="assembly"/>.</returns>
        /// <exception cref="SolMarshallingException">Cannot find a <see cref="SolType"/> for this <paramref name="assembly"/>.</exception>
        public abstract SolType Get(SolAssembly assembly);

        #region Nested type: FixedImpl

        private class FixedImpl : SolTypeBuilder
        {
            public FixedImpl(SolType type)
            {
                m_Type = type;
            }

            private readonly SolType m_Type;

            #region Overrides

            /// <inheritdoc />
            public override SolType Get(SolAssembly assembly)
            {
                return m_Type;
            }

            #endregion
        }

        #endregion

        #region Nested type: NativeImpl

        private class NativeImpl : SolTypeBuilder
        {
            public NativeImpl(Type type, bool canBeNil)
            {
                m_Type = type;
                m_CanBeNil = canBeNil;
            }

            private readonly bool m_CanBeNil;
            private readonly Type m_Type;

            #region Overrides

            /// <inheritdoc />
            /// <exception cref="SolMarshallingException">Cannot find SolType for this type.</exception>
            public override SolType Get(SolAssembly assembly)
            {
                return SolMarshal.GetSolType(assembly, m_Type);
                /*string name;
                if (SolValue.TryGetPrimitiveTypeNameOf(m_Type, out name)) {
                    return new SolType(name, m_CanBeNil);
                }
                SolClassDefinition definition;
                if (!assembly.TryGetClass(m_Type, out definition)) {
                    throw new SolMarshallingException(m_Type);
                }
                return new SolType(definition.Type, m_CanBeNil);*/
            }

            #endregion
        }

        #endregion
    }
}