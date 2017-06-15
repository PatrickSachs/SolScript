// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_Reflect" /> singleton is used to access information about language elements at runtime.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Singleton, typeof(std_Reflect)), SolLibraryName(TYPE), PublicAPI]
    public class std_Reflect : INativeClassSelf
    {
        /// <summary>
        ///     The type name is "Reflect".
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)]
        public const string TYPE = "Reflect";

        private static readonly ReadOnlyDictionary<SolTypeMode, SolString> s_TypeModeNames = new PSDictionary<SolTypeMode, SolString> {
            [SolTypeMode.Default] = "default",
            [SolTypeMode.Abstract] = "abstract",
            [SolTypeMode.Annotation] = "annotation",
            [SolTypeMode.Sealed] = "sealed",
            [SolTypeMode.Singleton] = "singleton"
        }.AsReadOnly();

        private static readonly ReadOnlyDictionary<SolAccessModifier, SolString> s_AccessModifierNames = new PSDictionary<SolAccessModifier, SolString> {
            [SolAccessModifier.Global] = "global",
            [SolAccessModifier.Local] = "local",
            [SolAccessModifier.Internal] = "internal"
        }.AsReadOnly();

        private static readonly ReadOnlyDictionary<SolMemberModifier, SolString> s_MemberModifierNames = new PSDictionary<SolMemberModifier, SolString> {
            [SolMemberModifier.Default] = "default",
            [SolMemberModifier.Abstract] = "abstract",
            [SolMemberModifier.Override] = "override"
        }.AsReadOnly();

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
        private static readonly SolString Str_access_modifier = SolString.ValueOf("access_modifier").Intern();
        private static readonly SolString Str_parameters = SolString.ValueOf("parameters").Intern();
        private static readonly SolString Str_optional = SolString.ValueOf("optional").Intern();
        private static readonly SolString Str_member_modifier = SolString.ValueOf("member_modifier").Intern();

        #region INativeClassSelf Members

        /// <inheritdoc />
        public SolClass Self { get; set; }

        #endregion

        /// <summary>
        ///     Gets information about a function.
        /// </summary>
        /// <param name="context" />
        /// <param name="functionRaw">The function(Must either be a string or a function.)</param>
        /// <param name="onClass">
        ///     (Optional) The class the function was declared in(Must either be string or class instance). Pass
        ///     nil is global function.
        /// </param>
        /// <returns>A table containing the information.</returns>
        /// <exception cref="SolRuntimeException">The function could not be found.</exception>
        /// <exception cref="SolRuntimeException">Cannot find the class.</exception>
        /// <remarks>
        ///     Table fields:
        ///     <list type="table">
        ///         <item>
        ///             <term><c>name</c> (string)</term>
        ///             <description>The name of the function.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>access_modifier</c> (string)</term>
        ///             <description>The access modifier of the function.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>member_modifier</c> (string)</term>
        ///             <description>The member modifier of the function.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>defined_in</c> (string)</term>
        ///             <description>The class name this function was defined in. (nil if lamda or global function)</description>
        ///         </item>
        ///         <item>
        ///             <term><c>type</c> (string)</term>
        ///             <description>The return type name of the function.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>can_be_nil</c> (bool)</term>
        ///             <description>Can be function return nil?</description>
        ///         </item>
        ///         <item>
        ///             <term><c>parameters</c> (table)</term>
        ///             <description>
        ///                 The parameters of this function. Contains an array part with tables describing each parameter(Fields:
        ///                 <c>name, type, can_be_nil</c>). Also contains the <c>optional</c> field, stating if the field accepts
        ///                 optional arguments.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term><c>annotations</c> (table)</term>
        ///             <description>An array containing all class names of the annotations on this function.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>source_location</c> (table)</term>
        ///             <description>
        ///                 The location in code the function was declared at. (Table fields: <c>file, line, column)</c>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public SolTable get_function_info(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolValue functionRaw, [SolContract(SolString.TYPE, true)] SolValue onClass)
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
                    [Str_access_modifier] = s_AccessModifierNames[definition.AccessModifier],
                    [Str_member_modifier] = s_MemberModifierNames[definition.MemberModifier],
                    [Str_defined_in] = definition.DefinedIn != null ? (SolValue) SolString.ValueOf(definition.DefinedIn.Type) : SolNil.Instance,
                    [Str_type] = SolString.ValueOf(definition.Type.Type),
                    [Str_can_be_nil] = SolBool.ValueOf(definition.Type.CanBeNil),
                    [Str_parameters] = GetParametersTable(definition.ParameterInfo),
                    [Str_annotations] = GetAnnotationsTable(definition.DeclaredAnnotations),
                    [Str_source_location] = GetSourceLocationTable(definition.Location)
                };
            }
            return new SolTable {
                [Str_name] = SolString.ValueOf("function#" + function.Id),
                [Str_access_modifier] = s_AccessModifierNames[SolAccessModifier.Global],
                [Str_member_modifier] = s_MemberModifierNames[SolMemberModifier.Default],
                [Str_defined_in] = SolNil.Instance,
                [Str_type] = SolString.ValueOf(function.ReturnType.Type),
                [Str_can_be_nil] = SolBool.ValueOf(function.ReturnType.CanBeNil),
                [Str_parameters] = GetParametersTable(function.ParameterInfo),
                [Str_annotations] = new SolTable(),
                [Str_source_location] = GetSourceLocationTable(function.Location)
            };
        }


        /// <summary>
        ///     Get information about a field definition.
        /// </summary>
        /// <param name="context" />
        /// <param name="fieldName">
        ///     The name of the field. Only strings are accepted since you cannot passs a reference to the
        ///     field.
        /// </param>
        /// <param name="onClass">(Optional) The class of the field(String or class instance). Nil if global field.</param>
        /// <returns>A table descripting the field.</returns>
        /// <exception cref="SolRuntimeException">Cannot find the field.</exception>
        /// <exception cref="SolRuntimeException">Cannot find the class.</exception>
        /// <remarks>
        ///     Table fields:
        ///     <list type="table">
        ///         <item>
        ///             <term><c>name</c> (string)</term>
        ///             <description>The name of the field.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>access_modifier</c> (string)</term>
        ///             <description>The access modifier of the field.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>defined_in</c> (string)</term>
        ///             <description>The class name this field was defined in. (nil if global field)</description>
        ///         </item>
        ///         <item>
        ///             <term><c>type</c> (string)</term>
        ///             <description>The type name of the field.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>can_be_nil</c> (bool)</term>
        ///             <description>Can be field be nil?</description>
        ///         </item>
        ///         <item>
        ///             <term><c>annotations</c> (table)</term>
        ///             <description>An array containing all class names of the annotations on this field.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>source_location</c> (table)</term>
        ///             <description>
        ///                 The location in code the field was declared at. (Table fields: <c>file, line, column)</c>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        [PublicAPI]
        public SolTable get_field_info(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString fieldName, [SolContract(SolValue.ANY_TYPE, true)] SolValue onClass)
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
                [Str_access_modifier] = SolString.ValueOf(definition.AccessModifier.ToString()),
                [Str_defined_in] = definition.DefinedIn != null ? (SolValue) SolString.ValueOf(definition.DefinedIn.Type) : SolNil.Instance,
                [Str_type] = SolString.ValueOf(definition.Type.Type),
                [Str_can_be_nil] = SolBool.ValueOf(definition.Type.CanBeNil),
                [Str_annotations] = GetAnnotationsTable(definition.DeclaredAnnotations),
                [Str_source_location] = GetSourceLocationTable(definition.Location)
            };
            return table;
        }


        /// <summary>
        ///     Gets information about a class definition.
        /// </summary>
        /// <param name="context" />
        /// <param name="value">The class. (Class name or instance)</param>
        /// <returns>A table describing the class.</returns>
        /// <exception cref="SolRuntimeException">Cannot find the class.</exception>
        /// <remarks>
        ///     Table fields:
        ///     <list type="table">
        ///         <item>
        ///             <term><c>type</c> (string)</term>
        ///             <description>The type name of the class.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>base_type</c> (string)</term>
        ///             <description>The base type name. nil if none.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>mode</c> (string)</term>
        ///             <description>The type mode of the class.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>fields</c> (table)</term>
        ///             <description>An array containing the names of all fields declared in this class.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>functions</c> (table)</term>
        ///             <description>An array containing the names of all functions declared in this class.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>annotations</c> (table)</term>
        ///             <description>An array containing all class names of the annotations on this field.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>source_location</c> (table)</term>
        ///             <description>
        ///                 The location in code the field was declared at. (Table fields: <c>file, line, column)</c>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        [SolContract(SolTable.TYPE, false), PublicAPI]
        public SolTable get_class_info(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue value)
        {
            SolClassDefinition definition = GetClassDefinition(context, value);
            SolTable table = new SolTable {
                [Str_type] = SolString.ValueOf(definition.Type),
                [Str_base_type] = definition.BaseClass != null ? (SolValue) SolString.ValueOf(definition.BaseClass.Type) : SolNil.Instance,
                [Str_mode] = s_TypeModeNames[definition.TypeMode],
                [Str_fields] = GetFieldsTable(definition.DeclaredFields),
                [Str_functions] = GetFunctionsTable(definition.DeclaredFunctions),
                [Str_annotations] = GetAnnotationsTable(definition.DeclaredAnnotations),
                [Str_source_location] = GetSourceLocationTable(definition.Location)
            };
            return table;
        }

        /*public SolTable get_function_annotations(SolExecutionContext context, SolFunction instance, SolString type)
        {
            SolClassFunction classFunction = instance as SolClassFunction;
            GlobalSolFunction globalFunction = instance as GlobalSolFunction;
            IReadOnlyList<SolClass> annotations;
            if (classFunction != null) {
                SolClass theClass = classFunction.ClassInstance;
                SolClass.Inheritance inheritance = theClass.FindInheritance(classFunction.Definition.DefinedIn).NotNull();
                try {
                    annotations = inheritance.GetVariables(classFunction.Definition.AccessModifier, SolClass.Inheritance.SolVariableMode.Declarations).GetAnnotations(classFunction.Definition.Name);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Cannot get annotations of function \"" + theClass.Type + "." + classFunction.Definition.Name + "\".", ex);
                }
            } else if (globalFunction != null) {
                annotations = globalFunction.Assembly.GetVariables(globalFunction.Definition.AccessModifier).GetAnnotations(globalFunction.Definition.Name);
            } else {
                // Lamda functions do not have annotations.
                return new SolTable();
            }
        }*/

        /// <summary>
        ///     Gets the annotations of a global field or function.
        /// </summary>
        /// <param name="context" />
        /// <param name="member">The member name.</param>
        /// <param name="type">(Optional) The annotation type. nil if you wish to obtain all annotations.</param>
        /// <returns>An array containing the annotations.</returns>
        /// <exception cref="SolRuntimeException">Failed to obtain the annotations.</exception>
        public SolTable get_global_annotations(SolExecutionContext context, SolString member, SolString type)
        {
            ReadOnlyList<SolClass> annotations;
            try {
                annotations = context.Assembly.LocalVariables.GetAnnotations(member.Value);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to get annotations of global \"" + member.Value + "\".", ex);
            }
            if (type == null) {
                return SolTable.Of(annotations);
            }
            SolType typeCheck = new SolType(type, false);
            return SolTable.Of(annotations.Where(a => typeCheck.IsCompatible(context.Assembly, a.Type)));
        }

        /// <summary>
        ///     Gets the annotations of a member of a class.
        /// </summary>
        /// <param name="context" />
        /// <param name="instance">The class instance.</param>
        /// <param name="member">The member name.</param>
        /// <param name="type">(Optional) The annotation type. nil if you wish to obtain all annotations.</param>
        /// <returns>An array containing the annotations.</returns>
        /// <exception cref="SolRuntimeException">Failed to obtain the annotations.</exception>
        public SolTable get_member_annotations(SolExecutionContext context, SolClass instance, SolString member, SolString type)
        {
            ReadOnlyList<SolClass> annotations = null;
            // todo: var source that literally searches EVERYTHING.
            // Check if the member is a global/internal
            IVariables source = instance.InheritanceChain.GetVariables(SolAccessModifier.Internal, SolVariableMode.All);
            if (source.IsDeclared(member.Value)) {
                try {
                    annotations = source.GetAnnotations(member.Value);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Failed to get annotations of member \"" + member.Value + "\" on a class instance of type \"" + instance.Type + "\".", ex);
                }
            } else {
                // If not - Well then let's being the quest to look through all the locals.
                SolClass.Inheritance active = instance.InheritanceChain;
                while (active != null) {
                    if ((source = active.GetVariables(SolAccessModifier.Local, SolVariableMode.Declarations)).IsDeclared(member.Value)) {
                        try {
                            annotations = source.GetAnnotations(member.Value);
                        } catch (SolVariableException ex) {
                            throw new SolRuntimeException(context, "Failed to get annotations of member \"" + member.Value + "\" on a class instance of type \"" + instance.Type + "\".", ex);
                        }
                    }
                    active = active.BaseInheritance;
                }
            }
            if (annotations == null) {
                throw new SolRuntimeException(context,
                    "Failed to get annotations of member \"" + member.Value + "\" on a class instance of type \"" + instance.Type + "\". No member with such a name exists.");
            }
            if (type == null) {
                return SolTable.Of(annotations);
            }
            SolType typeCheck = new SolType(type, false);
            return SolTable.Of(annotations.Where(a => typeCheck.IsCompatible(context.Assembly, a.Type)));
        }

        /// <summary>
        ///     Gets the annotations of a class.
        /// </summary>
        /// <param name="context" />
        /// <param name="instance">The class instance.</param>
        /// <param name="type">(Optional) The annotation type. nil if you wish to obtain all annotations.</param>
        /// <returns>An array containing the annotations.</returns>
        public SolTable get_class_annotations(SolExecutionContext context, SolClass instance, SolString type)
        {
            if (type == null) {
                return SolTable.Of(instance.Annotations);
            }
            SolType typeCheck = new SolType(type, false);
            return SolTable.Of(instance.Annotations.Where(a => typeCheck.IsCompatible(context.Assembly, a.Type)));
        }

        /// <summary>
        ///     Creates a new instance of the given class. Instance creation is enforced.
        /// </summary>
        /// <param name="context" />
        /// <param name="className">The class name.</param>
        /// <param name="args">The constructor.</param>
        /// <returns>The class.</returns>
        [SolContract(SolValue.CLASS_TYPE, false)]
        public SolClass create_instance(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString className, params SolValue[] args)
        {
            try {
                return context.Assembly.New(className, ClassCreationOptions.Enforce(), args);
            } catch (ArgumentException ex) {
                throw new SolRuntimeException(context, "The class \"" + className + "\" cannot be instantiated.", ex);
            } catch (SolTypeRegistryException ex) {
                throw new SolRuntimeException(context, "An error occured while creating an instance of class \"" + className + "\".", ex);
            }
        }

        #region Native Helpers

        private SolTable GetSourceLocationTable(SourceLocation location)
        {
            return new SolTable {
                [Str_file] = SolString.ValueOf(location.File).Intern(),
                [Str_line] = new SolNumber(location.Line),
                [Str_column] = new SolNumber(location.Column)
            };
        }


        private SolTable GetFieldsTable(IEnumerable<SolFieldDefinition> fields)
        {
            SolTable table = new SolTable();
            foreach (SolFieldDefinition field in fields) {
                table.Append(SolString.ValueOf(field.Name));
            }
            return table;
        }

        private SolTable GetFunctionsTable(IEnumerable<SolFunctionDefinition> functions)
        {
            SolTable table = new SolTable();
            foreach (SolFunctionDefinition function in functions) {
                table.Append(SolString.ValueOf(function.Name));
            }
            return table;
        }

        private SolTable GetAnnotationsTable(IEnumerable<SolAnnotationDefinition> annotations)
        {
            SolTable table = new SolTable();
            foreach (SolAnnotationDefinition annotation in annotations) {
                table.Append(SolString.ValueOf(annotation.Definition.Type));
            }
            return table;
        }

        private SolTable GetParametersTable(SolParameterInfo parameters)
        {
            SolTable table = new SolTable();
            foreach (SolParameter parameter in parameters) {
                table.Append(new SolTable {
                    [Str_name] = SolString.ValueOf(parameter.Name),
                    [Str_type] = SolString.ValueOf(parameter.Type.Type),
                    [Str_can_be_nil] = SolBool.ValueOf(parameter.Type.CanBeNil)
                });
            }
            table[Str_optional] = SolBool.ValueOf(parameters.AllowOptional);
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

        #endregion
    }
}