using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;

namespace SolScript.Interpreter.Library {
    public class SolLibrary {
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies) {
            Name = libraryName;
            m_Assemblies = sourceAssemblies;
        }

        internal const string STD_NAME = "std";

        private static readonly Dictionary<string, string> s_ClrMethodNameInfo = new Dictionary<string, string> {
            ["GetHashCode"] = null,
            ["GetType"] = null,
            ["ToString"] = "__to_string",
            // The built-in equals comparison if faster than marshalling the type to C# and performing a potentially costly operation.
            // todo: do regardless if the method has been overwritten.
            ["Equals"] = null
        };

        private static readonly Dictionary<string, string> s_ClrFieldNameInfo = new Dictionary<string, string>();
        private readonly Assembly[] m_Assemblies;
        private readonly Dictionary<SolAssembly, AssemblyClassInfo> m_AssemblyClasses = new Dictionary<SolAssembly, AssemblyClassInfo>();

        //private readonly Dictionary<string, ClassDef> m_Classes = new Dictionary<string, ClassDef>();
        //private readonly Dictionary<string, ClassDef> m_Types = new Dictionary<string, ClassDef>();
        //private readonly HashSet<string> s_ExcludedClrFields = new HashSet<string>();
        //private bool m_DidCreateClassDefinitions;

        public string Name { get; }
        public static SolLibrary StandardLibrary { get; } = new SolLibrary(STD_NAME, typeof (SolAssembly).Assembly);

        private AssemblyClassInfo GetAssemblyClassInfo(SolAssembly assembly) {
            AssemblyClassInfo info;
            if (m_AssemblyClasses.TryGetValue(assembly, out info)) {
                if (info.Assembly != assembly) throw new InvalidOperationException("The registered internal assemblies do not match.");
            } else {
                info = new AssemblyClassInfo(assembly);
                m_AssemblyClasses[assembly] = info;
            }
            return info;
        }

        //public ICollection<string> Functions => m_functions.Keys;
        //public ICollection<string> ClassNames => m_Types.Keys;
        //public ICollection<ClassDef> Classes => m_Types.Values;

        /// <summary> Builds the "hulls" of the classes without any actual content. This is
        ///     requires before building the actual contents of the library as types need
        ///     to be resolved in a static manner(as opposed to dynamic for the script
        ///     classes/functions). This is done to increase marshalling speed during
        ///     runtime. </summary>
        /// <returns> A collection of class definitions containing: Their
        ///     <see cref="ClassDef.ClrType"/> aswell as their <see cref="ClassDef.Mode"/>
        ///     and <see cref="ClassDef.Name"/>. </returns>
        public IReadOnlyCollection<SolClassBuilder> BuildClassHulls(SolAssembly forAssembly) {
            AssemblyClassInfo classInfo = GetAssemblyClassInfo(forAssembly);
            if (classInfo.DidCreateClassDefinitions) return classInfo.Builders;
            foreach (Assembly assembly in m_Assemblies) {
                foreach (Type type in assembly.GetTypes()) {
                    SolLibraryClassAttribute libraryClass = type.GetCustomAttribute<SolLibraryClassAttribute>();
                    if (libraryClass == null) continue;
                    if (libraryClass.LibraryName != Name) continue;
                    SolDebug.WriteLine("Library " + Name + " got: " + type.Name);
                    string className = type.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? type.Name;
                    SolTypeMode classMode = libraryClass.Mode;
                    SolClassBuilder builder = new SolClassBuilder(className, classMode).SetNativeType(type);
                    classInfo.AddBuilder(className, type, builder);
                }
            }
            classInfo.DidCreateClassDefinitions = true;
            return classInfo.Builders;
        }

        #region Nested type: AssemblyClassInfo

        /// <summary> A library is used universally between assemblies. Each of these data
        ///     classes holds the data for one assembly. </summary>
        private class AssemblyClassInfo {
            public AssemblyClassInfo(SolAssembly assembly) {
                Assembly = assembly;
                m_SolToBuilder = new Dictionary<string, SolClassBuilder>();
                m_NativeToBuilder = new Dictionary<Type, SolClassBuilder>();
            }

            public readonly SolAssembly Assembly;
            private readonly Dictionary<Type, SolClassBuilder> m_NativeToBuilder;
            private readonly Dictionary<string, SolClassBuilder> m_SolToBuilder;
            public bool DidCreateClassDefinitions;

            public IReadOnlyCollection<SolClassBuilder> Builders => m_NativeToBuilder.Values;

            public void AddBuilder(string solType, Type type, SolClassBuilder builder) {
                m_SolToBuilder.Add(solType, builder);
                m_NativeToBuilder.Add(type, builder);
            }

            public SolClassBuilder GetBuilder(string solType) {
                return m_SolToBuilder[solType];
            }

            public SolClassBuilder GetBuilder(Type nativeType) {
                return m_NativeToBuilder[nativeType];
            }

            public bool TryGetBuilder(Type nativeType, out SolClassBuilder builder) {
                return m_NativeToBuilder.TryGetValue(nativeType, out builder);
            }

            public bool TryGetBuilder(string solType, out SolClassBuilder builder) {
                return m_SolToBuilder.TryGetValue(solType, out builder);
            }
        }

        #endregion

        /// <summary> Builds the actual contents of the classes. This operation requires
        ///     you to have built the class hulls beforehand. </summary>
        /// <returns> A collection of fully operable class definitions. Ideally you should
        ///     never need to use the return value as they are the same references the the
        ///     ones returned from <see cref="BuildClassHulls(SolAssembly)"/>. </returns>
        /// <exception cref="InvalidOperationException"> The class hulls have no been built
        ///     yet.
        ///     <br/>
        ///     See also: <seealso cref="BuildClassHulls(SolAssembly)"/> </exception>
        public IReadOnlyCollection<SolClassBuilder> BuildClassBodies(SolAssembly forAssembly) {
            AssemblyClassInfo classInfo = GetAssemblyClassInfo(forAssembly);
            if (!classInfo.DidCreateClassDefinitions)
                throw new InvalidOperationException("Tried to build class bodies without having built the class hulls. Make sure to call BuildClassHulls() before BuildClassBodies().");
            foreach (SolClassBuilder builder in classInfo.Builders) {
                if (builder.NativeType == null) {
                    throw new InvalidOperationException(
                        "The ClrType of a class inside a SolLibrary is null. Did you manually temper with the class definitions or was it overwritten by another library?");
                }
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (ConstructorInfo constructor in builder.NativeType.GetConstructors(BINDING_FLAGS)) {
                    SolFunctionBuilder solConstructor;
                    if (TryBuildConstructor(forAssembly, constructor, out solConstructor)) {
                        builder.AddFunction(solConstructor);
                    }
                }
                if (builder.Functions.Count == 0) throw new InvalidOperationException("The class " + builder.Name + " does not define a constructor. Make sure to expose at least one constructor.");
                foreach (MethodInfo method in builder.NativeType.GetMethods(BINDING_FLAGS)) {
                    SolFunctionBuilder solMethod;
                    if (TryBuildMethod(forAssembly, method, out solMethod)) {
                        builder.AddFunction(solMethod);
                    }
                }
                foreach (InspectorField field in InspectorField.GetInspectorFields(builder.NativeType)) {
                    SolFieldBuilder solField;
                    if (TryBuildField(forAssembly, field, out solField)) {
                        builder.AddField(solField);
                    }
                }
                // ReSharper enable LoopCanBeConvertedToQuery
                // todo: annotations
                Type baseType = builder.NativeType.BaseType;
                SolClassBuilder baseTypeBuilder;
                if (baseType != null && classInfo.TryGetBuilder(baseType, out baseTypeBuilder)) {
                    builder.Extends(baseTypeBuilder.Name);
                }
            }
            return classInfo.Builders;
        }

        [ContractAnnotation("solField:null => false")]
        private bool TryBuildField(SolAssembly forAssembly, InspectorField field, [CanBeNull] out SolFieldBuilder solField) {
            SolLibraryVisibilityAttribute visibility = field.GetAttributes<SolLibraryVisibilityAttribute>(true).FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? field.IsPublic;
            if (!visible) {
                solField = null;
                return false;
            }
            string name = field.GetAttribute<SolLibraryNameAttribute>(true)?.Name;
            if (name == null && !s_ClrFieldNameInfo.TryGetValue(field.Name, out name)) {
                // todo: adjust naming conventions.
                name = field.Name;
            } else if (name == null) {
                // The name can still be null if the value in name info is null. Null values in
                // name info means that the function should be ignored.
                solField = null;
                return false;
            }
            // todo: annotations
            // todo: investigate if native fields may skip the data type declaration. 
            // (can be inferred from inspector field later on?) this would allow us to remove the assembly
            // parameter requirement from this method(an thus the other trybuild methods).
            solField = new SolFieldBuilder(name, SolMarshal.GetSolType(forAssembly, field.DataType)).MakeNativeField(field);
            return true;
        }

        [ContractAnnotation("solMethod:null => false")]
        private bool TryBuildMethod(SolAssembly forAssembly, MethodInfo method, [CanBeNull] out SolFunctionBuilder solMethod) {
            SolLibraryVisibilityAttribute visibility = method.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? method.IsPublic;
            if (!visible) {
                solMethod = null;
                return false;
            }
            string name = method.GetCustomAttribute<SolLibraryNameAttribute>()?.Name;
            if (name == null && !s_ClrMethodNameInfo.TryGetValue(method.Name, out name)) {
                // todo: adjust naming conventions.
                name = method.Name;
            } else if (name == null) {
                // The name can still be null if the value in name info is null. Null values in
                // name info means that the function should be ignored.
                solMethod = null;
                return false;
            }
            // todo: annotations
            solMethod = new SolFunctionBuilder(name).MakeNativeFunction(method);
            return true;
        }

        [ContractAnnotation("solConstructor:null => false")]
        private bool TryBuildConstructor(SolAssembly forAssembly, ConstructorInfo constructor, [CanBeNull] out SolFunctionBuilder solConstructor) {
            // todo: annotations                    
            SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? constructor.IsPublic;
            if (!visible) {
                solConstructor = null;
                return false;
            }
            solConstructor = new SolFunctionBuilder("__new").MakeNativeConstructor(constructor).SetInternal(true);
            return true;
        }

        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                   BindingFlags.Instance;
    }
}