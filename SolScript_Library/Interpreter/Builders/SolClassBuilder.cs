using System;
using System.Collections.Generic;
using PSUtility.Enumerables;
using SolScript.Utility;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     A class builder is used to define the script created classes before they are processed
    ///     by the TypeRegistry.
    /// </summary>
    public sealed class SolClassBuilder : SolConstructWithMembersBuilder.Generic<SolClassBuilder>, IAnnotateableBuilder, ISourceLocateable
    {
        /// <summary> Creates a new class builder. </summary>
        /// <param name="name"> The type name of the class you wish to create. </param>
        /// <param name="typeMode"> Which type of class is the created class? </param>
        public SolClassBuilder(string name, SolTypeMode typeMode)
        {
            Name = name;
            TypeMode = typeMode;
        }

        // All annotations on this class.
        private readonly PSUtility.Enumerables.List<SolAnnotationBuilder> m_Annotations = new PSUtility.Enumerables.List<SolAnnotationBuilder>();

        /// <summary>
        ///     The name of the base class(= the class this one extends).
        /// </summary>
        public string BaseClass { get; private set; }

        /// <summary> The native class this SolClass will marshal to and from. </summary>
        public Type NativeType { get; private set; }

        /// <summary> The name of the class. </summary>
        public string Name { get; }

        /// <summary> Which type of class is the created class? </summary>
        public SolTypeMode TypeMode { get; }
        
        #region IAnnotateableBuilder Members

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotation(SolAnnotationBuilder annotation)
        {
            m_Annotations.Add(annotation);
            return this;
        }

        /// <inheritdoc />
        public IAnnotateableBuilder ClearAnnotations()
        {
            m_Annotations.Clear();
            return this;
        }

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotations(params SolAnnotationBuilder[] annotations)
        {
            m_Annotations.AddRange(annotations);
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<SolAnnotationBuilder> Annotations => m_Annotations;

        #endregion

        #region ISourceLocateable Members

        /// <summary>
        ///     The location in source code this class was defined at.
        /// </summary>
        public SolSourceLocation Location { get; private set; }

        #endregion

        /// <inheritdoc cref="Location" />
        /// <exception cref="InvalidOperationException">A native class cannot have a source location.</exception>
        /// <see cref="NativeType"/>
        public SolClassBuilder SetLocation(SolSourceLocation location)
        {
            if (NativeType != null) {
                throw new InvalidOperationException("A native class cannot have a source location.");
            }
            Location = location;
            return this;
        }

        /// <inheritdoc cref="NativeType" />
        public SolClassBuilder SetNativeType(Type type)
        {
            NativeType = type;
            Location = SolSourceLocation.Native();
            return this;
        }

        /// <inheritdoc cref="BaseClass" />
        public SolClassBuilder SetBaseClass(string baseClass)
        {
            BaseClass = baseClass;
            return this;
        }
    }
}