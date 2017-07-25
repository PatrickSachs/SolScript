using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CSharp;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;
using SolScript.Properties;

namespace SolScript.Compiler.Native
{
    /// <summary>
    ///     This class actually compiles native code.
    /// </summary>
    public class NativeCompiler
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="compilerOptions" /> is <see langword="null" /></exception>
        public NativeCompiler([NotNull] Options compilerOptions)
        {
            if (compilerOptions == null) {
                throw new ArgumentNullException(nameof(compilerOptions));
            }
            CompilerOptions = compilerOptions;
        }

        /// <summary>
        ///     The options of this compiler.
        /// </summary>
        [NotNull]
        public Options CompilerOptions { get; set; }

        /// <summary>
        ///     Tries to dynamically compile the native class mapping for the given SolScript class definitions.
        /// </summary>
        /// <param name="definitions">The definitions to dynamically compile.</param>
        /// <param name="options">The assembly options.</param>
        /// <returns>The generated assembly.</returns>
        /// <exception cref="SolCompilerException">An error occured while compiling the mapping.</exception>
        public Assembly CompileNativeClassMapping(
            IEnumerable<SolClassDefinition> definitions)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CSharpCodeProvider csc = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            CompilerParameters parameters = new CompilerParameters(
                CompilerOptions.Assemblies.Select(s => s.Location).ToArray(),
                CompilerOptions.OutputFileName, false) {
                GenerateExecutable = false,
                TreatWarningsAsErrors = CompilerOptions.WarningsAreErrors,
                GenerateInMemory = !CompilerOptions.CreateAssemblyFile,
                OutputAssembly = CompilerOptions.OutputFileName
            };
            var namespaces = new PSDictionary<string, CodeNamespace>();
            var fullNameToDef = new PSDictionary<string, SolClassDefinition>();
            foreach (SolClassDefinition definition in definitions) {
                if (definition.DescriptorType != null) {
                    throw new SolCompilerException(definition.Location, CompilerResources.Err_DynMapClassIsAlreadyMapped.FormatWith(definition.Type, definition.DescriptorType.FullName));
                }
                List<SolClassDefinition> inheritance = definition.GetInheritance().ToList();
                // We are overriding the descriptor since we want to keep its signature.
                // And that's fine since the descriptor base methods will inject the values correctly anyway.
                if (inheritance.Count(i => i.DescriptorType != null) == 0) {
                    // Well, we could. But we're not going to. It serves no purpose. The reason why we create new
                    // override classes for native classes is that we will be able to use the overridden members.
                    throw new SolCompilerException(definition.Location, CompilerResources.Err_DynMapNoNativeDescriptor.FormatWith(definition.Type));
                }
                SolClassDefinition descriptorDef = inheritance.First(i => i.DescriptorType != null);
                CodeNamespace ns;
                {
                    string nsName = descriptorDef.DescriptorType.Namespace + ".SolClasses";
                    if (!namespaces.TryGetValue(nsName, out ns)) {
                        ns = namespaces[nsName] = new CodeNamespace(nsName);
                        compileUnit.Namespaces.Add(ns);
                    }
                }
                CodeTypeDeclaration classType = new CodeTypeDeclaration(definition.Type);
                ns.Types.Add(classType);
                classType.Attributes = MemberAttributes.Public;
                classType.BaseTypes.Add(descriptorDef.DescriptorType);
                classType.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SolNativeCompilerGeneratedAttribute))));

                fullNameToDef.Add(ns.Name + "." + classType.Name, definition);
                // INativeClassSelf
                {
                    classType.BaseTypes.Add(typeof(INativeClassSelf));

                    CodeMemberField selfField = new CodeMemberField(typeof(SolClass), nameof(INativeClassSelf.Self) + "__backingfield") {
                        Attributes = MemberAttributes.Private
                    };

                    CodeMemberProperty selfProp = new CodeMemberProperty {
                        Name = nameof(INativeClassSelf.Self),
                        Type = new CodeTypeReference(typeof(SolClass)),
                        Attributes = MemberAttributes.Public | MemberAttributes.Final
                    };
                    selfProp.ImplementationTypes.Add(typeof(INativeClassSelf));
                    selfProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), selfField.Name)));
                    selfProp.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), selfField.Name),
                        new CodePropertySetValueReferenceExpression()));

                    classType.Members.Add(selfField);
                    classType.Members.Add(selfProp);
                }
                // Functions
                var handledFuncNames = new PSHashSet<string>();
                foreach (SolClassDefinition inhCls in inheritance) {
                    foreach (SolFunctionDefinition function in inhCls.DeclaredFunctions) {
                        if (handledFuncNames.Contains(function.Name)
                            || function.MemberModifier != SolMemberModifier.Override
                            || function.AccessModifier == SolAccessModifier.Local) {
                            continue;
                        }
                        handledFuncNames.Add(function.Name);
                        // Okay, we found a function. Now find the lowest one it overrides.
                        foreach (SolClassDefinition inhClsRev in definition.GetInheritanceReversed()) {
                            // We don't want to step into the class the function was defined in as a function
                            // cannot override itself.
                            if (inhClsRev == inhCls) {
                                break;
                            }
                            SolFunctionDefinition overriddenDef;
                            if (inhClsRev.TryGetFunction(function.Name, true, out overriddenDef)) {
                                // It was overridden in this class.
                                if (overriddenDef == function) {
                                    throw new SolCompilerException(function.Location, CompilerResources.Err_DynMapFunctionTriedToOverrideSelf.FormatWith(function.ToString()));
                                }
                                // We did override a native method.
                                if (overriddenDef.Chunk.ChunkType != SolChunkWrapper.Type.NativeMethod) {
                                    continue;
                                }
                                MethodInfo overriddenMethod = overriddenDef.Chunk.GetNativeMethod();
                                CodeMemberMethod method = new CodeMemberMethod();
                                classType.Members.Add(method);
                                foreach (ParameterInfo parameter in overriddenMethod.GetParameters()) {
                                    method.Parameters.Add(new CodeParameterDeclarationExpression(parameter.ParameterType, parameter.Name));
                                }
                                method.Name = overriddenMethod.Name;
                                method.ReturnType = new CodeTypeReference(overriddenMethod.ReturnType);
                                method.Attributes = MemberAttributes.Override;
                                if (overriddenMethod.IsPublic) {
                                    method.Attributes |= MemberAttributes.Public;
                                } else if (overriddenMethod.IsFamilyAndAssembly) {
                                    method.Attributes |= MemberAttributes.FamilyAndAssembly;
                                } else if (overriddenMethod.IsFamilyOrAssembly) {
                                    method.Attributes |= MemberAttributes.FamilyOrAssembly;
                                } else if (overriddenMethod.IsFamily) {
                                    method.Attributes |= MemberAttributes.Family;
                                }

                                // Insert all arguments into an array.
                                method.Statements.Add(new CodeVariableDeclarationStatement(typeof(object[]), "__argArr"));
                                method.Statements.Add(new CodeAssignStatement(
                                    new CodeVariableReferenceExpression("__argArr"),
                                    new CodeArrayCreateExpression(typeof(object), method.Parameters.Count))
                                );
                                for (int i = 0; i < method.Parameters.Count; i++) {
                                    CodeParameterDeclarationExpression codeParam = method.Parameters[i];
                                    method.Statements.Add(new CodeAssignStatement(
                                        new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("__argArr"), new CodePrimitiveExpression(i)),
                                        new CodeVariableReferenceExpression(codeParam.Name))
                                    );
                                }
                                if (overriddenMethod.ReturnType == typeof(void)) {
                                    // We are returning void, Do not include a return statement.
                                    method.Statements.Add(new CodeMethodInvokeExpression(
                                        new CodeMethodReferenceExpression(
                                            new CodeTypeReferenceExpression(typeof(NativeCompiler)),
                                            nameof(MethodBodyNoConvert)
                                        ),
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), nameof(INativeClassSelf.Self)),
                                        new CodePrimitiveExpression(inhCls.Type),
                                        new CodeCastExpression(typeof(SolAccessModifier), new CodePrimitiveExpression((int) function.AccessModifier)),
                                        new CodePrimitiveExpression(function.Name),
                                        new CodeVariableReferenceExpression("__argArr")
                                    ));
                                } else {
                                    // We are not returning void. Build return statement and convert value.
                                    method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                                        new CodeMethodReferenceExpression(
                                            new CodeTypeReferenceExpression(typeof(NativeCompiler)),
                                            nameof(MethodBody),
                                            new CodeTypeReference(overriddenMethod.ReturnType)
                                        ),
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), nameof(INativeClassSelf.Self)),
                                        new CodePrimitiveExpression(inhCls.Type),
                                        new CodeCastExpression(typeof(SolAccessModifier), new CodePrimitiveExpression((int) function.AccessModifier)),
                                        new CodePrimitiveExpression(function.Name),
                                        new CodeVariableReferenceExpression("__argArr")
                                    )));
                                }
                                break;
                            }
                        }
                    }
                }
            }
            if (CompilerOptions.CreateSourceFile) {
                try {
                    using (TextWriter writer = File.CreateText(CompilerOptions.OutputFileName + ".cs")) {
                        csc.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions());
                    }
                } catch (Exception ex) {
                    throw new SolCompilerException(SolSourceLocation.Native(), CompilerResources.Err_DynMapFailedToWriteSourceFile, ex);
                }
            }
            CompilerResults results = csc.CompileAssemblyFromDom(parameters, compileUnit);
            List<CompilerError> errors = results.Errors.Cast<CompilerError>().ToList();
            if (CompilerOptions.WarningsAreErrors ? errors.Count != 0 : errors.Any(e => !e.IsWarning)) {
                StringBuilder errorBuilder = new StringBuilder();
                foreach (CompilerError error in results.Errors) {
                    errorBuilder.AppendLine(error.ToString());
                }
                throw new SolCompilerException(SolSourceLocation.Native(), CompilerResources.Err_DynMapCompilerError.FormatWith(fullNameToDef.Count, errorBuilder.ToString()));
            }
            Assembly assembly = results.CompiledAssembly;
            foreach (KeyValuePair<string, SolClassDefinition> defPair in fullNameToDef) {
                defPair.Value.DescriptorType = assembly.GetType(defPair.Key);
            }
            return assembly;
        }

        /// <summary>
        ///     Serves as the essective method body of a dynamically mapped native class.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="instance">The class instance to get the function from.</param>
        /// <param name="defName">The name of the inheritance level the function was defined at.</param>
        /// <param name="defAccess">The access modifier of the function.</param>
        /// <param name="funcName">The name of the funtion.</param>
        /// <param name="args">The native argument array.</param>
        /// <returns>The converted return value.</returns>
        /// <exception cref="TargetInvocationException">An error occured while calling the function.</exception>
        public static T MethodBody<T>(
            SolClass instance,
            string defName,
            SolAccessModifier defAccess,
            string funcName,
            object[] args)
        {
            try {
                SolValue raw = MethodBodyNoConvert(instance, defName, defAccess, funcName, args);
                return raw.ConvertTo<T>();
            } catch (Exception ex) {
                TargetInvocationException tEs = ex as TargetInvocationException;
                if (tEs != null) {
                    throw tEs;
                }
                string name = instance.Type + "." + funcName;
                if (instance.Type != defName) {
                    name += "#" + defName;
                }
                throw new TargetInvocationException(CompilerResources.Err_DynMapException.FormatWith(name), ex);
            }
        }

        /// <summary>
        ///     Serves as the essective method body of a dynamically mapped native class.
        /// </summary>
        /// <param name="instance">The class instance to get the function from.</param>
        /// <param name="defName">The name of the inheritance level the function was defined at.</param>
        /// <param name="defAccess">The access modifier of the function.</param>
        /// <param name="funcName">The name of the funtion.</param>
        /// <param name="args">The native argument array.</param>
        /// <returns>The raw return value.</returns>
        /// <exception cref="TargetInvocationException">An error occured while calling the function.</exception>
        public static SolValue MethodBodyNoConvert(
            SolClass instance,
            string defName,
            SolAccessModifier defAccess,
            string funcName,
            object[] args)
        {
            try {
                SolValue[] solArgs = SolMarshal.MarshalFromNative(instance.Assembly, args);
                /*SolClass.Inheritance inh = instance.FindInheritance(defName);
                if (inh == null) {
                    throw new SolRuntimeNativeException("Failed to find inheritance level \"" + defName + "\" in class \"" + instance.Type + "\".");
                }
                IVariables vars = inh.GetVariables(defAccess, SolVariableMode.Declarations);*/

                // We are not getting the variables from the inheritance since we wish to be able to user
                // overridden overridden members aswell.
                // The only exception to this are locals which cannot be overridden. But that also means
                // that we should never have local defAccess. But we'll just make sure.
                IVariables vars;
                switch (defAccess) {
                    case SolAccessModifier.Global:
                    case SolAccessModifier.Internal:
                        vars = instance.GetVariables(defAccess, SolVariableMode.All);
                        break;
                    case SolAccessModifier.Local:
                        SolClass.Inheritance inh = instance.FindInheritance(defName);
                        if (inh == null) {
                            throw new SolRuntimeNativeException("Failed to find inheritance level \"" + defName + "\" in class \"" + instance.Type + "\".");
                        }
                        vars = inh.GetVariables(SolAccessModifier.Local, SolVariableMode.Declarations);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(defAccess), defAccess, null);
                }
                SolValue funcRaw = vars.Get(funcName);
                SolFunction func = (SolFunction) funcRaw;
                return func.Call(new SolExecutionContext(instance.Assembly, "Native calling " + func), solArgs);
            } catch (Exception ex) {
                TargetInvocationException tEs = ex as TargetInvocationException;
                if (tEs != null) {
                    throw tEs;
                }
                string name = instance.Type + "." + funcName;
                if (instance.Type != defName) {
                    name += "#" + defName;
                }
                throw new TargetInvocationException(CompilerResources.Err_DynMapException.FormatWith(name), ex);
            }
        }

        #region Nested type: Options

        /// <summary>
        ///     Options realted to the native compiler.
        /// </summary>
        public class Options
        {
            /// <summary>
            ///     Creates new compiler options.
            /// </summary>
            /// <param name="assemblies">The assemblies being referenced by the compiler.</param>
            public Options([NotNull] Assembly[] assemblies)
            {
                Assemblies = assemblies;
            }

            /// <summary>
            ///     The assemblies being referenced by the compiler.
            /// </summary>
            [NotNull]
            public Assembly[] Assemblies { get; }

            /// <summary>
            ///     Should a dll file be created?
            /// </summary>
            public bool CreateAssemblyFile { get; set; } = false;

            /// <summary>
            ///     Should source files be created?
            /// </summary>
            public bool CreateSourceFile { get; set; } = false;

            /// <summary>
            ///     The output file name. Only used if either <see cref="CreateAssemblyFile" /> or <see cref="CreateSourceFile" /> is
            ///     true.
            /// </summary>
            [NotNull]
            public string OutputFileName { get; set; } = "SolScriptMappings.dll";

            /// <summary>
            ///     Should warnings be treated as errors?
            /// </summary>
            public bool WarningsAreErrors { get; set; } = false;
        }

        #endregion
    }
}