using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Libraries.std
{
    // ReSharper disable InconsistentNaming
    [SolLibraryClass(std.NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Reflect")]
    [PublicAPI]
    public class std_Reflect
    {
        static std_Reflect()
        {
            SolString.ValueOf(nameof(SolTypeMode.Default)).Intern();
            SolString.ValueOf(nameof(SolTypeMode.Abstract)).Intern();
            SolString.ValueOf(nameof(SolTypeMode.Annotation)).Intern();
            SolString.ValueOf(nameof(SolTypeMode.Sealed)).Intern();
            SolString.ValueOf(nameof(SolTypeMode.Singleton)).Intern();
            SolString.ValueOf(nameof(SolAccessModifier.None)).Intern();
            SolString.ValueOf(nameof(SolAccessModifier.Local)).Intern();
            SolString.ValueOf(nameof(SolAccessModifier.Internal)).Intern();
            SolString.ValueOf(SolSourceLocation.NATIVE_FILE).Intern();
        }

        private static readonly SolString Str_name = SolString.ValueOf("name").Intern();
        private static readonly SolString Str_can_be_nil = SolString.ValueOf("can_be_nil").Intern();
        private static readonly SolString Str_defined_in = SolString.ValueOf("defined_in").Intern();
        private static readonly SolString Str_type = SolString.ValueOf("type").Intern();
        private static readonly SolString Str_base_type = SolString.ValueOf("base_type").Intern();
        private static readonly SolString Str_mode = SolString.ValueOf("mode").Intern();
        private static readonly SolString Str_source_location = SolString.ValueOf("source_location").Intern();
        private static readonly SolString Str_fields = SolString.ValueOf("fields").Intern();
        private static readonly SolString Str_functions = SolString.ValueOf("functions").Intern();
        private static readonly SolString Str_annotations = SolString.ValueOf("annotations").Intern();
        private static readonly SolString Str_file = SolString.ValueOf("file").Intern();
        private static readonly SolString Str_line = SolString.ValueOf("line").Intern();
        private static readonly SolString Str_column = SolString.ValueOf("column").Intern();
        private static readonly SolString Str_modifier = SolString.ValueOf("modifier").Intern();
        private static readonly SolString Str_parameters = SolString.ValueOf("parameters").Intern();

        private SolTable GetSourceLocation(SolSourceLocation location)
        {
            return new SolTable {
                [Str_file] = SolString.ValueOf(location.File).Intern(),
                [Str_line] = new SolNumber(location.Line),
                [Str_column] = new SolNumber(location.Column)
            };
        }

        /// <exception cref="SolRuntimeException">An error occured.</exception>
        public SolTable get_function_info(SolExecutionContext context, [SolContract("any", false)] SolValue functionRaw, [SolContract("any", true)] SolValue onClass)
        {
            SolFunction function;
            SolFunctionDefinition definition;
            if (functionRaw.Type == SolFunction.TYPE) {
                function = (SolFunction) functionRaw;
                definition = (function as DefinedSolFunction)?.Definition;
            } else if (functionRaw.Type == SolString.TYPE) {
                SolString functionName = (SolString) functionRaw;
                function = null;
                if (onClass.Type == SolNil.TYPE) {
                    if (!context.Assembly.TryGetGlobalFunction(functionName.Value, out definition)) {
                        throw new SolRuntimeException(context, "No global function with the name \"" + functionName + "\" exists.");
                    }
                } else {
                    if (!GetClassDefinition(context, onClass).TryGetFunction(functionName.Value, false, out definition)) {
                        throw new SolRuntimeException(context, "The class \"" + onClass.Type + "\" does not have a function with the name \"" + functionName + "\".");
                    }
                }
            } else {
                throw new SolRuntimeException(context, "Can only get function info from strings representing a function name or functions itself; Got value of type \"" + functionRaw.Type + "\".");
            }
            if (definition != null) {
                return new SolTable {
                    [Str_name] = SolString.ValueOf(definition.Name),
                    [Str_modifier] = SolString.ValueOf(definition.AccessModifier.ToString()),
                    [Str_annotations] = GetAnnotations(definition.Annotations),
                    [Str_defined_in] = definition.DefinedIn != null ? (SolValue) SolString.ValueOf(definition.DefinedIn.Type) : SolNil.Instance,
                    [Str_type] = SolString.ValueOf(definition.ReturnType.Type),
                    [Str_can_be_nil] = SolBool.ValueOf(definition.ReturnType.CanBeNil),
                    [Str_parameters] = GetParameters(definition.ParameterInfo),
                    [Str_source_location] = GetSourceLocation(definition.Location)
                };
            }
            return new SolTable {
                [Str_name] = SolNil.Instance,
                [Str_modifier] = SolNil.Instance,
                [Str_annotations] = SolNil.Instance,
                [Str_defined_in] = SolNil.Instance,
                [Str_type] = SolString.ValueOf(function.ReturnType.Type),
                [Str_can_be_nil] = SolBool.ValueOf(function.ReturnType.CanBeNil),
                [Str_parameters] = GetParameters(function.ParameterInfo),
                [Str_source_location] = GetSourceLocation(function.Location)
            };
        }

        private SolTable GetFields(IEnumerable<SolFieldDefinition> fields)
        {
            SolTable table = new SolTable();
            foreach (SolFieldDefinition field in fields) {
                table.Append(SolString.ValueOf(field.Name));
            }
            return table;
        }

        private SolTable GetFunctions(IEnumerable<SolFunctionDefinition> functions)
        {
            SolTable table = new SolTable();
            foreach (SolFunctionDefinition function in functions) {
                table.Append(SolString.ValueOf(function.Name));
            }
            return table;
        }

        private SolTable GetAnnotations(IEnumerable<SolAnnotationDefinition> annotations)
        {
            SolTable table = new SolTable();
            foreach (SolAnnotationDefinition annotation in annotations) {
                table.Append(SolString.ValueOf(annotation.Definition.Type));
            }
            return table;
        }

        private SolTable GetParameters(SolParameterInfo parameters)
        {
            SolTable table = new SolTable();
            foreach (SolParameter parameter in parameters) {
                table.Append(new SolTable {
                    [Str_name] = SolString.ValueOf(parameter.Name),
                    [Str_type] = SolString.ValueOf(parameter.Type.Type),
                    [Str_can_be_nil] = SolBool.ValueOf(parameter.Type.CanBeNil)
                });
            }
            return table;
        }

        /// <exception cref="SolRuntimeException">An error occured.</exception>
        [PublicAPI]
        public SolTable get_field_info(SolExecutionContext context, [SolContract("string", false)] SolString fieldName, [SolContract("any", true)] SolValue onClass)
        {
            SolFieldDefinition definition;
            if (onClass.Type != SolNil.TYPE) {
                if (!GetClassDefinition(context, onClass).TryGetField(fieldName.Value, false, out definition)) {
                    throw new SolRuntimeException(context, "The class \"" + onClass.Type + "\" does not have a field with the name \"" + fieldName + "\".");
                }
            } else {
                if (!context.Assembly.TryGetGlobalField(fieldName.Value, out definition)) {
                    throw new SolRuntimeException(context, "No global field with the name \"" + fieldName + "\" exists.");
                }
            }
#if DEBUG
            definition = definition.NotNull();
#endif
            SolTable table = new SolTable {
                [Str_name] = SolString.ValueOf(definition.Name),
                [Str_type] = SolString.ValueOf(definition.Type.Type),
                [Str_can_be_nil] = SolBool.ValueOf(definition.Type.CanBeNil),
                [Str_defined_in] = definition.DefinedIn != null ? (SolValue) SolString.ValueOf(definition.DefinedIn.Type) : SolNil.Instance,
                [Str_annotations] = GetAnnotations(definition.Annotations),
                [Str_source_location] = GetSourceLocation(definition.Location),
                [Str_modifier] = SolString.ValueOf(definition.AccessModifier.ToString())
            };
            return table;
        }

        /// <exception cref="SolRuntimeException">Could not obtain definition.</exception>
        private SolClassDefinition GetClassDefinition(SolExecutionContext context, SolValue value)
        {
            if (value.IsClass) {
                return ((SolClass) value).InheritanceChain.Definition;
            }
            if (value.Type == SolString.TYPE) {
                SolClassDefinition definition;
                SolString type = (SolString) value;
                if (!context.Assembly.TryGetClass(type.Value, out definition)) {
                    throw new SolRuntimeException(context, "Cannot get class info for type \"" + type.Value + "\". No class with this name exists.");
                }
                return definition;
            }
            throw new SolRuntimeException(context, "Can only get class info from strings representing a class name or classes; Got value of type \"" + value.Type + "\".");
        }

        /// <exception cref="SolRuntimeException">An error occured.</exception>
        [SolContract("table", false)]
        [PublicAPI]
        public SolTable get_class_info(SolExecutionContext context, [SolContract("any", false)] SolValue value)
        {
            SolClassDefinition definition = GetClassDefinition(context, value);
            SolTable table = new SolTable {
                [Str_type] = SolString.ValueOf(definition.Type),
                [Str_base_type] = definition.BaseClass != null ? (SolValue) SolString.ValueOf(definition.BaseClass.Type) : SolNil.Instance,
                [Str_mode] = SolString.ValueOf(definition.TypeMode.ToString()),
                [Str_fields] = GetFields(definition.Fields),
                [Str_functions] = GetFunctions(definition.Functions),
                [Str_annotations] = GetAnnotations(definition.Annotations),
                [Str_source_location] = GetSourceLocation(definition.Location)
            };
            return table;
        }
    }
}