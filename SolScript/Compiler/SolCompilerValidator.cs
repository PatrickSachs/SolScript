using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Properties;

namespace SolScript.Compiler
{
    /// <summary>
    ///     The validator is used to check if SolScript scripts are correct before they can be compiled or run.
    /// </summary>
    public class SolCompilerValidator
    {
        /// <summary>
        ///     Creates a new validator for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" /></exception>
        public SolCompilerValidator([NotNull] SolAssembly assembly)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Assembly = assembly;
        }

        /// <summary>
        ///     The assembly this validator is validating.
        /// </summary>
        public SolAssembly Assembly { get; }

        /// <exception cref="SolCompilerException">
        ///     The class is invalid. See the exception and its possible nested inner
        ///     exceptions for details.
        /// </exception>
        public void ValidateClass(SolClassDefinition solClass, SolValidationContext context)
        {
            context.InClassDefinition = solClass;
            Stack <SolClassDefinition> inheritanceChain = solClass.GetInheritanceReversed();
            while (inheritanceChain.Count != 0) {
                SolClassDefinition current = inheritanceChain.Pop();
                foreach (SolFunctionDefinition function in current.DeclaredFunctions) {
                    try {
                        ValidateFunction(function, context);
                    } catch (SolCompilerException ex) {
                        throw new SolCompilerException(solClass.Location, CompilerResources.Err_ClassFunctionIsInvalid.FormatWith(solClass.Type, function), ex);
                    }
                }
                foreach (SolFieldDefinition field in current.DeclaredFields) {
                    try {
                        ValidateField(field, context);
                    } catch (SolCompilerException ex) {
                        throw new SolCompilerException(solClass.Location, CompilerResources.Err_ClassFieldIsInvalid.FormatWith(solClass.Type, field), ex);
                    }
                }
            }
            // Non abstract class need to implement all abstract functions.
            if (solClass.TypeMode != SolTypeMode.Abstract) {
                if (solClass.GetFlatFunctions().Any(f => f.MemberModifier == SolMemberModifier.Abstract)) {
                    throw new SolCompilerException(solClass.Location,
                        CompilerResources.Err_ClassDoesNotImplementAllAbstracts.FormatWith(solClass.Type,
                            solClass.GetFlatFunctions().Where(f => f.MemberModifier == SolMemberModifier.Abstract).JoinToString()));
                }
            }
            // Validate Meta Functions
            foreach (KeyValuePair<SolMetaFunction, SolClassDefinition.MetaFunctionLink> metaFunction in solClass.DeclaredMetaFunctions) {
                SolFunctionDefinition functionDefinition = metaFunction.Value.Definition;
                SolMetaFunction meta = metaFunction.Key;
                if (functionDefinition.AccessModifier != SolAccessModifier.Internal) {
                    throw new SolCompilerException(functionDefinition.Location, $"The meta function \"{functionDefinition}\" must be internal.");
                }
                if (!meta.Type.IsCompatible(Assembly, functionDefinition.Type)) {
                    throw new SolCompilerException(functionDefinition.Location,
                        $"The return type \"{functionDefinition.Type}\" of meta function \"{functionDefinition}\" is not compatible with the required return type \"{meta.Type}\"");
                }
                if (meta.Parameters != null) {
                    if (functionDefinition.ParameterInfo.Count != meta.Parameters.Types.Count || functionDefinition.ParameterInfo.AllowOptional != meta.Parameters.AllowOptional) {
                        throw new SolCompilerException(functionDefinition.Location,
                            $"The meta function \"{functionDefinition}\" has {functionDefinition.ParameterInfo.Count} parameters, and {(functionDefinition.ParameterInfo.AllowOptional ? "allows" : "does not allow")} optional arguments. The requirements for this function are {meta.Parameters.Types.Count} parameters and {(meta.Parameters.AllowOptional ? "allowed" : "not allowed")} optional parameters.");
                    }
                    for (int i = 0; i < meta.Parameters.Types.Count; i++) {
                        SolParameter parameter = functionDefinition.ParameterInfo[i];
                        if (!parameter.Type.IsCompatible(Assembly, meta.Parameters.Types[i])) {
                            throw new SolCompilerException(functionDefinition.Location,
                                $"The parameter \"{parameter.Name}\" of meta function \"{functionDefinition}\" is of type \"{parameter.Type}\", but should be of type \"{meta.Parameters.Types[i]}\"(or a compatible one).");
                        }
                    }
                }
            }
            context.InClassDefinition = null;
        }

        /// <exception cref="SolCompilerException">
        ///     The field is invalid. See the exception and its possible nested inner
        ///     exceptions for details.
        /// </exception>
        public void ValidateField(SolFieldDefinition solField, SolValidationContext context, bool validateInitializer = true)
        {
            context.InFieldDefinition = solField;
            SolClassDefinition classDefinition = solField.DefinedIn;
            if (classDefinition != null) {
                //  ==================================================================
                //      CLASS FIELD
                //  ==================================================================
                {
                    // Field may not conflict with any function at same level or non local function at other level.
                    SolFunctionDefinition conflictFunction;
                    if (classDefinition.TryGetFunction(solField.Name, false, out conflictFunction, delegate(SolFunctionDefinition f) {
                        if (f.DefinedIn == classDefinition) {
                            return true;
                        }
                        return f.AccessModifier != SolAccessModifier.Local;
                    })) {
                        throw new SolCompilerException(solField.Location, CompilerResources.Err_ClassFieldConflictsWithFunction.FormatWith(classDefinition.Type, conflictFunction, solField));
                    }
                }
                {
                    // Fields may not conflict with other fields at other levels. Only local fields at lower levels are allowed.
                    SolFieldDefinition conflictField;
                    if (classDefinition.BaseClass != null && classDefinition.BaseClass.TryGetField(solField.Name, false, out conflictField, f => f.AccessModifier != SolAccessModifier.Local)) {
                        throw new SolCompilerException(solField.Location, CompilerResources.Err_ConflictingClassField.FormatWith(classDefinition.Type, solField, conflictField));
                    }
                }
            } else {
                //  ==================================================================
                //      GLOBAL DEFINED FIELD
                //  ==================================================================
                {
                    // Global field and function names may not conflict.
                    SolFunctionDefinition conflictFunction;
                    if (solField.Assembly.TryGetGlobalFunction(solField.Name, out conflictFunction)) {
                        throw new SolCompilerException(solField.Location, CompilerResources.Err_GlobalFieldConflictsWithFunction.FormatWith(solField, conflictFunction));
                    }
                }
            }
            if (solField.Initializer.FieldType == SolFieldInitializerWrapper.Type.ScriptField && validateInitializer) {
                if (!solField.Initializer.GetScriptField().Validate(context)) {
                    throw new SolCompilerException(solField.Initializer.GetScriptField().Location, CompilerResources.Err_FailedToValidateField.FormatWith(solField.Name));
                }
            }
            context.InFieldDefinition = null;
        }

        /// <summary>Validates a function for correctness.</summary>
        /// <exception cref="SolCompilerException">
        ///     The function is invalid. See the exception and its possible nested inner
        ///     exceptions for details.
        /// </exception>
        public void ValidateFunction(SolFunctionDefinition solFunction, SolValidationContext context, bool validateChunk = true)
        {
            context.InFunctionDefinition = solFunction;
            SolClassDefinition classDefinition = solFunction.DefinedIn;
            if (classDefinition != null) {
                //  ==================================================================
                //      CLASS FUNCTION
                //  ==================================================================
                if (solFunction.MemberModifier == SolMemberModifier.Abstract) {
                    // An abstract function must be in an abstarct class.
                    if (classDefinition.TypeMode != SolTypeMode.Abstract) {
                        throw new SolCompilerException(solFunction.Location, CompilerResources.Err_AbstractFunctionIsNonAbstractClass.FormatWith(solFunction, classDefinition.Type));
                    }
                }
                {
                    // An override function must override a function that: 
                    //   a) exists
                    //   b) has the same access
                    // Of course the function itself must be marked as override in over to be able to overrie.
                    SolFunctionDefinition overriddenFunction;
                    if (classDefinition.BaseClass != null && classDefinition.BaseClass.TryGetFunction(solFunction.Name, false, out overriddenFunction, f => f.AccessModifier != SolAccessModifier.Local)) {
                        if (solFunction.MemberModifier == SolMemberModifier.Override) {
                            if (solFunction.AccessModifier != overriddenFunction.AccessModifier) {
                                throw new SolCompilerException(solFunction.Location,
                                    CompilerResources.Err_ClassFunctionOverridesOtherAccess.FormatWith(classDefinition.Type, solFunction, solFunction.AccessModifier, overriddenFunction,
                                        overriddenFunction.AccessModifier));
                            }
                        } else {
                            throw new SolCompilerException(solFunction.Location, CompilerResources.Err_ClassFunctionDoesNotOverride.FormatWith(classDefinition.Type, solFunction, overriddenFunction));
                        }
                    } else {
                        // We have an override function but no function to override.
                        if (solFunction.MemberModifier == SolMemberModifier.Override) {
                            throw new SolCompilerException(solFunction.Location, CompilerResources.Err_ClassFunctionOverridesNothing.FormatWith(classDefinition.Type, solFunction));
                        }
                    }
                }
                {
                    // Function and field names may not conflict.
                    SolFieldDefinition conflictField;
                    if (classDefinition.TryGetField(solFunction.Name, false, out conflictField, delegate(SolFieldDefinition f) {
                        if (f.DefinedIn == classDefinition) {
                            // Conflicts at self level never allowed.
                            return true;
                        }
                        // We don't worry about base level access.
                        return f.AccessModifier != SolAccessModifier.Local;
                    })) {
                        throw new SolCompilerException(solFunction.Location, CompilerResources.Err_ClassFieldConflictsWithFunction.FormatWith(classDefinition.Type, solFunction, conflictField));
                    }
                }
            } else {
                //  ==================================================================
                //      GLOBAL DEFINED FUNCTION
                //  ==================================================================
                // Global-Defined functions may not have member modifier.
                if (solFunction.MemberModifier != SolMemberModifier.Default) {
                    throw new SolCompilerException(solFunction.Location, CompilerResources.Err_GlobalFunctionMayNotHaveModifier.FormatWith(solFunction, solFunction.MemberModifier));
                }
                {
                    // Global field and function names may not conflict.
                    SolFieldDefinition conflictField;
                    if (solFunction.Assembly.TryGetGlobalField(solFunction.Name, out conflictField)) {
                        throw new SolCompilerException(solFunction.Location, CompilerResources.Err_GlobalFieldConflictsWithFunction.FormatWith(solFunction, conflictField));
                    }
                }
            }
            //  ==================================================================
            //      VALIDATE NOT DIRECTLY CLASS DEPENDANT DATA
            //  ==================================================================
            // Local-Access functions may not have member modifier.
            if (solFunction.AccessModifier == SolAccessModifier.Local && solFunction.MemberModifier != SolMemberModifier.Default) {
                throw new SolCompilerException(solFunction.Location, CompilerResources.Err_FunctionAccessProhibitsModifier.FormatWith(solFunction, SolAccessModifier.Local, solFunction.MemberModifier));
            }
            if (solFunction.Chunk.ChunkType == SolChunkWrapper.Type.ScriptChunk && validateChunk) {
                if (!solFunction.Chunk.GetScriptChunk().Validate(context)) {
                    throw new SolCompilerException(solFunction.Location, CompilerResources.Err_FailedToValidateFunction.FormatWith(solFunction.Name));
                }
            }
            context.InFunctionDefinition = null;
        }
    }
}