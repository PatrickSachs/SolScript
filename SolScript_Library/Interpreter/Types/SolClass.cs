using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Types {
    public class SolClass : SolValue, IValueIndexable {
        internal SolClass(SolClassDefinition definition) {
            Id = s_NextId++;
            m_Definition = definition;
            InheritanceChain = new Inheritance(this, definition, null);
            GlobalVariables = new ClassInheritanceVariables(this, InheritanceChain);
            InternalVariables = new ClassInheritanceVariables(this, InheritanceChain) {Parent = GlobalVariables};
            ClassMeta = new ClassMetaContainer();
            if (TypeMode == SolTypeMode.Annotation) {
                AnnotationMeta = new AnnotationMetaContainer();
            }
        }

        

        private static uint s_NextId;
        internal readonly AnnotationMetaContainer AnnotationMeta;
        internal readonly ClassMetaContainer ClassMeta;

        public readonly ClassInheritanceVariables GlobalVariables;
        public readonly ClassInheritanceVariables InternalVariables;
        public readonly uint Id;
        private readonly SolClassDefinition m_Definition;
        //public SolClass[] Annotations;
        public readonly Inheritance InheritanceChain;
        [CanBeNull] private SolFunction m_Ctor;
        private int m_MixinPtr;
        private Mixin[] m_Mixins;
        public SolAssembly Assembly => m_Definition.Assembly;
        public SolTypeMode TypeMode => m_Definition.TypeMode;
        public override bool IsClass => true;
        public override string Type => m_Definition.Type;

        /// <summary> Is this class initialized? A class counts as initialized as soon as
        ///     the constructor is called. </summary>
        public bool IsInitialized { get; internal set; }

        #region IValueIndexable Members

        public SolValue this[SolValue key] {
            get {
                // issue: class context exception
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolScriptInterpreterException(null,
                        "Tried to get-index a variable in " + Type + " with a " +
                        key.Type +
                        " value. Classes can only be indexed directly or by strings.");
                }
                //SolValue value = Context.VariableContext.GetValue(Context, keySolStr.Value);
                SolValue value = GlobalVariables.Get(keySolStr.Value);
                if (value == null) {
                    throw new SolScriptInterpreterException(null,
                        "Tried to get the non-assinged variable " + keySolStr.Value +
                        " in " + Type +
                        ".");
                }
                return value;
            }
            set {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolScriptInterpreterException(null,
                        "Tried to set-index a variable in " + Type + " with a " +
                        key.Type +
                        " value. Classes can only be indexed directly or by strings.");
                }
                //Context.VariableContext.AssignValue(Context, keySolStr.Value, value);
                GlobalVariables.Assign(keySolStr.Value, value);
            }
        }

        #endregion

        #region Overrides

        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolClass)) return this;
            for (int i = 0; i < m_Mixins.Length; i++) {
                object clrObj = m_Mixins[i].Native;
                if (clrObj == null) {
                    continue;
                }
                if (clrObj.GetType() == type || clrObj.GetType().IsSubclassOf(type)) {
                    return clrObj;
                }
            }
            return null;
        }

        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            if (context != null && ClassMeta.MetaToString != null) {
                SolString value = (SolString) ClassMeta.MetaToString.Call(context, this, EmptyArray);
                return value.Value;
            }
            return "class#" + Id + "<" + Type + ">";
        }

        public override SolValue GetN(SolExecutionContext context) {
            if (ClassMeta.MetaGetN != null) {
                return ClassMeta.MetaGetN.Call(context, this, EmptyArray);
            }
            return base.GetN(context);
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 20 + (int) Id + Type.GetHashCode();
            }
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaIsEqual != null) {
                SolBool value = (SolBool) ClassMeta.MetaIsEqual.Call(context, this, other);
                return value.Value;
            }
            SolClass otherType = other as SolClass;
            return otherType != null && Id == otherType.Id;
        }

        public override SolValue Concatenate(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaConcat != null) {
                return ClassMeta.MetaConcat.Call(context, this, other);
            }
            return base.Concatenate(context, other);
        }


        public override SolValue Add(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaAdd != null) {
                return ClassMeta.MetaAdd.Call(context, this, other);
            }
            return base.Add(context, other);
        }

        public override SolValue Subtract(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaSubtract != null) {
                return ClassMeta.MetaSubtract.Call(context, this, other);
            }
            return base.Subtract(context, other);
        }

        public override SolValue Multiply(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaMultiply != null) {
                return ClassMeta.MetaMultiply.Call(context, this, other);
            }
            return base.Multiply(context, other);
        }

        public override SolValue Divide(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaDivide != null) {
                return ClassMeta.MetaDivide.Call(context, this, other);
            }
            return base.Divide(context, other);
        }

        public override SolValue Exponentiate(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaExponentiate != null) {
                return ClassMeta.MetaExponentiate.Call(context, this, other);
            }
            return base.Exponentiate(context, other);
        }

        public override SolValue Modulu(SolExecutionContext context, SolValue other) {
            if (ClassMeta.MetaModulu != null) {
                return ClassMeta.MetaModulu.Call(context, this, other);
            }
            return base.Modulu(context, other);
        }

        public override IEnumerable<SolValue> Iterate(SolExecutionContext context) {
            // todo: iterator expressions/type ?
            if (ClassMeta.MetaIterate != null) {
                return ClassMeta.MetaIterate.Call(context, this).Iterate(context);
            }
            return base.Iterate(context);
        }

        #endregion

        /// <summary> Finds the inheritance link that contains the given class definition. </summary>
        /// <param name="definition"> The class definition. </param>
        /// <returns> The inheritance link, or null. </returns>
        [CanBeNull]
        public Inheritance FindInheritance(SolClassDefinition definition) {
            Inheritance active = InheritanceChain;
            while (active != null) {
                if (active.Definition == definition)
                    return active;
                active = active.BaseClass;
            }
            return null;
        }

        public MixinId CreateMixin(string type) {
            if (m_MixinPtr >= m_Mixins.Length) {
                var newArray = new Mixin[m_Mixins.Length*2];
                Array.Copy(m_Mixins, newArray, m_Mixins.Length);
                m_Mixins = newArray;
            }
            int mixinId = m_MixinPtr++;
            m_Mixins[mixinId] = new Mixin {Type = type};
            return new MixinId(this, mixinId);
        }

        public void AssignMixin(MixinId mixin, object native) {
            if (mixin.Instance != this) {
                throw new ArgumentException("The instance of this mixin points to anther class!", nameof(mixin));
            }
            m_Mixins[mixin.Index].Native = native;
        }

        /// <summary> Calls the constructor for this class. </summary>
        public void CallCtor(SolExecutionContext callingContext, params SolValue[] args) {
            if (IsInitialized) {
                throw new SolScriptInterpreterException(null, "Tried to call a constructor in type " + Type +
                                                              " multiple times.");
            }
            IsInitialized = true;
            // todo: annotations
            // __a_pre_new
            /*foreach (SolClass annotation in Annotations) {
                SolTable metaTable = annotation.AnnotationMeta.MetaPreCtor?.Call(callingContext, this, args) as SolTable;
                if (metaTable == null) {
                    continue;
                }
                // new_args
                SolValue newArgs = metaTable["new_args"];
                if (!newArgs.IsEqual(callingContext, SolNil.Instance)) {
                    SolTable newArgsTable = newArgs as SolTable;
                    if (newArgsTable == null) {
                        throw new SolScriptInterpreterException(callingContext,
                            $"\"new_args\" provided by the annotation \"{annotation.Type}\" on class \"{Type}\" was a \"{newArgs.Type}\", however a \"table!\" was expected.");
                    }
                    SolDebug.WriteLine("pre-pre: " + InternalHelper.JoinToString(",", args));
                    args = newArgsTable.ToArray();
                    SolDebug.WriteLine("post-pre: " + InternalHelper.JoinToString(",", args));
                }
            }*/
            if (m_Ctor != null) {
                callingContext.StackTrace.Push(new SolExecutionContext.StackFrame(m_Ctor.Location, Type + ".__new", m_Ctor));
                m_Ctor.Call(callingContext, this, args);
                callingContext.StackTrace.Pop();
            }
            // __a_post_new
            /*foreach (SolClass annotation in Annotations) {
                annotation.AnnotationMeta.MetaPostCtor?.Call(callingContext, this, args);
            }*/
        }

        [CanBeNull]
        private static SolFunction GetFunctionAndAssertType(SolClass @class, string name, SolType type) {
            SolValue funcRaw;
            // issue: private funcs don't work, internal only due to bug
            if (@class.GlobalVariables.TryGet(name, out funcRaw) == VariableGet.Success)
            {
                SolFunction function = funcRaw as SolFunction;
                if (function != null && !type.IsCompatible(@class.Assembly, function.Return))
                {
                    throw SolScriptInterpreterException.InvalidTypes(null, type.ToString(),
                        function.Return.ToString(), name + " function in " + @class.Type + " must return a " + type);
                }
                return function;
            }
            return null;
        }

        /// <summary> Rebuilds all meta functions from the variable context of this class.
        ///     Calling this method multiple times not does carry any side-effects but is
        ///     typically unnecessary. Needs to be called once manually. </summary>
        public void RebuildMetaFunctions() {
            m_Ctor = GetFunctionAndAssertType(this, "__new", new SolType("any", true));
            // issue: the return type needs to be true as a CLR-string can be null. (special handling for ToString()?)
            ClassMeta.MetaToString = GetFunctionAndAssertType(this, "__to_string", new SolType("string", true));
            ClassMeta.MetaIsEqual = GetFunctionAndAssertType(this, "__is_equal", new SolType("bool", false));
            ClassMeta.MetaIterate = GetFunctionAndAssertType(this, "__iterate", new SolType("table", false));
            ClassMeta.MetaModulu = GetFunctionAndAssertType(this, "__modulu", new SolType("any", true));
            ClassMeta.MetaExponentiate = GetFunctionAndAssertType(this, "__exp", new SolType("any", true));
            ClassMeta.MetaDivide = GetFunctionAndAssertType(this, "__divide", new SolType("any", true));
            ClassMeta.MetaAdd = GetFunctionAndAssertType(this, "__add", new SolType("any", true));
            ClassMeta.MetaSubtract = GetFunctionAndAssertType(this, "__subtract", new SolType("any", true));
            ClassMeta.MetaMultiply = GetFunctionAndAssertType(this, "__multiply", new SolType("any", true));
            ClassMeta.MetaConcat = GetFunctionAndAssertType(this, "__concat", new SolType("any", true));
            ClassMeta.MetaGetN = GetFunctionAndAssertType(this, "__get_n", new SolType("any", true));
            if (TypeMode == SolTypeMode.Annotation) {
                AnnotationMeta.MetaPreCtor = GetFunctionAndAssertType(this, "__a_pre_new", new SolType("table", true));
                AnnotationMeta.MetaPostCtor = GetFunctionAndAssertType(this, "__a_post_new", new SolType("table", true));
                AnnotationMeta.MetaGetVar = GetFunctionAndAssertType(this, "__a_get_var", new SolType("table", true));
                AnnotationMeta.MetaSetVar = GetFunctionAndAssertType(this, "__a_set_var", new SolType("table", true));
                AnnotationMeta.MetaDeclareField = GetFunctionAndAssertType(this, "__a_declare_field", new SolType("table", true));
            }
        }

        #region Nested type: AnnotationMetaContainer

        internal class AnnotationMetaContainer {
            [CanBeNull] public SolFunction MetaDeclareField;
            [CanBeNull] public SolFunction MetaGetVar;
            [CanBeNull] public SolFunction MetaPostCtor;
            [CanBeNull] public SolFunction MetaPreCtor;
            [CanBeNull] public SolFunction MetaSetVar;
        }

        #endregion

        #region Nested type: ClassMetaContainer

        internal class ClassMetaContainer {
            [CanBeNull] public SolFunction MetaAdd;

            [CanBeNull] public SolFunction MetaConcat;

            [CanBeNull] public SolFunction MetaDivide;

            [CanBeNull] public SolFunction MetaExponentiate;

            [CanBeNull] public SolFunction MetaGetN;

            [CanBeNull] public SolFunction MetaIsEqual;

            [CanBeNull] public SolFunction MetaIterate;

            [CanBeNull] public SolFunction MetaModulu;

            [CanBeNull] public SolFunction MetaMultiply;

            [CanBeNull] public SolFunction MetaSubtract;

            [CanBeNull] public SolFunction MetaToString;
        }

        #endregion

        #region Nested type: Inheritance

        public class Inheritance {
            public Inheritance(SolClass classInstance, SolClassDefinition definition, [CanBeNull] Inheritance baseClass) {
                BaseClass = baseClass;
                Definition = definition;
                Variables = new ClassInheritanceVariables(classInstance, this);
            }

            public readonly SolClassDefinition Definition;
            /* Each inheritance link has their own variables representing the 
             * non-global variables. These variables have the class global variables 
             * as parent, thus accessing globals and internals once no local exists. 
             * Furthermore new variables will be declared in the local scope. */

            /// <summary> The local variables of this inheritance level. Uses the class global
            ///     variables as parent. </summary>
            public readonly ClassInheritanceVariables Variables;

            [CanBeNull] public Inheritance BaseClass;

            [CanBeNull] public object NativeObject;
        }

        #endregion

        #region Nested type: Initializer

        /// <summary> The initializer class is used to initialize SolClasses. </summary>
        public sealed class Initializer {
            public Initializer(SolClass forClass) {
                m_ForClass = forClass;
            }

            private readonly SolClass m_ForClass;

            //private HashSet<string> m_Public = new HashSet<string>();

            /// <summary> Creates the instance of this SolClass by assigning the default (or
            ///     specified) values to the fields and then calls the constructor. </summary>
            public SolClass Create(SolExecutionContext context, params SolValue[] args) {
                Inheritance currentInheritance = m_ForClass.InheritanceChain;
                // issue: properly init everything 
                ClassInheritanceVariables variables = m_ForClass.InheritanceChain.Variables;
                SolClassDefinition definition = m_ForClass.m_Definition;
                foreach (string fieldName in definition.FieldNames) {
                    SolExpression initializer = definition.GetFieldInitializer(fieldName);
                    if (initializer != null) {
                        SolValue initialValue = initializer.Evaluate(context, variables);
                        SolDebug.WriteLine("Initializing field " + fieldName + " to " + initialValue);
                        variables.Assign(fieldName, initialValue);
                    } else {
                        // todo: default values or error for fields without initializer.
                        if (!definition.GetField(fieldName).Type.CanBeNil) {
                        }
                    }
                }
                m_ForClass.RebuildMetaFunctions();
                m_ForClass.CallCtor(context, args);
                return m_ForClass;
            }

            /// <summary> Warning: NO finds of this class have been assigned and the ctor will
            ///     NOT be called. The IsInitialized variable of the class is set to FALSE. You
            ///     are responsible for making sure that this class can be used.
            ///     <br/>
            ///     The interpreter internally uses this method to marshal already existing
            ///     native objects to new SolClasses. </summary>
            [NotNull]
            public SolClass CreateWithoutInitialization() {
                m_ForClass.RebuildMetaFunctions();
                return m_ForClass;
            }
        }

        #endregion

        #region Nested type: Mixin

        private class Mixin {
            public object Native;
            public string Type;
        }

        #endregion
    }
}