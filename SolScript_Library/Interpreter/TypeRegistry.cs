using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter {
    public class TypeRegistry {
        public TypeRegistry(SolAssembly linkedAssembly) {
            LinkedAssembly = linkedAssembly;
        }

        public readonly SolAssembly LinkedAssembly;

        private readonly Dictionary<string, SolClassBuilder> m_Builders = new Dictionary<string, SolClassBuilder>();
        private readonly Dictionary<string, SolClassDefinition> m_ClassDefinitions = new Dictionary<string, SolClassDefinition>();
        private readonly Dictionary<Type, SolClassDefinition> m_NativeClasses = new Dictionary<Type, SolClassDefinition>();

        private State m_State;

        public IReadOnlyCollection<SolClassDefinition> ClassDefinitions {
            get {
                AssetStateExactAndHigher(State.Generated, "Cannot receive class definitions if they aren't generated yet.");
                return m_ClassDefinitions.Values;
            }
        }

        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(Type nativeType, [CanBeNull] out SolClassDefinition definition) {
            return m_NativeClasses.TryGetValue(nativeType, out definition);
        }

        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(string typeName, [CanBeNull] out SolClassDefinition definition) {
            return m_ClassDefinitions.TryGetValue(typeName, out definition);
        }

        private bool AssetStateExactAndHigher(State state, string message = "Invalid State!") {
            if ((int) m_State < (int) state) {
                throw new InvalidOperationException($"{message} - In State: {m_State}({(int) m_State}); Required State: {state}({(int) state}) or higher.");
            }
            return true;
        }

        private bool AssetStateExactAndLower(State state, string message = "Invalid State!") {
            if ((int) m_State > (int) state) {
                throw new InvalidOperationException($"{message} - In State: {m_State}({(int) m_State}); Required State: {state}({(int) state}) or lower.");
            }
            return true;
        }

        private bool AssetStateExact(State state, string message = "Invalid State!") {
            if (m_State != state) {
                throw new InvalidOperationException($"{message} - In State: {m_State}({(int) m_State}); Required State: {state}({(int) state}) exactly.");
            }
            return true;
        }

        public void RegisterClass(SolClassBuilder builder) {
            AssetStateExact(State.Registry, "Can only register classes during registry state. New classes cannot be registered once the definitions have been generated.");
            m_Builders.Add(builder.Name, builder);
        }

        public void RegisterClasses(IEnumerable<SolClassBuilder> builders) {
            foreach (SolClassBuilder builder in builders) {
                RegisterClass(builder);
            }
        }

        public void CreateClassDefinitions() {
            AssetStateExact(State.Registry, "Generating class definitions will advance the state to Generated, and thus can only be called in Registry state.");
            m_State = State.Generated;
            foreach (SolClassBuilder builder in m_Builders.Values) {
                SolClassDefinition def = new SolClassDefinition(LinkedAssembly, builder.Name, builder.TypeMode);
                if (builder.NativeType != null) {
                    def.NativeType = builder.NativeType;
                    m_NativeClasses.Add(builder.NativeType, def);
                }
                foreach (SolFieldBuilder fieldBuilder in builder.Fields) {
                    SolFieldDefinition fieldDefinition = new SolFieldDefinition(def) {
                        Type = fieldBuilder.Type,
                        Modifiers = fieldBuilder.Modifiers,
                        FieldInitializer = fieldBuilder.ScriptField,
                        NativeBackingField = fieldBuilder.NativeField
                    };
                    def.SetField(fieldBuilder.Name, fieldDefinition);
                }
                foreach (SolFunctionBuilder functionBuilder in builder.Functions) {
                    SolFunctionDefinition functionDefinition = new SolFunctionDefinition(def) {
                        Modifiers = functionBuilder.Modifiers,
                        Creator1 = functionBuilder.ScriptFunction,
                        Creator2 = functionBuilder.NativeMethod,
                        Creator3 = functionBuilder.NativeConstructor
                    };
                    def.SetFunction(functionBuilder.Name, functionDefinition);
                }
                m_ClassDefinitions.Add(builder.Name, def);
            }
            foreach (SolClassBuilder builder in m_Builders.Values) {
                if (builder.BaseClass == null) continue;
                SolClassDefinition def = m_ClassDefinitions[builder.Name];
                SolClassDefinition baseDef = m_ClassDefinitions[builder.BaseClass];
                def.BaseClass = baseDef;
            }
        }

        private SolClass CreateInstance_Impl(SolClassDefinition definition) {
            AssetStateExactAndHigher(State.Generated, "Class instances can only be created once the class definitions have been generated.");
            SolClass instance = new SolClass(definition);
            {
                // Build inheritance tree and declare fields
                IVariables globalVariables = instance.GlobalVariables;
                SolClassDefinition activeDefinition = definition;
                SolClass.Inheritance activeInheritance = null;
                while (activeDefinition != null) {
                    SolClass.Inheritance newInheritance = new SolClass.Inheritance(instance, activeDefinition, null);
                    if (activeInheritance != null) activeInheritance.BaseClass = newInheritance;
                    activeInheritance = newInheritance;
                    IVariables activeVariables = activeInheritance.Variables;
                    foreach (string fieldName in activeDefinition.FieldNames) {
                        SolFieldDefinition field = activeDefinition.GetField(fieldName);
                        if (!InternalHelper.IsLocal(field.Modifiers)) {
                            if (InternalHelper.IsInternal(field.Modifiers)) {
                                // internal
                                // issue: internal visible outside of class!
                                // issue: this means that the entire variable system is still fucking flawed
                                globalVariables.Declare(fieldName, field.Type, true);
                            } else {
                                // global (= not local & not internal)
                                globalVariables.Declare(fieldName, field.Type, false);
                            }
                        } else {
                            // todo: should inheritance tree locals be local?
                            activeVariables.Declare(fieldName, field.Type, false);
                        }
                    }
                    activeDefinition = activeDefinition.BaseClass;
                }
                instance.InheritanceChain = activeInheritance;
                globalVariables.Declare("self", new SolType(instance.Type), true);
            }
            return instance;
        }

        /// <summary> Creates a new class instance. Please refer to the parameter
        ///     documentation for more detail. </summary>
        /// <param name="name"> The name of the class </param>
        /// <param name="enforce"> Settings this to true will enforce instance creation
        ///     even if no instance of this class can be created(e.g. annotations) </param>
        /// <param name="args"> The arguments </param>
        /// <returns> Type created SolClass </returns>
        /// <exception cref="SolScriptInterpreterException"> No class with
        ///     <paramref name="name"/> exists. </exception>
        /// <exception cref="SolScriptInterpreterException"> The class has invalid mixins. </exception>
        /// <exception cref="SolScriptInterpreterException"> The class cannot be created. </exception>
        public SolClass.Initializer CreateInstance(string name, bool enforce = false, params SolValue[] args) {
            SolClassDefinition definition;
            if (!m_ClassDefinitions.TryGetValue(name, out definition)) {
                // todo: more exceptions types!
                throw new NotImplementedException(name + " - No class with this name exists.");
            }
            return CreateInstance(definition, enforce, args);
        }

        /// <summary> Creates a class instance from a class definition. </summary>
        public SolClass.Initializer CreateInstance(SolClassDefinition definition, bool enforce = false, params SolValue[] args) {
            if (!enforce && !definition.CanBeCreated()) {
                // todo: more exceptions types!
                throw new NotImplementedException(definition.Type + " - This class cannot be instantiated.");
            }
            SolClass instance = CreateInstance_Impl(definition);
            return new SolClass.Initializer(instance);
        }

        #region Nested type: State

        private enum State {
            Registry = 0,
            Generated = 1
        }

        #endregion
    }

    #region Nested type: SolClassDefinition

    public class SolClassDefinition {
        public SolClassDefinition(SolAssembly assembly, string type, SolTypeMode typeMode) {
            Assembly = assembly;
            Type = type;
            TypeMode = typeMode;
        }

        public readonly SolAssembly Assembly;
        private readonly Dictionary<string, SolFieldDefinition> m_Fields = new Dictionary<string, SolFieldDefinition>();
        private readonly Dictionary<string, SolFunctionDefinition> m_Functions = new Dictionary<string, SolFunctionDefinition>();
        public readonly string Type;
        public readonly SolTypeMode TypeMode;
        public SolClassDefinition BaseClass;
        
        public Type NativeType;

        public IReadOnlyCollection<SolFieldDefinition> Fields => m_Fields.Values;
        public IReadOnlyCollection<SolFunctionDefinition> Functions => m_Functions.Values;
        public IReadOnlyCollection<string> FieldNames => m_Fields.Keys;
        public IReadOnlyCollection<string> FunctionNames => m_Functions.Keys;

        #region Overrides

        public override string ToString() {
            return $"SolClassDefinition(Type={Type}, Fields={m_Fields.Count}, Functions={m_Functions.Count})";
        }

        #endregion

        /// <summary> Checks if the class does extend the given class at some point. </summary>
        /// <remarks> Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed. </remarks>
        public bool DoesExtendInHierarchy(string className) {
            SolClassDefinition definition = BaseClass;
            while (definition != null) {
                if (definition.Type == className) return true;
                definition = definition.BaseClass;
            }
            return false;
        }

        /// <summary> Can an instance of this class be created by the user? Only
        ///     <see cref="SolTypeMode.Default"/> and <see cref="SolTypeMode.Sealed"/>
        ///     classes can be created using the new keyword. </summary>
        public bool CanBeCreated() {
            switch (TypeMode) {
                case SolTypeMode.Default:
                case SolTypeMode.Sealed:
                    return true;
                case SolTypeMode.Singleton:
                case SolTypeMode.Annotation:
                case SolTypeMode.Abstract:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool HasFunction(string name) {
            return m_Functions.ContainsKey(name);
        }

        public bool HasField(string name) {
            return m_Fields.ContainsKey(name);
        }

        /// <summary> Gets a function definition in this class definition. </summary>
        /// <param name="name"> The name of the function. </param>
        /// <param name="declaredOnly"> If this parameter is true only functions declared
        ///     directly in this class will be searched. </param>
        /// <param name="definition"> The returned function. - Only valid if the method
        ///     returned true. </param>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetFunction(string name, bool declaredOnly, [CanBeNull] out SolFunctionDefinition definition) {
            SolClassDefinition activeClassDefinition = this;
            while (activeClassDefinition != null) {
                if (activeClassDefinition.m_Functions.TryGetValue(name, out definition)) return true;
                activeClassDefinition = declaredOnly ? null : activeClassDefinition.BaseClass;
            }
            definition = null;
            return false;
        }

        /// <summary> Gets a field definition in this class definition. </summary>
        /// <param name="name"> The name of the field. </param>
        /// <param name="declaredOnly"> If this parameter is true only field declared
        ///     directly in this class will be searched. </param>
        /// <param name="definition"> The returned field. - Only valid if the method
        ///     returned true. </param>
        public bool GetField(string name, bool declaredOnly, [CanBeNull] out SolFieldDefinition definition) {
            SolClassDefinition activeClassDefinition = this;
            while (activeClassDefinition != null) {
                if (activeClassDefinition.m_Fields.TryGetValue(name, out definition)) return true;
                activeClassDefinition = declaredOnly ? null : activeClassDefinition.BaseClass;
            }
            definition = null;
            return false;
        }

        [CanBeNull]
        public SolExpression GetFieldInitializer(string name) {
            return m_Fields[name].FieldInitializer;
        }

        public bool SetFieldIfNotExists(string name, SolFieldDefinition field) {
            if (m_Fields.ContainsKey(name)) return false;
            SetField(name, field);
            return true;
        }

        public bool SetFunctionIfNotExists(string name, SolFunctionDefinition function) {
            if (m_Functions.ContainsKey(name)) return false;
            SetFunction(name, function);
            return true;
        }

        public void SetField(string name, SolFieldDefinition field) {
            m_Fields[name] = field;
        }

        public void SetFunction(string name, SolFunctionDefinition function) {
            m_Functions[name] = function;
        }
    }

    #endregion

    #region Nested type: SolFieldDefinition

    public class SolFieldDefinition {
        public SolFieldDefinition(SolClassDefinition definedIn) {
            DefinedIn = definedIn;
        }

        public SolClassDefinition DefinedIn;
        public SolExpression FieldInitializer;
        public AccessModifiers Modifiers;
        public InspectorField NativeBackingField;
        public SolType Type;
    }

    #endregion

    #region Nested type: SolFunctionDefinition

    public class SolFunctionDefinition {
        public SolFunctionDefinition(SolClassDefinition definedIn) {
            DefinedIn = definedIn;
        }

        public readonly SolClassDefinition DefinedIn;

        public SolFunction Creator1;
        public MethodInfo Creator2;
        public ConstructorInfo Creator3;
        private SolFunction m_Impl;
        public AccessModifiers Modifiers;

        /// <summary> Returns the actual SolFunction which can be invoked on classes. </summary>
        public SolFunction GetImplementation() {
            if (m_Impl == null) {
                if (Creator1 != null) {
                    // issue: function creator should probably not be an expression for class functions. lamdas are another story.
                    m_Impl = Creator1;
                } else if (Creator2 != null) {
                    m_Impl = SolCSharpClassFunction.CreateFrom(DefinedIn, this);
                } else if (Creator3 != null) {
                    m_Impl = SolCSharpConstructorFunction.CreateFrom(DefinedIn, this);
                } else {
                    throw new InvalidOperationException("The function does not provide any creators.");
                }
            }
            return m_Impl;
        }
    }

    #endregion
}