using System;
using System.Reflection;
using SevenBiT.Inspector;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Wraps either a <see cref="InspectorField" /> or a <see cref="SolExpression" />
    /// </summary>
    public sealed class SolFieldInitializerWrapper
    {
        #region Type enum

        /// <summary>
        ///     The type of the wrapper field.
        /// </summary>
        public enum Type
        {
            /// <summary>
            ///     The field is a script field which needs to be initialized with the given expression.
            /// </summary>
            ScriptField,

            /// <summary>
            ///     The field is a native field or property.
            /// </summary>
            NativeField
        }

        #endregion

        /// <summary>
        ///     Creates a new wrapper for a script field.
        /// </summary>
        /// <param name="expression">The field initializer.</param>
        public SolFieldInitializerWrapper(SolExpression expression)
        {
            FieldType = Type.ScriptField;
            m_Field = expression.NotNull();
        }

        /// <summary>
        ///     Creates a new wrapper for a native field.
        /// </summary>
        /// <param name="field">The native field itself.</param>
        public SolFieldInitializerWrapper(InspectorField field)
        {
            // todo: wrapper inside a wrapper, possibly create field & property seperate?
            FieldType = Type.NativeField;
            m_Field = field;
        }

        private readonly object m_Field;

        /// <summary>
        ///     The type of the field. Check this before calling the get methods.
        /// </summary>
        /// <seealso cref="Type" />
        public Type FieldType { get; }

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            SolFieldInitializerWrapper wrapper = obj as SolFieldInitializerWrapper;
            return wrapper != null && Equals(wrapper);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return ((m_Field?.GetHashCode() ?? 0) * 397) ^ (int) FieldType;
            }
        }

        #endregion

        /// <inheritdoc />
        private bool Equals(SolFieldInitializerWrapper other)
        {
            return Equals(m_Field, other.m_Field) && FieldType == other.FieldType;
        }

        /// <summary>
        ///     Obtains a reference to the <see cref="SolExpression" /> in this wrapper.
        /// </summary>
        /// <returns>The script field initializer.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="FieldType" /> is not <see cref="Type.ScriptField" />.</exception>
        public SolExpression GetScriptField()
        {
            if (FieldType != Type.ScriptField)
            {
                throw new InvalidOperationException("Tried to obtain script field - The registered field is of type " + FieldType + ".");
            }
            return (SolExpression)m_Field;
        }
        /// <summary>
        ///     Obtains a reference to the <see cref="InspectorField" /> in this wrapper.
        /// </summary>
        /// <returns>The native field wrapper.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="FieldType" /> is not <see cref="Type.ScriptField" />.</exception>
        public InspectorField GetNativeField()
        {
            if (FieldType != Type.NativeField)
            {
                throw new InvalidOperationException("Tried to obtain native field - The registered field is of type " + FieldType + ".");
            }
            return (InspectorField)m_Field;
        }
    }
}