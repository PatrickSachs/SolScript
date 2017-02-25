using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Builders;

namespace SolScript.Interpreter.Library
{
    public class SolLibrary
    {
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies)
        {
            Name = libraryName;
            m_Assemblies = sourceAssemblies;
            FallbackMethodPostProcessor = NativeMethodPostProcessor.GetDefault();
            // object Methods
            RegisterMethodPostProcessor(nameof(GetHashCode), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(GetType), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(Equals), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(ToString), NativeMethodPostProcessor.GetRenamerAndAccessorAndReturn(SolMetaKey.Stringify.Name, SolAccessModifier.Internal, SolMetaKey.Stringify.Type));
            // Annotations
            NativeMethodPostProcessor internalPostProcessor = NativeMethodPostProcessor.GetAccessor(SolAccessModifier.Internal);
            RegisterMethodPostProcessor(SolMetaKey.AnnotationGetVariable.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaKey.AnnotationSetVariable.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaKey.AnnotationCallFunction.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaKey.AnnotationPreConstructor.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaKey.AnnotationPostConstructor.Name, internalPostProcessor);
            // todo: meta function post processors (detect operators).
            FallbackFieldPostProcessor = NativeFieldPostProcessor.GetDefault();
        }

        internal const string STD_NAME = "std";
        private readonly Assembly[] m_Assemblies;
        private readonly Dictionary<SolAssembly, AssemblyClassInfo> m_AssemblyClasses = new Dictionary<SolAssembly, AssemblyClassInfo>();
        private readonly Dictionary<string, NativeFieldPostProcessor> m_FieldPostProcessors = new Dictionary<string, NativeFieldPostProcessor>();
        private readonly Dictionary<string, NativeMethodPostProcessor> m_MethodPostProcessors = new Dictionary<string, NativeMethodPostProcessor>();
        private NativeFieldPostProcessor m_FallbackFieldPostProcessor;
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
        ///     This fallback processor will be used in case there is no explicitly defined post processor the a field.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     Cannot set the fallback field processor to null.
        ///     <paramref name="value" />
        /// </exception>
        public NativeFieldPostProcessor FallbackFieldPostProcessor {
            get { return m_FallbackFieldPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Cannot set the fallback field processor to null.");
                }
                m_FallbackFieldPostProcessor = value;
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
            if (m_MethodPostProcessors.TryGetValue(name, out postProcessor)) {
                return postProcessor;
            }
            return FallbackMethodPostProcessor;
        }

        /// <summary>
        ///     Gets the registered post processor for the given field name. If no expcilty defined post processor for this name
        ///     could be found, the <see cref="FallbackFieldPostProcessor" /> will be returned instead.
        /// </summary>
        /// <param name="name">The field name to look up for.</param>
        /// <returns>The post processor.</returns>
        private NativeFieldPostProcessor GetFieldPostProcessor(string name)
        {
            NativeFieldPostProcessor postProcessor;
            if (m_FieldPostProcessors.TryGetValue(name, out postProcessor)) {
                return postProcessor;
            }
            return FallbackFieldPostProcessor;
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
        /// <param name="overrideExisting">If this is true already existing post processors with the same name will be overridden.</param>
        /// <exception cref="ArgumentNullException"><paramref name="postProcessor" /> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     Another processor with the same name has already been registered(Only if
        ///     <paramref name="overrideExisting" /> is false).
        /// </exception>
        public void RegisterMethodPostProcessor(string methodName, NativeMethodPostProcessor postProcessor, bool overrideExisting = false)
        {
            if (overrideExisting) {
                m_MethodPostProcessors[methodName] = postProcessor;
            } else {
                m_MethodPostProcessors.Add(methodName, postProcessor);
            }
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

        #region Nested type: NativeFieldPostProcessor

        /// <summary>
        ///     A <see cref="NativeFieldPostProcessor" /> is used to modify certain aspects of a native field(or potentially a
        ///     property) during creation.
        /// </summary>
        public abstract class NativeFieldPostProcessor
        {
            /// <summary>
            ///     Returns a default implementation of the <see cref="NativeFieldPostProcessor" />, returning all values according to
            ///     the speicifed default values.
            /// </summary>
            /// <returns>The post processor instance.</returns>
            public static NativeFieldPostProcessor GetDefault()
            {
                return Default.Instance;
            }

            // todo: provide a way for the implementation to specify which aspects should even be touched by the post processor. 
            // This may not be needed now, but could be important as the post processor scales.
            /// <summary>
            ///     By default an explicit <see cref="SolLibraryNameAttribute" /> overrides the result of the <see cref="GetName" />
            ///     method, and so on. If this value is true however, the explict arguments are ignored and the results of the method
            ///     calls of this post processor take precedence.
            /// </summary>
            /// <param name="field">The field referene.</param>
            /// <returns>If the attributes on this field should be overridden.</returns>
            public virtual bool OverridesExplicitAttributes(FieldOrPropertyInfo field) => false;

            /// <summary>
            ///     If this returns true no field for this method can be created. By default all fields can be created.
            /// </summary>
            public virtual bool DoesFailCreation(FieldOrPropertyInfo method) => false;

            /// <summary>
            ///     Gets the remapped function name. The default is <see cref="FieldOrPropertyInfo.Name" />.
            /// </summary>
            /// <param name="field">The field referene.</param>
            /// <returns>The new field name to use in SolScript.</returns>
            public virtual string GetName(FieldOrPropertyInfo field) => field.Name;

            /// <summary>
            ///     Gets the remapped field type. The default is either marshalled from the actual native field type or inferred from
            ///     one of its attributes(but they will be determined at a later stage; once the definitions are being generated).
            /// </summary>
            /// <param name="field">The field referene.</param>
            /// <returns>The remapped field type, or null if you do not wish to remap.</returns>
            /// <remarks>
            ///     Very important: If you do not wish to remap the field type you must return null and NOT the default SolType
            ///     value.
            /// </remarks>
            public virtual SolType? GetFieldType(FieldOrPropertyInfo field) => null;

            /// <summary>
            ///     Gets the remapped field <see cref="SolAccessModifier" />. Default is <see cref="SolAccessModifier.None" />.
            /// </summary>
            /// <param name="field">The field referene.</param>
            /// <returns>The new field <see cref="SolAccessModifier" /> to use in SolScript.</returns>
            public virtual SolAccessModifier GetAccessModifier(FieldOrPropertyInfo field) => SolAccessModifier.None;

            #region Nested type: Default

            private sealed class Default : NativeFieldPostProcessor
            {
                private Default() {}
                public static readonly Default Instance = new Default();
            }

            #endregion
        }

        #endregion

        #region Nested type: NativeMethodPostProcessor

        /// <summary>
        ///     A <see cref="NativeMethodPostProcessor" /> is used to modify certain aspects of a native function during creation.
        ///     This for example allows to remap the name of the method(e.g this is internally used to remap ToString() to
        ///     __to_string()).
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

            /// <summary>
            ///     Returns a default implementation of the <see cref="NativeMethodPostProcessor" /> that always fails the creation of
            ///     the function. This is useful if you want to hide a function entirely.
            /// </summary>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetFailer()
            {
                return Fail.Instance;
            }

            /// <summary>
            ///     Returns a <see cref="NativeMethodPostProcessor" /> that generates all default values aside from the name which is
            ///     the fixed value.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetRenamer(string name)
            {
                return new Rename(name);
            }

            /// <summary>
            ///     Returns a <see cref="NativeMethodPostProcessor" /> that generates all default values aside from the
            ///     <see cref="SolAccessModifier" /> which is a fixed values.
            /// </summary>
            /// <param name="access">The access modifiers.</param>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetAccessor(SolAccessModifier access)
            {
                return new Access(access);
            }

            /// <summary>
            ///     Returns a <see cref="NativeMethodPostProcessor" /> that generates all default values aside from the name &
            ///     <see cref="SolAccessModifier" /> which are fixed values.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="access">The access modifiers.</param>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetRenamerAndAccessor(string name, SolAccessModifier access)
            {
                return new RenameAccess(name, access);
            }

            /// <summary>
            ///     Returns a <see cref="NativeMethodPostProcessor" /> that generates all default values aside from the name,
            ///     <see cref="SolAccessModifier" /> & return type which are fixed values.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="access">The access modifiers.</param>
            /// <param name="returnType">The return type.</param>
            /// <returns>The post processor instance.</returns>
            public static NativeMethodPostProcessor GetRenamerAndAccessorAndReturn(string name, SolAccessModifier access, SolType returnType)
            {
                return new RenameAccessReturn(name, access, returnType);
            }

            // todo: provide a way for the implementation to specify which aspects should even be touched by the post processor. 
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
            ///     Gets the remapped return type. The default is either marshalled from the actual native return type or inferred from
            ///     one of its attributes(but they will be determined at a later stage; once the definitions are being generated).
            /// </summary>
            /// <param name="method">The method referene.</param>
            /// <returns>The remapped return type, or null if you do not wish to remap.</returns>
            /// <remarks>
            ///     Very important: If you do not wish to remap the return type you must return null and NOT the default SolType
            ///     value.
            /// </remarks>
            public virtual SolType? GetReturn(MethodInfo method) => null;

            /// <summary>
            ///     Gets the remapped function <see cref="SolAccessModifier" />. Default is <see cref="SolAccessModifier.None" />.
            /// </summary>
            /// <param name="method">The method referene.</param>
            /// <returns>The new function <see cref="SolAccessModifier" /> to use in SolScript.</returns>
            public virtual SolAccessModifier GetAccessModifier(MethodInfo method) => SolAccessModifier.None;

            #region Nested type: Access

            private class Access : NativeMethodPostProcessor
            {
                public Access(SolAccessModifier access)
                {
                    m_Access = access;
                }

                private readonly SolAccessModifier m_Access;

                #region Overrides

                public override SolAccessModifier GetAccessModifier(MethodInfo method) => m_Access;

                #endregion
            }

            #endregion

            #region Nested type: Default

            private sealed class Default : NativeMethodPostProcessor
            {
                private Default() {}
                public static readonly Default Instance = new Default();
            }

            #endregion

            #region Nested type: Fail

            private sealed class Fail : NativeMethodPostProcessor
            {
                private Fail() {}
                public static readonly Fail Instance = new Fail();

                #region Overrides

                /// <inheritdoc />
                public override bool DoesFailCreation(MethodInfo method) => true;

                #endregion
            }

            #endregion

            #region Nested type: Rename

            private class Rename : NativeMethodPostProcessor
            {
                public Rename(string name)
                {
                    m_Name = name;
                }

                private readonly string m_Name;

                #region Overrides

                public override string GetName(MethodInfo method) => m_Name;

                #endregion
            }

            #endregion

            #region Nested type: RenameAccess

            private class RenameAccess : Rename
            {
                public RenameAccess(string name, SolAccessModifier access) : base(name)
                {
                    m_Access = access;
                }

                private readonly SolAccessModifier m_Access;

                #region Overrides

                public override SolAccessModifier GetAccessModifier(MethodInfo method) => m_Access;

                #endregion
            }

            #endregion

            #region Nested type: RenameAccessReturn

            private sealed class RenameAccessReturn : RenameAccess
            {
                /// <inheritdoc />
                public RenameAccessReturn(string name, SolAccessModifier access, SolType returnType) : base(name, access)
                {
                    m_ReturnType = returnType;
                }

                private readonly SolType m_ReturnType;

                #region Overrides

                /// <inheritdoc />
                public override SolType? GetReturn(MethodInfo method) => m_ReturnType;

                #endregion
            }

            #endregion
        }

        #endregion

        // todo: ctor post processor
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
                    string str = method.Name;
                    Attribute[] attr = method.GetCustomAttributes().ToArray();
                    if (method.IsSpecialName) {
                        continue;
                    }
                    if (TryBuildMethod(forAssembly, method, out solMethod)) {
                        builder.AddFunction(solMethod);
                    }
                }
                foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(builder.NativeType)) {
                    SolFieldBuilder solField;
                    if (field.IsSpecialName)
                    {
                        continue;
                    }
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
        private bool TryBuildField(SolAssembly forAssembly, FieldOrPropertyInfo field, [CanBeNull] out SolFieldBuilder solField)
        {
            // todo: investigate if invisibility should truly take precendence over the post processor enforcing creation control over attributes. ("OverrideExplicitAttributes")
            SolLibraryVisibilityAttribute visibility = field.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? field.IsPublic;
            if (!visible) {
                solField = null;
                return false;
            }
            string name;
            SolAccessModifier access;
            SolType? remappedType;
            NativeFieldPostProcessor postProcessor = GetFieldPostProcessor(field.Name);
            if (postProcessor.DoesFailCreation(field)) {
                solField = null;
                return false;
            }
            if (postProcessor.OverridesExplicitAttributes(field)) {
                name = postProcessor.GetName(field);
                access = postProcessor.GetAccessModifier(field);
                remappedType = postProcessor.GetFieldType(field);
            } else {
                // todo: auto create sol style naming if desired
                name = field.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? postProcessor.GetName(field);
                access = field.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? postProcessor.GetAccessModifier(field);
                remappedType = field.GetCustomAttribute<SolContractAttribute>()?.GetSolType() ?? postProcessor.GetFieldType(field);
            }
            solField = new SolFieldBuilder(name).MakeNativeField(field).SetAccessModifier(access);
            if (remappedType.HasValue) {
                solField.FieldNativeType(remappedType.Value);
            } else {
                solField.FieldType(SolMarshal.GetSolType(forAssembly, field.DataType));
            }
            // todo: annotations for native fields
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
            // todo: investigate if invisibility should truly take precendence over the post processor enforcing creation control over attributes. ("OverrideExplicitAttributes")
            SolLibraryVisibilityAttribute visibility = method.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? method.IsPublic;
            if (!visible) {
                solMethod = null;
                return false;
            }
            string name;
            SolAccessModifier access;
            SolType? remappedReturn;
            NativeMethodPostProcessor postProcessor = GetMethodPostProcessor(method.Name);
            if (postProcessor.DoesFailCreation(method)) {
                solMethod = null;
                return false;
            }
            if (postProcessor.OverridesExplicitAttributes(method)) {
                name = postProcessor.GetName(method);
                access = postProcessor.GetAccessModifier(method);
                remappedReturn = postProcessor.GetReturn(method);
            } else {
                name = method.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? postProcessor.GetName(method);
                access = method.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? postProcessor.GetAccessModifier(method);
                remappedReturn = method.GetCustomAttribute<SolContractAttribute>()?.GetSolType() ?? postProcessor.GetReturn(method);
            }
            // todo: annotations for native functions. if it has a native attribute that is an annotation add it here.
            solMethod = new SolFunctionBuilder(name).MakeNativeFunction(method).SetAccessModifier(access);
            if (remappedReturn.HasValue) {
                solMethod.NativeReturns(remappedReturn.Value);
            }
            return true;
        }

        [ContractAnnotation("solConstructor:null => false")]
        private bool TryBuildConstructor(SolAssembly forAssembly, ConstructorInfo constructor, [CanBeNull] out SolFunctionBuilder solConstructor)
        {
            // todo: flesh out ctors as well as functions
            // todo: annotations                    
            SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? constructor.IsPublic;
            if (!visible) {
                solConstructor = null;
                return false;
            }
            SolAccessModifier accessModifier = constructor.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? SolAccessModifier.Internal;
            solConstructor = new SolFunctionBuilder("__new").MakeNativeConstructor(constructor).SetAccessModifier(accessModifier);
            return true;
        }

        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                   BindingFlags.Instance;
    }
}