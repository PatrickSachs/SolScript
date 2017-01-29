using System;
using System.Collections.Generic;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This abstract base class is used for all builders which allow members(functions & fields) on them.
    /// </summary>
    public abstract class SolMemberBuilder
    {
        protected readonly Dictionary<string, SolFieldBuilder> FieldsLookup = new Dictionary<string, SolFieldBuilder>();
        protected readonly Dictionary<string, SolFunctionBuilder> FunctionsLookup = new Dictionary<string, SolFunctionBuilder>();

        public IReadOnlyCollection<SolFieldBuilder> Fields => FieldsLookup.Values;
        public IReadOnlyCollection<SolFunctionBuilder> Functions => FunctionsLookup.Values;

        /// <summary> Adds a field with the given name and field definition to the builder. </summary>
        /// <param name="field"> The field definition itself. </param>
        /// <param name="overwrite"> Should existing fields be overwritten? </param>
        /// <returns>The builder itself to allow method chaining.</returns>
        /// <exception cref="ArgumentException">
        ///     A field with this name already exists and and <paramref name="overwrite" /> is false.
        /// </exception>
        public virtual SolMemberBuilder AddField(SolFieldBuilder field, bool overwrite = false)
        {
            if (!overwrite && FieldsLookup.ContainsKey(field.Name)) {
                throw new ArgumentException($"The field \"{field.Name}\" already exists, and overwrite it set to {false}!", nameof(field));
            }
            FieldsLookup[field.Name] = field;
            return this;
        }

        /// <summary> Adds a function with the given name and field definition to the builder. </summary>
        /// <param name="function"> The function definition itself. </param>
        /// <param name="overwrite"> Should existing functions be overwritten? </param>
        /// <returns>The builder itself to allow method chaining.</returns>
        /// <exception cref="ArgumentException">
        ///     A function with this name already exists and and <paramref name="overwrite" /> is false.
        /// </exception>
        public virtual SolMemberBuilder AddFunction(SolFunctionBuilder function, bool overwrite = false)
        {
            if (!overwrite && FieldsLookup.ContainsKey(function.Name)) {
                throw new ArgumentException($"The function \"{function.Name}\" already exists, and overwrite it set to {false}!", nameof(function));
            }
            FunctionsLookup[function.Name] = function;
            return this;
        }

        #region Nested type: Generic

        /// <summary>
        ///     This class simply provides a generic type parameter to allow the builder syntax of the methods to return the most
        ///     derived type.
        /// </summary>
        /// <typeparam name="T">The most derived type(= type type that extends this class).</typeparam>
        public abstract class Generic<T> : SolMemberBuilder where T : Generic<T>
        {
            /// <inheritdoc cref="SolMemberBuilder.AddField"/>
            /// <exception cref="ArgumentException">
            ///     A field with this name already exists and and <paramref name="overwrite" /> is false.
            /// </exception>
            public new virtual T AddField(SolFieldBuilder field, bool overwrite = false)
            {
                return (T)base.AddField(field, overwrite);
            }

            /// <inheritdoc cref="SolMemberBuilder.AddFunction"/>
            /// <exception cref="ArgumentException">
            ///     A function with this name already exists and and <paramref name="overwrite" /> is false.
            /// </exception>
            public new virtual T AddFunction(SolFunctionBuilder function, bool overwrite = false)
            {
                return (T)base.AddFunction(function, overwrite);
            }
        }

        #endregion
    }
}