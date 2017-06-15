using System;
using System.Text;
using Irony.Parsing;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Properties;

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
        public Expression_TableConstructor(SolAssembly assembly, SourceLocation location, SolExpression[] keys, SolExpression[] values) : base(assembly, location)
        {
            if (keys.Length != values.Length) {
                throw new ArgumentException($"Not the same amount of keys({keys.Length}) and values({values.Length}) has been passed.", nameof(keys));
            }
            m_Keys = new Array<SolExpression>(keys);
            m_Values = new Array<SolExpression>(values);
        }

        private readonly Array<SolExpression> m_Keys;
        private readonly Array<SolExpression> m_Values;

        /// <summary>
        ///     A read-only collection of all keys in this constructor. The matching value can be found in the
        ///     <see cref="Values" /> list at them same index.
        /// </summary>
        public ReadOnlyList<SolExpression> Keys => m_Keys.AsReadOnly();

        /// <summary>
        ///     A read-only collection of all values in this constructor. The matching key can be found in the <see cref="Keys" />
        ///     list at them same index.
        /// </summary>
        public ReadOnlyList<SolExpression> Values => m_Values.AsReadOnly();

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An error occured while evaluating the expression or assigning the table values.</exception>
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            if (context != null) {
                context.CurrentLocation = Location;
            }
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

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            bool success = true;
            for (int i = 0; i < m_Keys.Length; i++) {
                SolExpression key = m_Keys[i];
                SolExpression value = m_Values[i];
                ValidationResult keyResult = key.Validate(context);
                // Evalualte all keys/values even if one fails.
                if (!keyResult) {
                    context.Errors.Add(new SolError(Location, CompilerResources.Err_TableConstructorKeyError.FormatWith(i)));
                    success = false;
                }
                ValidationResult valueResult = value.Validate(context);
                if (!valueResult) {
                    context.Errors.Add(new SolError(Location, CompilerResources.Err_TableConstructorValueError.FormatWith(i)));
                    success = false;
                }
            }
            return new ValidationResult(success, new SolType(SolTable.TYPE, false));
        }

        /// <inheritdoc />
        public override bool IsConstant {
            get {
                foreach (SolExpression expression in Keys) {
                    if (!expression.IsConstant) return false;
                }
                foreach (SolExpression expression in Values) {
                    if (!expression.IsConstant) return false;
                }
                return true;
            }
        }

        #endregion
    }
}