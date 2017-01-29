using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Classes
{
    // ReSharper disable InconsistentNaming
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Reflect")]
    public class ReflectionModule
    {
        [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
        [SolLibraryName("FunctionInfo")]
        public class FunctionInfo
        {
            public FunctionInfo(string name, string type)
            {
                Name = name;
                Type = type;
            }

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Name;

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Type;

            #region Overrides

            public override string ToString()
            {
                return "FunctionInfo(class=" + Type + ", name=" + Name + ")";
            }

            #endregion

            public SolString get_name()
            {
                return new SolString(Name);
            }

            public SolString get_compatible_class()
            {
                return new SolString(Type);
            }

            public SolValue call(SolExecutionContext context, SolClass instance, params SolValue[] args)
            {
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

        public override string ToString()
        {
            return "Reflection Module";
        }

        [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
        [SolLibraryName("FieldInfo")]
        public class FieldInfo
        {
            public FieldInfo(string name, string type)
            {
                Name = name;
                Type = type;
            }

            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Name;
            [SolLibraryVisibility(SolLibrary.STD_NAME, false)] internal readonly string Type;

            #region Overrides

            public override string ToString()
            {
                return "FieldInfo(class=" + Type + ", name=" + Name + ")";
            }

            #endregion

            public SolString get_name()
            {
                return new SolString(Name);
            }

            public SolString get_compatible_class()
            {
                return new SolString(Type);
            }

            public SolValue get(SolExecutionContext context, SolClass instance)
            {
                // todo: redo reflection; cant get lower level locals
                try {
                    return instance.InheritanceChain.Variables.Get(Name);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Cannot call function \"" + Name + "\" on instance of class \"" + instance.Type + "\"", ex);
                }
            }

            public void set(SolExecutionContext context, SolClass instance, SolValue value)
            {
                instance.GlobalVariables.Assign(Name, value);
            }
        }

        public FunctionInfo get_function(SolExecutionContext context, [SolContract("class", false)] SolValue rawClass, string functionName)
        {
            SolClassDefinition classDef = GetClassDef(context, rawClass);
            // todo: reflection only looks at the top level - step into lower levels!
            foreach (string funcDefName in classDef.FunctionNames) {
                if (funcDefName == functionName) {
                    return new FunctionInfo(functionName, classDef.Type);
                }
            }
            throw new SolRuntimeException(context, "Cannot get function \"" + functionName + "\" of class \"" + classDef.Type + "\". No function with this name exists.");
        }

        public FieldInfo get_field(SolExecutionContext context, [SolContract("class", false)] SolValue rawClass, string fieldName)
        {
            SolClassDefinition classDef = GetClassDef(context, rawClass);
            foreach (string fieldDefName in classDef.FieldNames) {
                if (fieldDefName == fieldName) {
                    return new FieldInfo(fieldName, classDef.Type);
                }
            }
            throw new SolRuntimeException(context, "Cannot get field \"" + fieldName + "\" of class \"" + classDef.Type + "\". No field with this name exists.");
        }

        [SolLibraryVisibility(SolLibrary.STD_NAME, false)]
        private static SolClassDefinition GetClassDef(SolExecutionContext context, SolValue rawClass)
        {
            SolClass solClass = rawClass as SolClass;
            if (solClass == null) {
                throw new SolRuntimeException(context, "Cannot get class defintion of a " + rawClass.Type + " value.");
            }
            return solClass.InheritanceChain.Definition;
        }

        // ReSharper restore InconsistentNaming
    }
}