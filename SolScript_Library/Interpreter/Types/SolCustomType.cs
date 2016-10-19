using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Types {
    public class SolCustomType : SolValue, IValueIndexable {
        internal SolCustomType(string name) {
            m_id = nextId++;
            m_TypeName = name;
        }

        private static readonly SolType toStringType = new SolType("string", false);
        private static readonly SolType isEqualType = new SolType("bool", false);
        private static readonly SolType ctorType = new SolType("any", true);

        private static uint nextId;
        private readonly uint m_id;

        public SolExecutionContext Context; // = new SolExecutionContext(AssemblyL);
        [CanBeNull] private SolFunction m_ctor;
        [CanBeNull] private SolFunction m_metaIsEqual;
        [CanBeNull] private SolFunction m_metaToString;
        private string m_TypeName;

        /// <summary> Is the type has been created from a LibraryClass is has a backing
        ///     ClrObject. The first object in the array indicated the ClrObject of the
        ///     class intself. All other objects indicate the objects of mixins. All, some
        ///     or none elemts may be null if the type was declared in script. </summary>
        public object[] ClrObjects { get; internal set; }

        public override string Type {
            get { return m_TypeName; }
            protected set {
                SolDebug.WriteLine("Warning - Type name of " + m_TypeName + " changed to " + value);
                m_TypeName = value;
            }
        }

        #region IValueIndexable Members

        public SolValue this[SolValue key] {
            get {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolScriptInterpreterException("Tried to get-index a variable in " + Type + " with a " +
                                                            key.Type +
                                                            " value. Classes can only be indexed directly or by strings.");
                }
                SolValue value = Context.VariableContext.GetValue_X(keySolStr.Value);
                if (value == null) {
                    throw new SolScriptInterpreterException("Tried to get the non-assinged variable " + keySolStr.Value +
                                                            " in " + Type +
                                                            ".");
                }
                return value;
            }
            set {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolScriptInterpreterException("Tried to set-index a variable in " + Type + " with a " +
                                                            key.Type +
                                                            " value. Classes can only be indexed directly or by strings.");
                }
                Context.VariableContext.AssignValue(keySolStr.Value, value);
            }
        }

        #endregion

        /*internal static SolCustomType FromName(string name) {
            SolCustomType type = new SolCustomType {m_TypeName = name};
            return type;
        }

        internal static SolCustomType FromNameAndClr(string name, object[] clrObjects) {
            SolCustomType type = new SolCustomType {
                m_TypeName = name,
                ClrObjects = clrObjects
            };
            return type;
        }*/

        [CanBeNull]
        public override object ConvertTo(Type type) {
            return null;
        }

        public void CallCtor(params SolValue[] args) {
            m_ctor?.Call(args, Context);
        }

        [CanBeNull]
        private static SolFunction GetFunctionAndAssertType(VarContext context, string typeName, string name,
            SolType type) {
            SolFunction function = context.GetValue_X(name) as SolFunction;
            //SolDebug.WriteLine("get " + typeName + "."+name + " -> " + function);
            if (function != null && !type.IsCompatible(function.Return)) {
                throw SolScriptInterpreterException.InvalidTypes(function.Location, type.ToString(),
                    function.Return.ToString(), name + " function in " + typeName + " must return a " + type);
            }
            return function;
        }

        public void RebuildMetaFunctions() {
            VarContext varContext = Context.VariableContext;
            m_ctor = GetFunctionAndAssertType(varContext, m_TypeName, "__new", ctorType);
            m_metaToString = GetFunctionAndAssertType(varContext, m_TypeName, "__to_string", toStringType);
            m_metaIsEqual = GetFunctionAndAssertType(varContext, m_TypeName, "__is_equal", isEqualType);
        }

        protected override string ToString_Impl() {
            if (m_metaToString != null) {
                SolString value = (SolString) m_metaToString.Call(EmptyArray, Context);
                return value.Value;
            }
            return "Object#" + m_id + "<" + Type + ">";
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 20 + (int) m_id + Type.GetHashCode();
            }
        }

        public override bool IsEqual(SolValue other) {
            if (m_metaIsEqual != null) {
                SolBoolean value = (SolBoolean) m_metaIsEqual.Call(new[] {other}, Context);
                return value.Value;
            }
            SolCustomType otherType = other as SolCustomType;
            return otherType != null && m_id == otherType.m_id;
        }
    }
}