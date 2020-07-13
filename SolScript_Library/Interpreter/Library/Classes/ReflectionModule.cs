using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Classes {
    // ReSharper disable InconsistentNaming
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Reflect")]
    public class ReflectionModule {
        [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
        [SolLibraryName("FunctionInfo")]
        public class FunctionInfo {
            public FunctionInfo(string name, string type) {
                Name = name;
                Type = type;
            }

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Name;

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Type;

            public SolString get_name() {
                return new SolString(Name);
            }

            public SolString get_compatible_class() {
                return new SolString(Type);
            }

            public override string ToString() {
                return "FunctionInfo(class=" + Type + ", name=" + Name + ")";
            }

            public SolValue call(SolExecutionContext context, SolClass instance, params SolValue[] args) {   
                // todo: reflection module
                     throw new NotImplementedException();    
                /*SolFunction value = instance.Context.VariableContext.GetValue(context, Name) as SolFunction;
                if (value == null) {
                    // This error should hopefully never occur as the FunctionInfo is checked when generating.
                    throw new SolScriptInterpreterException(context,
                        "Cannot call function \"" + Name + "\" on instance of class \"" + instance.Type +
                        "\": The class does not have such a function.");
                }
                return value.Call(context, args);*/
            }
        }

        public override string ToString() {
            return "Reflection Module";
        }

        [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
        [SolLibraryName("FieldInfo")]
        public class FieldInfo {
            public FieldInfo(string name, string type) {
                Name = name;
                Type = type;
            }

            public override string ToString() {
                return "FieldInfo(class=" + Type + ", name=" + Name + ")";
            }

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Name;
            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Type;

            public SolString get_name() {
                return new SolString(Name);
            }

            public SolString get_compatible_class() {
                return new SolString(Type);
            }

            public SolValue get(SolExecutionContext context, SolClass instance) {
                // todo: local not supported, internal only works due to another bug.
                SolValue value = instance.GlobalVariables.Get(Name);
                if (value == null) {
                    // This error should hopefully never occur as the FunctionInfo is checked when generating.
                    throw new SolScriptInterpreterException(context,
                        "Cannot call function \"" + Name + "\" on instance of class \"" + instance.Type +
                        "\": The class does not have such a function.");
                }
                return value;
            }

            public void set(SolExecutionContext context, SolClass instance, SolValue value) {
                instance.GlobalVariables.Assign(Name, value);
            }
        }

        public FunctionInfo get_function(SolExecutionContext context, SolValue rawClass, string functionName) {
            SolClassDefinition classDef = GetClassDef(context, rawClass);
            // todo: reflection only looks at the top level - step into lower levels!
            foreach (string funcDefName in classDef.FunctionNames) {
                if (funcDefName == functionName) {
                    return new FunctionInfo(functionName, classDef.Type);
                }
            }
            throw new SolScriptInterpreterException(context,
                "Cannot get function \"" + functionName + "\" of class \"" + classDef.Type +
                "\". No function with this name exists.");
        }

        public FieldInfo get_field(SolExecutionContext context, SolValue rawClass, string fieldName) {
            SolClassDefinition classDef = GetClassDef(context, rawClass);
            foreach (string fieldDefName in classDef.FieldNames) {
                if (fieldDefName == fieldName) {
                    return new FieldInfo(fieldName, classDef.Type);
                }
            }
            throw new SolScriptInterpreterException(context,
                "Cannot get field \"" + fieldName + "\" of class \"" + classDef.Type +
                "\". No field with this name exists.");
        }

        [SolLibraryVisibility(SolLibrary.STD_NAME, false)]
        private SolClassDefinition GetClassDef(SolExecutionContext context, SolValue rawClass) {
            string className;
            if (rawClass.IsClass) {
                className = rawClass.Type;
            } else if (rawClass.Type == SolString.TYPE) {
                className = ((SolString) rawClass).Value;
            } else {
                throw new SolScriptInterpreterException(context,
                    "Cannot get the class of a \"" + rawClass.Type + "\" value. Only strings and classes are supported.");
            }
            SolClassDefinition classDef;
            if (!context.Assembly.TypeRegistry.TryGetClass(className, out classDef)) {
                throw new SolScriptInterpreterException(context,
                    "Cannot get the class named \"" + className + "\". The class does not exist.");
            }
            return classDef;
        }

        // ReSharper restore InconsistentNaming
    }
}