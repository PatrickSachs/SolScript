using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;

namespace SolScript.Interpreter.Library
{
    public class SolLibrary
    {
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies)
        {
            Name = libraryName;
            m_Assemblies = sourceAssemblies;
            FallbackMethodPostProcessor = NativeMethodPostProcessor.GetDefault();
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
        private readonly Dictionary<string, NativeMethodPostProcessor> m_PostProcessors = new Dictionary<string, NativeMethodPostProcessor>();
        private NativeMethodPostProcessor m_FallbackMethodPostProcessor;

        public string Name { get; }
        public static SolLibrary StandardLibrary { get; } = new SolLibrary(STD_NAME, typeof(SolAssembly).Assembly);

        /// <summary>
        ///     This fallback processor will be used in case there is no explicitly defined post processor the a method.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     Cannot set the fallback method processor to null.
        ///     <paramref name="value" />
        /// </exception>
        public NativeMethodPostProcessor FallbackMethodPostProcessor {
            get { return m_FallbackMethodPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Cannot set the fallback method processor to null.");
                }
                m_FallbackMethodPostProcessor = value;
            }
        }

        /// <summary>
        ///     Gets the registered post processor for the given method name. If no expcilty defined post processor for this name
        ///     could be found, the <see cref="FallbackMethodPostProcessor" /> will be returned instead.
        /// </summary>
        /// <param name="name">The method name to look up for.</param>
        /// <returns>The post processor.</returns>
        private NativeMethodPostProcessor GetMethodPostProcessor(string name)
        {
            NativeMethodPostProcessor postProcessor;
            if (m_PostProcessors.TryGetValue(name, out postProcessor)) {
                return postProcessor;
            }
            return FallbackMethodPostProcessor;
        }

        /// <summary>
        ///     Registers a <see cref="NativeMethodPostProcessor" /> for a native method. These post processors are typically used
        ///     to adjust the name and
        ///     access modifiers of meta functions.
        /// </summary>
        /// <param name="methodName">
        ///     If a native method has this name it will be sent to the post processor for ... well
        ///     processing.
        /// </param>
        /// <param name="postProcessor">The actual post processor.</param>
        /// <exception cref="ArgumentNullException"><paramref name="postProcessor" /> is null.</exception>
        /// <exception cref="ArgumentException">Another processor with the same name has already been registered.</exception>
        public void RegisterMethodPostProcessor(string methodName, NativeMethodPostProcessor postProcessor)
        {
            m_PostProcessors.Add(methodName, postProcessor);
        }

        /// <summary>
        ///     Gets the class info for a certain assembly. The class info is used to store all of the assembly individual data of
        ///     a Library. If the assembly does not have a class info yet, it will be created.
        /// </summary>
        /// <param name="assembly">The assembly to get the class info of.</param>
        /// <returns>The class info.</returns>
        private AssemblyClassInfo GetAssemblyClassInfo(SolAssembly assembly)
        {
            AssemblyClassInfo info;
            if (m_AssemblyClasses.TryGetValue(assembly, out info)) {
                if (info.Assembly != assembly) {
                    throw new InvalidOperationException("The registered internal assemblies do not match.");
                }
            } else {
                info = new AssemblyClassInfo(assembly);
                m_AssemblyClasses[assembly] = info;
            }
            return info;
        }

        /// <summary>
        ///     Builds the "hulls" of the classes without any actual content. This is
        ///     requires before building the actual contents of the library as types need
        ///     to be resolved in a static manner(as opposed to dynamic for the script
        ///     classes/functions). This is done to increase marshalling speed during
        ///     runtime.
        /// </summary>
        /// <returns>
        ///     A collection of class definitions containing: Their
        ///     <see cref="ClassDef.ClrType" /> aswell as their <see cref="ClassDef.Mode" />
        ///     and <see cref="ClassDef.Name" />.
        /// </returns>
        public IReadOnlyCollection<SolClassBuilder> BuildClassHulls(SolAssembly forAssembly /*, out SolGlobalsBuilder globalsBuilder*/)
        {
            AssemblyClassInfo classInfo = GetAssemblyClassInfo(forAssembly);
            //globalsBuilder = classInfo.GlobalsBuilder;
            if (classInfo.DidCreateClassDefinitions) {
                return classInfo.Builders;
            }
            foreach (Assembly assembly in m_Assemblies) {
                foreach (Type type in assembly.GetTypes()) {
                    //SolDebug.WriteLine(type.Name);
                    SolLibraryClassAttribute libraryClass = type.GetCustomAttribute<SolLibraryClassAttribute>();
                    if (libraryClass != null && libraryClass.LibraryName == Name) {
                        SolDebug.WriteLine("Library " + Name + " got: " + type.Name);
                        string className = type.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? type.Name;
                        SolTypeMode classMode = libraryClass.Mode;
                        SolClassBuilder builder = new SolClassBuilder(className, classMode).SetNativeType(type);
                        classInfo.AddBuilder(className, type, builder);
                    }
                    SolGlobalAttribute globalClassAttribute = type.GetCustomAttribute<SolGlobalAttribute>();
                    if (globalClassAttribute != null && globalClassAttribute.Library == Name) {
                        SolDebug.WriteLine("Library " + Name + " scans global class: " + type.Name);
                        // todo: global native fields
                        // todo: does this belong in build class hulls?
                        // todo: investigate merging builder & definition
                        // todo: ivestigate where the scanning should be done
                        // todo: remove the work of having to scan the native stuff for types from the function impls(prolly belongs in type registry in case multiple libs are used)
                        foreach (MethodInfo method in type.GetMethods(BINDING_FLAGS)) {
                            SolGlobalAttribute globalMethodAttribute = method.GetCustomAttribute<SolGlobalAttribute>();
                            if (globalMethodAttribute != null && globalMethodAttribute.Library == Name) {
                                SolDebug.WriteLine("  Found " + method.Name);
                                SolFunctionBuilder solMethod;
                                if (TryBuildMethod(forAssembly, method, out solMethod)) {
                                    forAssembly.TypeRegistry.GlobalsBuilder.AddFunction(solMethod);
                                }
                            }
                        }
                    }
                }
            }
            classInfo.DidCreateClassDefinitions = true;
            return classInfo.Builders;
        }

        #region Nested type: AssemblyClassInfo

        /// <summary>
        ///     A library is used universally between assemblies. Each of these data
        ///     classes holds the data for one assembly.
        /// </summary>
        private class AssemblyClassInfo
        {
            public AssemblyClassInfo(SolAssembly assembly)
            {
                Assembly = assembly;
                m_SolToBuilder = new Dictionary<string, SolClassBuilder>();
                m_NativeToBuilder = new Dictionary<Type, SolClassBuilder>();
                //GlobalsBuilder = new SolGlobalsBuilder();
            }

            public readonly SolAssembly Assembly;
            private readonly Dictionary<Type, SolClassBuilder> m_NativeToBuilder;
            private readonly Dictionary<string, SolClassBuilder> m_SolToBuilder;
            //public readonly SolGlobalsBuilder GlobalsBuilder;
            public bool DidCreateClassDefinitions;

            public IReadOnlyCollection<SolClassBuilder> Builders => m_NativeToBuilder.Values;

            public void AddBuilder(string solType, Type type, SolClassBuilder builder)
            {
                m_SolToBuilder.Add(solType, builder);
                m_NativeToBuilder.Add(type, builder);
            }

            public SolClassBuilder GetBuilder(string solType)
            {
                return m_SolToBuilder[solType];
            }

            public SolClassBuilder GetBuilder(Type nativeType)
            {
                return m_NativeToBuilder[nativeType];
            }

            public bool TryGetBuilder(Type nativeType, out SolClassBuilder builder)
            {
                return m_NativeToBuilder.TryGetValue(nativeType, out builder);
            }

            public bool TryGetBuilder(string solType, out SolClassBuilder builder)
            {
                return m_SolToBuilder.TryGetValue(solType, out builder);
            }
        }

        #endregion

        #region Nested type: NativeMethodPostProcessor

        /// <summary>
        ///     A <see cref="NativeMethodPostProcessor" /> is used to modify certain aspects of a native function during creation.
        ///     This allows to remap the name of the method(e.g this is internally used to remap ToString() to __to_string()).
        /// </summary>
        public abstract class NativeMethodPostProcessor
        {
            /// <summary>
            ///     Returns a default implementation of the <see cref="NativeMethodPostProcessor" />, returning all values according to
            ///     the speicifed default values.
            /// </summary>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetDefault()
            {
                return Default.Instance;
            }

            // todo: provide a way for the implementation to specific which aspects should even be touched by the post processor. 
            // This may not be needed now, but could be important as the post processor scales.
            /// <summary>
            ///     By default an explicit <see cref="SolLibraryNameAttribute" /> overrides the result of the <see cref="GetName" />
            ///     method, and so on. If this value is true however, the explict arguments are ignored and the results of the method
            ///     calls of this post processor take precedence.
            /// </summary>
            /// <param name="method">The method referene.</param>
            /// <returns>If the attributes on this method should be overridden.</returns>
            public virtual bool OverridesExplicitAttributes(MethodInfo method) => false;

            /// <summary>
            ///     If this returns true no function for this method can be created. By default all methods can be created.
            /// </summary>
            public virtual bool DoesFailCreation(MethodInfo method) => false;

            /// <summary>
            ///     Gets the remapped function name. The default is <see cref="MethodInfo.Name" />.
            /// </summary>
            /// <param name="method">The method referene.</param>
            /// <returns>The new function name to use in SolScript.</returns>
            public virtual string GetName(MethodInfo method) => method.Name;

            /// <summary>
            ///     Gets the remapped function <see cref="AccessModifier" />. Default is <see cref="AccessModifier.None" />.
            /// </summary>
            /// <param name="method">The method referene.</param>
            /// <returns>The new function <see cref="AccessModifier" /> to use in SolScript.</returns>
            public virtual AccessModifier GetAccessModifier(MethodInfo method) => AccessModifier.None;

            #region Nested type: Default

            internal sealed class Default : NativeMethodPostProcessor
            {
                private Default() {}
                public static readonly Default Instance = new Default();
            }

            #endregion
        }

        #endregion

        /// <summary>
        ///     Builds the actual contents of the classes. This operation requires
        ///     you to have built the class hulls beforehand.
        /// </summary>
        /// <returns>
        ///     A collection of fully operable class definitions. Ideally you should
        ///     never need to use the return value as they are the same references the the
        ///     ones returned from <see cref="BuildClassHulls(SolAssembly)" />.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The class hulls have no been built
        ///     yet.
        ///     <br />
        ///     See also: <seealso cref="BuildClassHulls(SolAssembly)" />
        /// </exception>
        public IReadOnlyCollection<SolClassBuilder> BuildClassBodies(SolAssembly forAssembly)
        {
            AssemblyClassInfo classInfo = GetAssemblyClassInfo(forAssembly);
            if (!classInfo.DidCreateClassDefinitions) {
                throw new InvalidOperationException("Tried to build class bodies without having built the class hulls. Make sure to call BuildClassHulls() before BuildClassBodies().");
            }
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
                if (builder.Functions.Count == 0) {
                    throw new InvalidOperationException("The class " + builder.Name + " does not define a constructor. Make sure to expose at least one constructor.");
                }
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
        private bool TryBuildField(SolAssembly forAssembly, InspectorField field, [CanBeNull] out SolFieldBuilder solField)
        {
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
            AccessModifier accessModifier = field.GetAttribute<SolLibraryAccessModifierAttribute>(true)?.AccessModifier ?? AccessModifier.None;
            // (can be inferred from inspector field later on?) this would allow us to remove the assembly
            // parameter requirement from this method(an thus the other trybuild methods).
            solField = new SolFieldBuilder(name, SolMarshal.GetSolType(forAssembly, field.DataType)).MakeNativeField(field).SetAccessModifier(accessModifier);
            return true;
        }

        /// <summary>
        ///     Tries to create the function builder for the given method.
        /// </summary>
        /// <param name="forAssembly">The assembly to work in.</param>
        /// <param name="method">The method to create the builder for.</param>
        /// <param name="solMethod">The created builder. Only valid if the method returned true.</param>
        /// <returns>true if the builder could be created, false if not.</returns>
        [ContractAnnotation("solMethod:null => false")]
        private bool TryBuildMethod(SolAssembly forAssembly, MethodInfo method, [CanBeNull] out SolFunctionBuilder solMethod)
        {
            SolLibraryVisibilityAttribute visibility = method.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? method.IsPublic;
            if (!visible) {
                solMethod = null;
                return false;
            }
            string name;
            AccessModifier access;
            NativeMethodPostProcessor postProcessor = GetMethodPostProcessor(method.Name);
            SolLibraryNameAttribute nameAttribute = method.GetCustomAttribute<SolLibraryNameAttribute>();
            if (postProcessor.DoesFailCreation(method)) {
                solMethod = null;
                return false;
            }
            if (postProcessor.OverridesExplicitAttributes(method)) {
                name = postProcessor.GetName(method);
                access = postProcessor.GetAccessModifier(method);
            } else {
                SolLibraryAccessModifierAttribute accessAttribute = method.GetCustomAttribute<SolLibraryAccessModifierAttribute>();
                name = nameAttribute?.Name ?? postProcessor.GetName(method); 
                access = accessAttribute?.AccessModifier ?? postProcessor.GetAccessModifier(method); 
            }
            // todo: annotations for native functions. if it has a native attribute that is an annotation add it here.
            solMethod = new SolFunctionBuilder(name).MakeNativeFunction(method).SetAccessModifier(access);
            return true;
        }

        [ContractAnnotation("solConstructor:null => false")]
        private bool TryBuildConstructor(SolAssembly forAssembly, ConstructorInfo constructor, [CanBeNull] out SolFunctionBuilder solConstructor)
        {
            // todo: annotations                    
            SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? constructor.IsPublic;
            if (!visible) {
                solConstructor = null;
                return false;
            }
            AccessModifier accessModifier = constructor.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? AccessModifier.Internal;
            solConstructor = new SolFunctionBuilder("__new").MakeNativeConstructor(constructor).SetAccessModifier(accessModifier);
            return true;
        }

        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                   BindingFlags.Instance;
    }
}