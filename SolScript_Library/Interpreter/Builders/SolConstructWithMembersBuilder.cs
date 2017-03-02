using System;
using System.Collections.Generic;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     This abstract base class is used for all builders which allow members(functions & fields) on them.
    /// </summary>
    public abstract class SolConstructWithMembersBuilder : SolBuilderBase
    {
        protected readonly Dictionary<string, SolFieldBuilder> FieldsLookup = new Dictionary<string, SolFieldBuilder>();
        protected readonly Dictionary<string, SolFunctionBuilder> FunctionsLookup = new Dictionary<string, SolFunctionBuilder>();

        public IReadOnlyCollection<SolFieldBuilder> Fields => FieldsLookup.Values;
        public int FieldCount => FieldsLookup.Count;
        public IReadOnlyCollection<SolFunctionBuilder> Functions => FunctionsLookup.Values;
        public int FunctionCount => FunctionsLookup.Count;

        /// <summary> Adds a field with the given name and field definition to the builder. </summary>
        /// <param name="field"> The field definition itself. </param>
        /// <param name="overwrite"> Should existing fields be overwritten? </param>
        /// <returns>The builder itself to allow method chaining.</returns>
        /// <exception cref="ArgumentException">
        ///     A field with this name already exists and and <paramref name="overwrite" /> is false.
        /// </exception>
        public virtual SolConstructWithMembersBuilder AddField(SolFieldBuilder field, bool overwrite = false)
        {
            if (!overwrite && FieldsLookup.ContainsKey(field.Name))
            {
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
        public virtual SolConstructWithMembersBuilder AddFunction(SolFunctionBuilder function, bool overwrite = false)
        {
            if (!overwrite && FieldsLookup.ContainsKey(function.Name)) {
                throw new ArgumentException($"The function \"{function.Name}\" already exists, and overwrite it set to {false}!", nameof(function));
            }
            FunctionsLookup[function.Name] = function;
            return this;
        }

        /*#region Nested type: Annotateable

        /// <summary>
        ///     This subclass for <see cref="SolConstructWithMembersBuilder" /> allows to builder to additionally support annotations.
        /// </summary>
        public abstract class Annotateable : SolConstructWithMembersBuilder
        {
            /// <summary>
            ///     The actual list all annotations are stored inside.
            /// </summary>
            protected readonly List<SolAnnotationData> AnnotationsList = new List<SolAnnotationData>();

            /// <summary>
            ///     All annotations of this member builder.
            /// </summary>
            public IReadOnlyList<SolAnnotationData> Annotations => AnnotationsList;

            /// <summary>
            ///     Adds a new annotation to the ones already registered.
            /// </summary>
            /// <param name="annotation">The annotation data.</param>
            public Annotateable AddAnnotation(SolAnnotationData annotation)
            {
                AnnotationsList.Add(annotation);
                return this;
            }
        }
        
        #endregion*/

        #region Nested type: Generic

        /// <summary>
        ///     This class simply provides a generic type parameter to allow the builder syntax of the methods to return the most
        ///     derived type.
        /// </summary>
        /// <typeparam name="T">The most derived type(= type type that extends this class).</typeparam>
        public abstract class Generic<T> : SolConstructWithMembersBuilder where T : Generic<T>
        {
            /*#region Nested type: Annotateable

            /// <summary>
            ///     This subclass for <see cref="SolConstructWithMembersBuilder" /> allows to builder to additionally support annotations.
            /// </summary>
            public new abstract class Annotateable : SolConstructWithMembersBuilder
            {
                /// <summary>
                ///     The actual list all annotations are stored inside.
                /// </summary>
                protected readonly List<SolAnnotationData> AnnotationsList = new List<SolAnnotationData>();

                /// <summary>
                ///     All annotations of this member builder.
                /// </summary>
                public IReadOnlyList<SolAnnotationData> Annotations => AnnotationsList;

                /// <summary>
                ///     Adds a new annotation to the ones already registered.
                /// </summary>
                /// <param name="annotation">The annotation data.</param>
                public Annotateable AddAnnotation(SolAnnotationData annotation)
                {
                    AnnotationsList.Add(annotation);
                    return this;
                }
            }

            #endregion*/
            /// <inheritdoc cref="SolConstructWithMembersBuilder.AddField" />
            /// <exception cref="ArgumentException">
            ///     A field with this name already exists and and <paramref name="overwrite" /> is false.
            /// </exception>
            public new virtual T AddField(SolFieldBuilder field, bool overwrite = false)
            {
                return (T) base.AddField(field, overwrite);
            }

            /// <inheritdoc cref="SolConstructWithMembersBuilder.AddFunction" />
            /// <exception cref="ArgumentException">
            ///     A function with this name already exists and and <paramref name="overwrite" /> is false.
            /// </exception>
            public new virtual T AddFunction(SolFunctionBuilder function, bool overwrite = false)
            {
                return (T) base.AddFunction(function, overwrite);
            }
        }

        #endregion
    }
}