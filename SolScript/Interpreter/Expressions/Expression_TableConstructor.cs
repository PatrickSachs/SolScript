// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System.Collections.Generic;
using System.Text;
using NodeParser;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression creates a table from a set of keys and values.
    /// </summary>
    public class Expression_TableConstructor : SolExpression
    {
        /// <inheritdoc />
        /// <param name="assembly">The assembly of the table.</param>
        /// <param name="location">The expression location.</param>
        /// <param name="fields">
        ///     All table fields. The key can be null if they represent the array like part of the table. Index
        ///     will be applied by the native iteration order of the enumerable.
        /// </param>
        public Expression_TableConstructor(SolAssembly assembly, NodeLocation location, IEnumerable<KeyValuePair<SolExpression, SolExpression>> fields) : base(assembly, location)
        {
            m_Fields = InternalHelper.CreateArray(fields);
        }

        private readonly Array<KeyValuePair<SolExpression, SolExpression>> m_Fields;

        /// <summary>
        ///     A read only list of all fields in this table.
        /// </summary>
        public ReadOnlyList<KeyValuePair<SolExpression, SolExpression>> Fields => m_Fields.AsReadOnly();

        /// <inheritdoc />
        public override bool IsConstant {
            get {
                foreach (var field in m_Fields) {
                    if (!field.Key.IsConstant) {
                        return false;
                    }
                    if (!field.Value.IsConstant) {
                        return false;
                    }
                }
                return true;
            }
        }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An error occured while evaluating the expression or assigning the table values.</exception>
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            if (context != null) {
                context.CurrentLocation = Location;
            }
            SolTable table = new SolTable();
            int nextArrayIdx = 0;
            foreach (var field in m_Fields) {
                SolValue key = field.Key?.Evaluate(context, parentVariables) ?? new SolNumber(nextArrayIdx++);
                SolValue value = field.Value.Evaluate(context, parentVariables);
                try {
                    table[key] = value;
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, $"An error occured while creating the table at key \"{key}\".", ex);
                }
            }
            table.SetN(m_Fields.Length);
            return table;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            int nextArrayIdx = 0;
            builder.AppendLine("{");
            foreach (var field in m_Fields) {
                // ReSharper disable once ConstantNullCoalescingCondition
                builder.AppendLine("  [" + (field.Key?.ToString() ?? nextArrayIdx++.ToString()) + "] = " + field.Value);
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            bool success = true;
            for (int i = 0; i < m_Fields.Length; i++) {
                var field = m_Fields[i];
                // If the key is null we have an array like part, so the key will always be valid.
                if (field.Key != null) {
                    ValidationResult keyResult = field.Key.Validate(context);
                    // Evalualte all keys/values even if one fails.
                    if (!keyResult) {
                        context.Errors.Add(new SolError(Location, CompilerResources.Err_TableConstructorKeyError.FormatWith(i)));
                        success = false;
                    }
                }
                ValidationResult valueResult = field.Value.Validate(context);
                if (!valueResult) {
                    context.Errors.Add(new SolError(Location, CompilerResources.Err_TableConstructorValueError.FormatWith(i)));
                    success = false;
                }
            }
            return new ValidationResult(success, new SolType(SolTable.TYPE, false));
        }

        #endregion
    }
}