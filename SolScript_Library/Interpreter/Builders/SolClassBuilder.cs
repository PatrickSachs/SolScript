using System;
using System.Collections.Generic;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     A class builder is used to define the script created classes before they are processed
    ///     by the TypeRegistry.
    /// </summary>
    public sealed class SolClassBuilder : SolConstructWithMembersBuilder.Generic<SolClassBuilder>, IAnnotateableBuilder
    {
        /// <summary> Creates a new class builder. </summary>
        /// <param name="name"> The type name of the class you wish to create. </param>
        /// <param name="typeMode"> Which type of class is the created class? </param>
        public SolClassBuilder(string name, SolTypeMode typeMode)
        {
            Name = name;
            TypeMode = typeMode;
        }

        private readonly List<SolAnnotationData> m_Annotations = new List<SolAnnotationData>();

        public string BaseClass { get; set; }

        /// <summary> The native class this SolClass will marshal to and from. </summary>
        public Type NativeType { get; set; }

        /// <summary> The name of the class. </summary>
        public string Name { get; set; }

        /// <summary> Which type of class is the created class? </summary>
        public SolTypeMode TypeMode { get; set; }

        public SolSourceLocation Location { get; set; }

        #region IAnnotateableBuilder Members

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotation(SolAnnotationData annotation)
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
        public IAnnotateableBuilder AddAnnotations(params SolAnnotationData[] annotations)
        {
            m_Annotations.AddRange(annotations);
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<SolAnnotationData> Annotations => m_Annotations;

        #endregion

        public SolClassBuilder AtLocation(SolSourceLocation location)
        {
            Location = location;
            return this;
        }

        public SolClassBuilder SetNativeType(Type type)
        {
            NativeType = type;
            Location = SolSourceLocation.Native();
            return this;
        }

        public SolClassBuilder Extends(string baseClass)
        {
            BaseClass = baseClass;
            return this;
        }
    }
}