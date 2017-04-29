using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Library
{
    public class SolLibrary
    {
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies)
        {
            Name = libraryName;
            m_Assemblies = new Array<Assembly>(sourceAssemblies);
            /*FallbackMethodPostProcessor = NativeMethodPostProcessor.GetDefault();
            // object Methods
            RegisterMethodPostProcessor(nameof(GetHashCode), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(GetType), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(Equals), NativeMethodPostProcessor.GetFailer());
            RegisterMethodPostProcessor(nameof(ToString), NativeMethodPostProcessor.GetRenamerAndAccessorAndReturn(SolMetaFunction.__to_string.Name, SolAccessModifier.Internal, SolMetaFunction.__to_string.Type));
            // Annotations
            NativeMethodPostProcessor internalPostProcessor = NativeMethodPostProcessor.GetAccessor(SolAccessModifier.Internal);
            RegisterMethodPostProcessor(SolMetaFunction.__a_get_variable.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaFunction.__a_set_variable.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaFunction.__a_pre_new.Name, internalPostProcessor);
            RegisterMethodPostProcessor(SolMetaFunction.__a_post_new.Name, internalPostProcessor);
            // todo: meta function post processors (detect operators).
            // Fields
            FallbackFieldPostProcessor = NativeFieldPostProcessor.GetDefault();
            m_FieldPostProcessors.Add(nameof(Attribute.TypeId), new NativeFieldPostProcessor.FailOnType(typeof(Attribute)));
            m_FieldPostProcessors.Add(nameof(INativeClassSelf.Self), new NativeFieldPostProcessor.FailOnInterface(typeof(INativeClassSelf)));*/
        }

        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                   BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private const BindingFlags GLOBAL_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                                          BindingFlags.DeclaredOnly;

        private readonly Array<Assembly> m_Assemblies;
        private readonly Dictionary<string, NativeFieldPostProcessor> m_FieldPostProcessors = new Dictionary<string, NativeFieldPostProcessor>();
        /*private readonly List<SolFieldBuilder> m_GlobalFieldBuilders = new List<SolFieldBuilder>();
        private readonly List<SolFunctionBuilder> m_GlobalFunctions = new List<SolFunctionBuilder>();
        private readonly Dictionary<string, NativeMethodPostProcessor> m_MethodPostProcessors = new Dictionary<string, NativeMethodPostProcessor>();
        private readonly Dictionary<Type, SolClassBuilder> m_NativeToBuilder = new Dictionary<Type, SolClassBuilder>();
        private readonly Dictionary<Type, Type> m_NativeToDescriptor = new Dictionary<Type, Type>();
        private readonly Dictionary<string, SolClassBuilder> m_SolToBuilder = new Dictionary<string, SolClassBuilder>();*/
        //private NativeFieldPostProcessor m_FallbackFieldPostProcessor;
        //private NativeMethodPostProcessor m_FallbackMethodPostProcessor;

        public IReadOnlyList<Assembly> Assemblies {
            get { return m_Assemblies; }
        }

        /*/// <summary>
        ///     All classes in this library.
        /// </summary>
        /// <exception cref="InvalidOperationException">The builders have not been created yet.</exception>
        /// <seealso cref="HasBeenCreated" />
        public IReadOnlyDictionary<string, SolClassBuilder> Classes {
            get {
                if (!HasBeenCreated) {
                    throw new InvalidOperationException("Cannot obtain library " + Name + " class builders if they are not created yet.");
                }
                return m_SolToBuilder;
            }
        }*/

        /*/// <summary>
        ///     All global fields in this library.
        /// </summary>
        /// <exception cref="InvalidOperationException">The builders have not been created yet.</exception>
        /// <seealso cref="HasBeenCreated" />
        public IReadOnlyList<SolFieldBuilder> GlobalFields {
            get {
                if (!HasBeenCreated) {
                    throw new InvalidOperationException("Cannot obtain library " + Name + " field builders if they are not created yet.");
                }
                return m_GlobalFieldBuilders;
            }
        }

        /// <summary>
        ///     All global functions in this library.
        /// </summary>
        /// <exception cref="InvalidOperationException">The builders have not been created yet.</exception>
        /// <seealso cref="HasBeenCreated" />
        public IReadOnlyList<SolFunctionBuilder> GlobalFunctions {
            get {
                if (!HasBeenCreated) {
                    throw new InvalidOperationException("Cannot obtain library " + Name + " function builders if they are not created yet.");
                }
                return m_GlobalFunctions;
            }
        }*/

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public string Name { get; }

        //public IReadOnlyDictionary<Type, Type> NativeToDescriptor => m_NativeToDescriptor;

        /*/// <summary>
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
        }*/

        /// <summary>
        ///     Have the builders of this library been created?
        /// </summary>
        public bool HasBeenCreated { get; private set; }

        /*/// <summary>
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
        }*/

        /*/// <summary>
        ///     Creates the library by creating all builders for the registered classes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The library has already been created.</exception>
        /// <seealso cref="HasBeenCreated" />
        public void Create()
        {
            if (HasBeenCreated) {
                throw new InvalidOperationException("The library " + Name + " has already been created.");
            }
            foreach (Assembly assembly in m_Assemblies) {
                // === Create Builder Hulls
                // Two steps needed to determine base classes.
                foreach (Type type in assembly.GetTypes()) {
                    SolTypeDescriptorAttribute typeDescriptor = type.GetCustomAttribute<SolTypeDescriptorAttribute>();
                    if (typeDescriptor != null && typeDescriptor.LibraryName == Name) {
                        string className = type.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? type.Name;
                        SolDebug.WriteLine("Library " + Name + " got descriptor: " + type.Name + " (Describes: " + typeDescriptor.Describes.Name + ") => " + className);
                        SolTypeMode classMode = typeDescriptor.TypeMode;
                        SolClassBuilder builder = new SolClassBuilder(className, classMode).SetNativeType(type).MakeDescriptor(typeDescriptor.Describes);
                        m_SolToBuilder.Add(className, builder);
                        m_NativeToBuilder.Add(typeDescriptor.Describes, builder);
                        m_NativeToDescriptor.Add(typeDescriptor.Describes, type);
                    }
                    SolLibraryClassAttribute libraryClass = type.GetCustomAttribute<SolLibraryClassAttribute>();
                    if (libraryClass != null && libraryClass.LibraryName == Name) {
                        string className = type.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? type.Name;
                        SolDebug.WriteLine("Library " + Name + " got: " + type.Name + " => " + className);
                        SolTypeMode classMode = libraryClass.Mode;
                        SolClassBuilder builder = new SolClassBuilder(className, classMode).SetNativeType(type);
                        m_SolToBuilder.Add(className, builder);
                        m_NativeToBuilder.Add(type, builder);
                    }
                    SolGlobalAttribute globalClassAttribute = type.GetCustomAttribute<SolGlobalAttribute>();
                    if (globalClassAttribute != null && globalClassAttribute.Library == Name) {
                        SolDebug.WriteLine("Library " + Name + " scans global class: " + type.Name);
                        foreach (MethodInfo method in type.GetMethods(GLOBAL_BINDING_FLAGS)) {
                            SolGlobalAttribute globalMethodAttribute = method.GetCustomAttribute<SolGlobalAttribute>();
                            if (globalMethodAttribute != null && globalMethodAttribute.Library == Name) {
                                SolDebug.WriteLine("  Found Method " + method.Name);
                                SolFunctionBuilder solMethod;
                                if (TryBuildMethod(method, out solMethod)) {
                                    m_GlobalFunctions.Add(solMethod);
                                }
                            }
                        }
                        foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(type)) {
                            SolGlobalAttribute globalAttribute = field.GetCustomAttribute<SolGlobalAttribute>();
                            if (globalAttribute != null && globalAttribute.Library == Name) {
                                SolDebug.WriteLine("  Found Field " + field.Name);
                                SolFieldBuilder solField;
                                if (TryBuildField(field, out solField)) {
                                    m_GlobalFieldBuilders.Add(solField);
                                }
                            }
                        }
                    }
                }
            }
            // === Create Builder Bodies
            foreach (SolClassBuilder builder in m_SolToBuilder.Values) {
                Type baseType = builder.NativeType.BaseType;
                SolClassBuilder baseTypeBuilder;
                if (baseType != null && m_NativeToBuilder.TryGetValue(baseType, out baseTypeBuilder)) {
                    builder.SetBaseClass(baseTypeBuilder.Name);
                }
                foreach (ConstructorInfo constructor in builder.NativeType.GetConstructors(BINDING_FLAGS)) {
                    SolFunctionBuilder solConstructor;
                    if (TryBuildConstructor(constructor, out solConstructor)) {
                        if (builder.BaseClass != null) {
                            solConstructor.SetMemberModifier(SolMemberModifier.Override);
                        }
                        builder.AddFunction(solConstructor);
                    }
                }
                if (builder.Functions.Count == 0) {
                    throw new InvalidOperationException("The class " + builder.Name + " does not define a constructor. Make sure to expose at least one constructor.");
                }
                foreach (MethodInfo method in builder.NativeType.GetMethods(BINDING_FLAGS)) {
                    if (method.IsSpecialName) {
                        continue;
                    }
                    SolFunctionBuilder solMethod;
                    if (TryBuildMethod(method, out solMethod)) {
                        builder.AddFunction(solMethod);
                    }
                }
                foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(builder.NativeType)) {
                    //string fieldName = field.Name;
                    //if (fieldName == nameof(Attribute.TypeId)) {
                    //    Console.WriteLine("got it.");
                    //}
                    SolFieldBuilder solField;
                    if (field.IsSpecialName) {
                        continue;
                    }
                    if (TryBuildField(field, out solField)) {
                        builder.AddField(solField);
                    }
                }
                // todo: annotations for native types
            }
            HasBeenCreated = true;
        }

        [ContractAnnotation("solField:null => false")]
        private bool TryBuildField(FieldOrPropertyInfo field, [CanBeNull] out SolFieldBuilder solField)
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
            solField = SolFieldBuilder.NewNativeField(name, field)
                .SetAccessModifier(access)
                .SetFieldType(remappedType.HasValue ? SolTypeBuilder.Fixed(remappedType.Value) : SolTypeBuilder.Native(field.DataType));
            // todo: annotations for native fields
            NativeAnnotations(field, solField);
            return true;
        }

        /// <summary>
        ///     Tries to create the function builder for the given method.
        /// </summary>
        /// <param name="method">The method to create the builder for.</param>
        /// <param name="solMethod">The created builder. Only valid if the method returned true.</param>
        /// <returns>true if the builder could be created, false if not.</returns>
        [ContractAnnotation("solMethod:null => false")]
        private bool TryBuildMethod(MethodInfo method, [CanBeNull] out SolFunctionBuilder solMethod)
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
            SolParameterBuilder[] parameters;
            Type[] marshalTypes;
            bool allowOptional;
            bool sendContext;
            InternalHelper.GetParameterBuilders(method.GetParameters(), out parameters, out marshalTypes, out allowOptional, out sendContext);
            // todo: annotations for native functions. if it has a native attribute that is an annotation add it here.
            // Member Mods are set here since need access to the type and the inheritance data.
            SolMemberModifier memberModifier = method.IsAbstract ? SolMemberModifier.Abstract : method.IsOverride() ? SolMemberModifier.Override : SolMemberModifier.Default;
            if (memberModifier == SolMemberModifier.Override) {
                SolClassBuilder definition;
                if (!m_NativeToBuilder.TryGetValue(method.GetBaseDefinition().DeclaringType.NotNull(), out definition)) {
                    // If we got here it means that the NATIVE method is an override. But it is not an override
                    // in SolScript since it overrides a method declared in a class that is not part of SoScriot.
                    memberModifier = SolMemberModifier.Default;
                    SolDebug.WriteLine("Changed member mod of " + method.Name + " from native " + method.DeclaringType + " from override to none.");
                }
            }
            solMethod = SolFunctionBuilder.NewNativeFunction(name, method)
                .SetAccessModifier(access)
                .SetReturnType(remappedReturn.HasValue ? SolTypeBuilder.Fixed(remappedReturn.Value) : SolTypeBuilder.Native(method.ReturnType))
                .SetParameters(parameters)
                .SetNativeMarshalTypes(marshalTypes)
                .SetAllowOptionalParameters(allowOptional)
                .SetNativeSendContext(sendContext)
                .SetMemberModifier(memberModifier);
            NativeAnnotations(method, solMethod);
            return true;
        }

        [ContractAnnotation("solConstructor:null => false")]
        private bool TryBuildConstructor(ConstructorInfo constructor, [CanBeNull] out SolFunctionBuilder solConstructor)
        {
            // todo: ctors are marked as abstract or override later on since we need to know if they have a base class. this should be done here but cant.
            // todo: flesh out ctors as well as functions
            // todo: annotations                    
            SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttributes<SolLibraryVisibilityAttribute>().FirstOrDefault(a => a.LibraryName == Name);
            bool visible = visibility?.Visible ?? constructor.IsPublic;
            if (!visible) {
                solConstructor = null;
                return false;
            }
            SolAccessModifier accessModifier = constructor.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? SolAccessModifier.Internal;
            SolParameterBuilder[] parameters;
            Type[] marshalTypes;
            bool allowOptional;
            bool sendContext;
            InternalHelper.GetParameterBuilders(constructor.GetParameters(), out parameters, out marshalTypes, out allowOptional, out sendContext);
            solConstructor = SolFunctionBuilder.NewNativeConstructor(SolMetaFunction.__new.Name, constructor)
                .SetAccessModifier(accessModifier)
                .SetReturnType(SolTypeBuilder.Fixed(new SolType(SolNil.TYPE, true)))
                .SetParameters(parameters)
                .SetNativeMarshalTypes(marshalTypes)
                .SetAllowOptionalParameters(allowOptional)
                .SetNativeSendContext(sendContext);
            NativeAnnotations(constructor, solConstructor);
            return true;
        }

        private void NativeAnnotations(ICustomAttributeProvider member, IAnnotateableBuilder builder)
        {
            foreach (Attribute attribute in member.GetCustomAttributes(true).Cast<Attribute>()) {
                Type type = attribute.GetType();
                SolClassBuilder annotationBuilder;
                if (!m_NativeToBuilder.TryGetValue(type, out annotationBuilder)) {
                    continue;
                }
                if (annotationBuilder.TypeMode != SolTypeMode.Annotation) {
                    continue;
                }
                builder.AddAnnotation(new SolAnnotationBuilder(SolSourceLocation.Native(), annotationBuilder.Name));
            }
        }*/
    }
}