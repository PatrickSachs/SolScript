using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression creates a table from a set of keys and values.
    /// </summary>
    public class Expression_TableConstructor : SolExpression
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        ///     Not the same amount of <paramref name="keys" /> and <paramref name="values" />
        ///     passed.
        /// </exception>
        public Expression_TableConstructor(SolAssembly assembly, SolSourceLocation location, SolExpression[] keys, SolExpression[] values) : base(assembly, location)
        {
            if (keys.Length != values.Length) {
                throw new ArgumentException($"Not the same amount of keys({keys.Length}) and values({values.Length}) has been passed.", nameof(keys));
            }
            m_Keys = keys;
            m_Values = values;
        }

        private readonly SolExpression[] m_Keys;
        private readonly SolExpression[] m_Values;

        /// <summary>
        ///     A read-only collection of all keys in this constructor. The matching value can be found in the
        ///     <see cref="Values" /> list at them same index.
        /// </summary>
        public IReadOnlyList<SolExpression> Keys => m_Keys;

        /// <summary>
        ///     A read-only collection of all values in this constructor. The matching key can be found in the <see cref="Keys" />
        ///     list at them same index.
        /// </summary>
        public IReadOnlyList<SolExpression> Values => m_Values;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An error occured while evaluating the expression or assigning the table values.</exception>
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            SolTable table = new SolTable();
            for (int i = 0; i < m_Keys.Length; i++) {
                SolValue key = m_Keys[i].Evaluate(context, parentVariables);
                SolValue value = m_Values[i].Evaluate(context, parentVariables);
                try {
                    table[key] = value;
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, $"An error occured while creating the table at key \"{key}\".", ex);
                }
            }
            table.SetN(m_Keys.Length);
            return table;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            for (int i = 0; i < m_Keys.Length; i++) {
                SolExpression key = m_Keys[i];
                SolExpression value = m_Values[i];
                builder.AppendLine("  [" + key + "] = " + value);
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion
    }
}