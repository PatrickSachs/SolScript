using System;
using System.Collections.Generic;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     A class builder is used to define the script created classes before they are processed
    ///     by the TypeRegistry.
    /// </summary>
    public sealed class SolClassBuilder : SolMemberBuilder.Generic<SolClassBuilder>
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

        public IReadOnlyCollection<SolAnnotationData> Annotations => m_Annotations;

        public void AddAnnotation(SolAnnotationData annotation)
        {
            m_Annotations.Add(annotation);
        }

        public string BaseClass { get; set; }

        /// <summary> The native class this SolClass will marshal to and from. </summary>
        public Type NativeType { get; set; }

        /// <summary> The name of the class. </summary>
        public string Name { get; set; }

        /// <summary> Which type of class is the created class? </summary>
        public SolTypeMode TypeMode { get; set; }

        public SolClassBuilder SetNativeType(Type type)
        {
            NativeType = type;
            return this;
        }

        public SolClassBuilder Extends(string baseClass)
        {
            BaseClass = baseClass;
            return this;
        }
    }
}