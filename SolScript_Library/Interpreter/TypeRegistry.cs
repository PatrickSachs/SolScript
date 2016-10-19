using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter {
    /// <summary> A low-level type definition for SolScript. Refer to SolFunction and
    ///     derived classes if you want to create functions at runtime. </summary>
    public class TypeDef {
        public enum TypeMode {
             Default,Singleton//,Global
        }

        public AnnotDef[] Annotations;

        /// <summary> If the type is made from a csharp type this value is set. It is used
        ///     to create a backing object instance for every script instance of the given
        ///     type. </summary>
        [CanBeNull] public Type ClrType;

        /// <summary> An array of the fields in this class. </summary>
        public FieldDef[] Fields;

        /// <summary> An array of the functions in this class. </summary>
        public FuncDef[] Functions;

        /// <summary> All mixed in classes. Mixed in classes will copy their members into
        ///     this class. </summary>
        public string[] Mixins;

        /// <summary> The name of the class. Used for the singleton and for creating the
        ///     class. </summary>
        public string Name;

        /// <summary> Singleton types will create an instance named after their class name. </summary>
        public TypeMode Mode;

        #region Nested type: AnnotDef

        public class AnnotDef {
            public SolExpression[] Arguments;
            public string Name;
        }

        #endregion

        #region Nested type: FieldDef

        public class FieldDef {
            public AnnotDef[] Annotations;
            public SolExpression Creator1;
            public InspectorField Creator2;
            public bool Local;
            public string Name;
            public SolType Type;
        }

        #endregion

        #region Nested type: FuncDef

        public class FuncDef {
            public AnnotDef[] Annotations;

            /// <summary> The creator for script functions </summary>
            [CanBeNull] public SolExpression Creator1;

            /// <summary> The creator for csharp functions. </summary>
            [CanBeNull] public MethodInfo Creator2;

            public bool Local;
            public string Name;
        }

        #endregion
    }

    public class TypeRegistry {
        public readonly Dictionary<string, TypeDef> Types = new Dictionary<string, TypeDef>();

        /// <summary> Inserts all members of a given type defition into a given variable
        ///     context. This is used to create mixins. </summary>
        /// <param name="type"> The type to mixin </param>
        /// <param name="target"> The variable contex to mix into </param>
        /// <param name="context"> The execution context used to evaluate potential
        ///     expressions </param>
        /// <param name="newName"> The name of the __new function (ctor). Sometimes this
        ///     function might have to be renamed(e.g. in order to still be able to access
        ///     the ctor of mixins in a parent class) </param>
        public void InsertMembers(TypeDef type, VarContext target, SolExecutionContext context, string newName, SolCustomType typeInstance, int mixinId) {
            DynamicReference mixinRef = new DynamicReference.CustomTypeMixinClr(typeInstance, mixinId);
            foreach (TypeDef.FieldDef field in type.Fields) {
                if (field.Creator1 != null)
                {
                    target.DeclareVariable(field.Name, field.Creator1.Evaluate(context), field.Type, field.Local);
                } else {
                    target.DeclareVariable(field.Name, field.Creator2, mixinRef, field.Type, field.Local);
                }
            }
            foreach (TypeDef.FuncDef function in type.Functions) {
                SolExpression creator1 = function.Creator1;
                SolValue functionInstance;
                if (creator1 != null) {
                    functionInstance = creator1.Evaluate(context);
                } else {
                    functionInstance = SolCSharpFunction.CreateFrom(function.Creator2.NotNull(), mixinRef);
                }
                target.SetValue(function.Name == "__new" ? newName : function.Name, functionInstance,
                    new SolType("function", false), function.Local);
            }
            if (type.ClrType != null) {
                ConstructorInfo constructor = type.ClrType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                             BindingFlags.Instance).FirstOrDefault();
                if (constructor != null) {
                    SolCSharpConstructorFunction ctorFunc = new SolCSharpConstructorFunction(constructor, typeInstance, mixinId);
                    target.SetValue(newName, ctorFunc, new SolType("function", false), true);
                }
            }
        }

        public SolCustomType CreateInstance(SolAssembly assembly, string name, SolValue[] args) {
            TypeDef typeDef;
            if (!Types.TryGetValue(name, out typeDef)) {
                throw new SolScriptInterpreterException("Tried to create an instance of type " + name +
                                                        ", but no type with this name exists.");
            }
            SolCustomType customType = new SolCustomType(typeDef.Name) {
                Context = new SolExecutionContext(assembly),
                ClrObjects = new object[1 + typeDef.Mixins.Length]
            };
            VarContext variables = customType.Context.VariableContext;
            variables.ParentContext = assembly.RootContext.VariableContext;
            for (int i = 0; i < typeDef.Mixins.Length; i++) {
                string mixin = typeDef.Mixins[i];
                TypeDef mixinDef;
                if (!Types.TryGetValue(mixin, out mixinDef)) {
                    throw new SolScriptInterpreterException("The mixin " + mixin + " for class " + name +
                                                            " does not exist.");
                }
                InsertMembers(mixinDef, variables, customType.Context, "__new_" + mixin, customType, i+1);
            }
            InsertMembers(typeDef, variables, customType.Context, "__new", customType, 0);
            customType.RebuildMetaFunctions();
            customType.CallCtor(args);
            return customType;
        }
    }
}