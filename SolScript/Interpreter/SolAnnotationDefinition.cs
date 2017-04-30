using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Expressions;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This definition is used to declare the usage of an annotation.
    /// </summary>
    public sealed class SolAnnotationDefinition : SolDefinitionBase
    {
        /// <summary>
        ///     Parser usage only.
        /// </summary>
        [Obsolete(InternalHelper.O_PARSER_MSG, InternalHelper.O_PARSER_ERR)]
        [UsedImplicitly]
        public SolAnnotationDefinition() {}

        /// <summary>Creates a new annotation definition.</summary>
        /// <param name="type">The annotation type name.</param>
        /// <param name="arguments">The annotation constructor arguments.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="type" /> is <see langword="null" /> -or-
        ///     <paramref name="arguments" /> is <see langword="null" />
        /// </exception>
        public SolAnnotationDefinition(SolClassDefinitionReference type, params SolExpression[] arguments)
        {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }
            Arguments = new PSList<SolExpression>(arguments);
            m_Reference = type;
            //Arguments = ReadOnlyList<SolExpression>.FromDelegate(() => ArgumentsList ?? EmptyReadOnlyList<SolExpression>.Value);
        }

        private readonly SolClassDefinitionReference m_Reference;

        /// <summary>
        ///     The annotation class name.
        /// </summary>
        /// <remarks>Always valid.</remarks>
        public string ClassName => m_Reference.ClassName;

        /// <summary>
        ///     The annotation class definition.
        /// </summary>
        /// <remarks>May not be valid dependig on assembly state.</remarks>
        /// <exception cref="InvalidOperationException">Failed to get the class definition.</exception>
        public SolClassDefinition Definition {
            get {
                SolClassDefinition definition;
                if (!m_Reference.TryGetDefinition(out definition)) {
                    throw new InvalidOperationException(Resources.Err_ClassDefinitionNotValid.ToString(m_Reference.ClassName));
                }
                return definition;
            }
        }

        /// <summary>
        ///     The annotation constructor arguments.
        /// </summary>
        public IList<SolExpression> Arguments { get; internal set; }
    }
}