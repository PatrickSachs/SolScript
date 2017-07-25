// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Irony;
using Irony.Parsing;
using NodeParser.Exceptions;
using PSUtility.Enumerables;
using PSUtility.Reflection;
using PSUtility.Strings;
using SolScript.Compiler.Native;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Libraries.lang;
using SolScript.Parser.Nodes;
using SolScript.Utility;
using Resources = SolScript.Properties.Resources;

namespace SolScript.Interpreter
{
    public sealed partial class SolAssembly
    {
        #region Nested type: Builder

        /// <summary>
        ///     The assembly builder is used to create new assemblies.
        /// </summary>
        public class Builder
        {
            /// <summary>
            ///     Creates a new builder instance.
            /// </summary>
            internal Builder()
            {
                m_Libraries.Add(lang.GetLibrary());
            }

            private readonly PSHashSet<SolLibrary> m_Libraries = new PSHashSet<SolLibrary>();
            private readonly PSHashSet<string> m_SrcFileNames = new PSHashSet<string>();
            private readonly PSList<string> m_SrcStrings = new PSList<string>();
            private SolAssembly m_Assembly;

            private SolAssemblyOptions m_Options;

            /// <summary>
            ///     The source files referenced by this builder.
            /// </summary>
            public ReadOnlyHashSet<string> SourceFiles => m_SrcFileNames.AsReadOnly();

            /// <summary>
            ///     The source strings referenced by this builder.
            /// </summary>
            public ReadOnlyList<string> SourceStrings => m_SrcStrings.AsReadOnly();

            /// <summary>
            ///     Includes new source files in this builder.
            /// </summary>
            /// <param name="files">The files.</param>
            /// <returns>The builder.</returns>
            public Builder IncludeSourceFiles(params string[] files)
            {
                m_SrcFileNames.AddRange(files);
                return this;
            }

            /// <summary>
            ///     Includes new source strings in this builder.
            /// </summary>
            /// <param name="strings">The strings.</param>
            /// <returns>The builder.</returns>
            public Builder IncludeSourceStrings(params string[] strings)
            {
                m_SrcStrings.AddRange(strings);
                return this;
            }

            /// <summary>
            ///     Includes new libraries in this builder.
            /// </summary>
            /// <param name="libraries">The libraries.</param>
            /// <returns>The builder.</returns>
            /// <remarks>The <see cref="lang" /> library is included by default.</remarks>
            public Builder IncludeLibraries(params SolLibrary[] libraries)
            {
                m_Libraries.AddRange(libraries);
                return this;
            }

            /// <summary>
            ///     Tries to build the assembly. Check <see cref="Errors" /> for possible errors or warnings.
            /// </summary>
            /// <param name="options">The options for the assembly.</param>
            /// <param name="assembly">The assembly.</param>
            /// <returns>true if the assembly was successfully created, false if not.</returns>
            public bool TryBuild(SolAssemblyOptions options, out SolAssembly assembly)
            {
                m_Options = options;
                CurrentlyParsingThreadStatic = m_Assembly = assembly = new SolAssembly(options);
                if (!TryBuildLibraries()) {
                    goto Fail;
                }
                if (!TryBuildScripts()) {
                    goto Fail;
                }
                if (!TryPostProcessPreValidation()) {
                    goto Fail;
                }
                // todo: --!validate scripts !-- 
                if (options.CreateNativeMapping) {
                    if (!TryCreateNativeMapping()) {
                        goto Fail;
                    }
                }
                if (!TryCreate()) {
                    goto Fail;
                }
                CurrentlyParsingThreadStatic = null;
                if (!CacheGrammar) {
                    s_Grammar = null;
                }
                return true;
                Fail:
                CurrentlyParsingThreadStatic = null;
                if (!CacheGrammar) {
                    s_Grammar = null;
                }
                return false;
            }

            /// <summary>
            ///     Ensures that the given class has a constructor.
            /// </summary>
            /// <param name="definition">The class definition.</param>
            /// <exception cref="InvalidOperationException">Failed to generate a constructor.</exception>
            private void EnforceConstructor(SolClassDefinition definition)
            {
                SolClassDefinition.MetaFunctionLink ctor;
                if (!definition.TryGetMetaFunction(SolMetaFunction.__new, out ctor)) {
                    SolFunctionDefinition ctorDef = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                        Name = SolMetaFunction.__new.Name,
                        Type = SolType.AnyNil,
                        AccessModifier = SolAccessModifier.Internal,
                        MemberModifier = definition.BaseClass != null ? SolMemberModifier.Override : SolMemberModifier.Default,
                        DefinedIn = definition
                    };
                    if (definition.BaseClass == null) {
                        ctorDef.Chunk = SolChunkWrapper.EmptyOf(m_Assembly);
                        ctorDef.ParameterInfo = SolParameterInfo.None;
                    } else {
                        EnforceConstructor(definition.BaseClass);
                        SolClassDefinition.MetaFunctionLink baseCtor;
                        if (!definition.BaseClass.TryGetMetaFunction(SolMetaFunction.__new, out baseCtor)) {
                            throw new InvalidOperationException(Resources.Err_NoClassConstructor.FormatWith(definition.BaseClass.Type));
                        }
                        ctorDef.ParameterInfo = baseCtor.Definition.ParameterInfo;
                        Expression_Literal codeGetCtorLiteral = new Expression_Literal(m_Assembly, SolSourceLocation.Native(), SolString.ValueOf(SolMetaFunction.__new.Name));
                        Statement_Base codeGetBaseCtor = new Statement_Base(m_Assembly, SolSourceLocation.Native(), codeGetCtorLiteral);
                        Expression_Statement codeGetBaseCtorExpr = new Expression_Statement(m_Assembly, SolSourceLocation.Native(), codeGetBaseCtor);
                        SolChunk chunk = new SolChunk(m_Assembly, SolSourceLocation.Native(), new[] {
                            new Statement_CallFunction(m_Assembly, SolSourceLocation.Native(),
                                codeGetBaseCtorExpr,
                                ctorDef.ParameterInfo.Select(p => new Expression_GetVariable(m_Assembly, SolSourceLocation.Native(), new AVariable.Named(p.Name)))
                            )
                        });
                        ctorDef.Chunk = new SolChunkWrapper(chunk);
                    }
                    definition.AssignFunctionDirect(ctorDef);
                    definition.FindAndRegisterMetaFunction(SolMetaFunction.__new);
                }
            }

            private bool TryPostProcessPreValidation()
            {
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    EnforceConstructor(definition);
                    /*SolClassDefinition.MetaFunctionLink ctor;
                    if (!definition.TryGetMetaFunction(SolMetaFunction.__new, out ctor)) {
                        SolFunctionDefinition ctorDef = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                            Name = SolMetaFunction.__new.Name,
                            Type = SolType.AnyNil,
                            AccessModifier = SolAccessModifier.Internal,
                            MemberModifier = definition.BaseClass != null ? SolMemberModifier.Override : SolMemberModifier.Default,
                            DefinedIn = definition
                        };
                        SolClassDefinition.MetaFunctionLink baseCtor;
                        // We may not be able to obain the base ctor it hasnt ben generated yet by this method.
                        // This won't cause problems since an error is created anyway once the pamraters mismatch here.
                        if (definition.BaseClass != null && definition.BaseClass.TryGetMetaFunction(SolMetaFunction.__new, out baseCtor)) {
                            SolChunk chunk = new SolChunk(m_Assembly, SolSourceLocation.Native(), new SolStatement[] {
                                new Statement_CallFunction(m_Assembly, SolSourceLocation.Native(),
                                    new Expression_GetVariable(m_Assembly, SolSourceLocation.Native(), new AVariable.Named(SolMetaFunction.__new.Name)),
                                    ctorDef.ParameterInfo.Select(p => new Expression_GetVariable(m_Assembly, SolSourceLocation.Native(), new AVariable.Named(p.Name))
                                    )
                                )
                            });
                            ctorDef.Chunk = new SolChunkWrapper(chunk);
                            ctorDef.ParameterInfo = baseCtor.Definition.ParameterInfo;
                        } else {
                            ctorDef.Chunk = new SolChunkWrapper(new SolChunk(m_Assembly, SolSourceLocation.Native(), ArrayUtility.Empty<SolStatement>()));
                            ctorDef.ParameterInfo = SolParameterInfo.None;
                        }
                        definition.AssignFunctionDirect(ctorDef);
                        definition.FindAndRegisterMetaFunction(SolMetaFunction.__new);
                    }*/
                }
                return true;
            }

            private bool TryCreateNativeMapping()
            {
                // TODO: it feels kind of hacky to just override the previous values. maybe gen the native mappings in one go with the rest?
                var requiresNativeMapping = new PSList<SolClassDefinition>();
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    //Trace.WriteLine("Considering " + definition + " for mapping...");
                    if (definition.IsNativeClass) {
                        //Trace.WriteLine("   ... no: native class");
                        // Native classes don't need a native binding.
                        continue;
                    }
                    if (definition.BaseClass == null || !definition.BaseClass.IsNativeClass) {
                        //Trace.WriteLine("   ... no: base not native class");
                        // We are not inheriting from a native class.
                        continue;
                    }
                    //Trace.WriteLine("   ... >>> TAKE IT <<<");
                    requiresNativeMapping.Add(definition);
                }
                NativeCompiler.Options options = new NativeCompiler.Options(new PSHashSet<Assembly>(m_Libraries.SelectMany(l => l.Assemblies).Concat(Assembly.GetExecutingAssembly())).ToArray()) {
                    CreateAssemblyFile = m_Options.NativeMappingOutputPath != null,
                    CreateSourceFile = m_Options.NativeMappingOutputPath != null,
                    WarningsAreErrors = m_Options.WarningsAreErrors
                };
                if (m_Options.NativeMappingOutputPath != null) {
                    options.OutputFileName = m_Options.NativeMappingOutputPath;
                }
                NativeCompiler compiler = new NativeCompiler(options);
                try {
                    compiler.CompileNativeClassMapping(requiresNativeMapping);
                } catch (SolCompilerException ex) {
                    m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, Resources.Err_FailedToBuildDynamicMapping, false, ex));
                    return false;
                }
                return true;
            }

            private bool TryCreate()
            {
                SolExecutionContext context = new SolExecutionContext(m_Assembly, m_Assembly.Name + " initialization context");
                m_Assembly.GlobalVariables = new GlobalVariable(m_Assembly);
                m_Assembly.InternalVariables = new InternalVariables(m_Assembly);
                m_Assembly.LocalVariables = new LocalVariables(m_Assembly);

                // ===========================================================================

                // Declare all global functions and fields but do not initialize them since their annotations or field
                // initializer might refer to an undeclared field/function.
                // todo: figure all this out. I dont think there will a proper way to never create a deadlock like situation.
                // A possible solution would be to do it like java and don't let initializers refer to members below them. 
                // Or like C# and only allow constants, but I'd REALLY(I mean really!) like to avoid that.
                // Declare Functions ... (AND ASSIGN!)
                foreach (KeyValuePair<string, SolFunctionDefinition> funcPair in m_Assembly.GlobalFunctionPairs) {
                    //SolDebug.WriteLine("Processing global function " + funcPair.Key + " ...");
                    SolFunctionDefinition funcDefinition = funcPair.Value;
                    IVariables declareInVariables = m_Assembly.GetVariables(funcDefinition.AccessModifier);
                    SolFunction function;
                    switch (funcDefinition.Chunk.ChunkType) {
                        case SolChunkWrapper.Type.ScriptChunk:
                            function = new SolScriptGlobalFunction(funcDefinition);
                            break;
                        case SolChunkWrapper.Type.NativeMethod:
                            function = new SolNativeGlobalFunction(funcDefinition, DynamicReference.NullReference.Instance);
                            break;
                        case SolChunkWrapper.Type.NativeConstructor:
                            m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None,
                                Resources.Err_GlobalFunctionCannotBeNativeConstructor.ToString(funcPair, funcDefinition.Chunk.GetNativeConstructor().DeclaringType)));
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    try {
                        declareInVariables.Declare(funcPair.Key, new SolType(SolFunction.TYPE, false));
                        if (funcDefinition.DeclaredAnnotations.Count > 0) {
                            try {
                                declareInVariables.AssignAnnotations(funcPair.Key, funcDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                            } catch (SolTypeRegistryException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFunctionAnnotations.ToString(funcDefinition.Name),
                                    false, ex));
                                return false;
                            }
                        }
                        declareInVariables.Assign(funcPair.Key, function);
                    } catch (SolVariableException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalFunction.ToString(funcDefinition.Name), false, ex));
                        return false;
                    }
                }
                // Initialize global fields
                foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in m_Assembly.GlobalFieldPairs) {
                    //SolDebug.WriteLine("Processing global field " + fieldPair.Key + " ...");
                    SolFieldDefinition fieldDefinition = fieldPair.Value;
                    IVariables declareInVariables = m_Assembly.GetVariables(fieldDefinition.AccessModifier);
                    switch (fieldDefinition.Initializer.FieldType) {
                        case SolFieldInitializerWrapper.Type.ScriptField:
                            try {
                                declareInVariables.Declare(fieldPair.Key, fieldDefinition.Type);
                                if (fieldDefinition.DeclaredAnnotations.Count > 0) {
                                    try {
                                        declareInVariables.AssignAnnotations(fieldPair.Key, fieldDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                                    } catch (SolVariableException ex) {
                                        m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFieldAnnotations.ToString(fieldDefinition.Name), false, ex));
                                        return false;
                                    }
                                }
                                SolExpression scriptInitializer = fieldDefinition.Initializer.GetScriptField();
                                if (scriptInitializer != null) {
                                    declareInVariables.Assign(fieldPair.Key, scriptInitializer.Evaluate(context, m_Assembly.LocalVariables));
                                }
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalField.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            break;
                        case SolFieldInitializerWrapper.Type.NativeField:
                            FieldOrPropertyInfo nativeField = fieldDefinition.Initializer.GetNativeField();
                            try {
                                declareInVariables.DeclareNative(fieldPair.Key, fieldDefinition.Type, nativeField, DynamicReference.NullReference.Instance);
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalField.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            try {
                                declareInVariables.AssignAnnotations(fieldPair.Key, fieldDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFieldAnnotations.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return true;
            }

            /// <summary>
            ///     Builds all scripts.
            /// </summary>
            /// <returns>true if everything worked as expected, false if an error occured.</returns>
            private bool TryBuildScripts()
            {
                var trees = new PSList<SolNodeRoot>();
                bool hasError = false;
                try {
                    Grammar.BuildGrammar(m_Options.WarningsAreErrors ? GrammarErrorLevel.Warning : GrammarErrorLevel.Error);
                } catch (NodeParserGrammarException ex) {
                    // The grammar failed to build; we need to exit right away.
                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), Resources.Err_FailedToBuildSolScriptGrammar, false, ex));
                    return false;
                }
                // Scan the source strings & files for code.
                for (int i = 0; i < m_SrcStrings.Count; i++) {
                    try {
                        SolNodeRoot tree = (SolNodeRoot) Grammar.Parse(m_SrcStrings[i], "Source:" + i.ToString(CultureInfo.InvariantCulture),
                            m_Options.WarningsAreErrors ? ErrorLevel.Warning : ErrorLevel.Error);
                        trees.Add(tree);
                    } catch (NodeParserParseErrorException ex) {
                        hasError = true;
                        m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, Resources.Err_FailedToParseFile.FormatWith("Source String # " + i), false, ex));
                    }
                }
                foreach (string fileName in m_SrcFileNames) {
                    string text;
                    try {
                        // todo: Parser Read File Encoding
                        text = File.ReadAllText(fileName);
                    } catch (ArgumentException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIsInvalid.ToString(fileName ?? "null"), false, ex));
                        return false;
                    } catch (NotSupportedException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIsInvalid.ToString(fileName ?? "null"), false, ex));
                        return false;
                    } catch (IOException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    } catch (UnauthorizedAccessException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    } catch (SecurityException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    }
                    try {
                        SolNodeRoot tree = Grammar.Parse(text, Path.GetFileName(fileName), m_Options.WarningsAreErrors ? ErrorLevel.Warning : ErrorLevel.Error) as SolNodeRoot;
                        if (tree != null) {
                            trees.Add(tree);
                        }
                    } catch (NodeParserParseErrorException ex) {
                        hasError = true;
                        m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, Resources.Err_FailedToParseFile.FormatWith(fileName), false, ex));
                    }
                }
                // ===========================================================================
                foreach (SolNodeRoot root in trees) {
                    //Console.WriteLine(root.ToTreeString());
                    foreach (SolDefinition definition in root.GetValue()) {
                        SolClassDefinition classDefinition = definition as SolClassDefinition;
                        if (classDefinition != null) {
                            try {
                                m_Assembly.m_ClassDefinitions.Add(classDefinition.Type, classDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(classDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateClass.ToString(classDefinition.Type), false, ex));
                                hasError = true;
                            }
                        }
                        SolFieldDefinition fieldDefinition = definition as SolFieldDefinition;
                        if (fieldDefinition != null) {
                            try {
                                m_Assembly.m_GlobalFields.Add(fieldDefinition.Name, fieldDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateGlobalField.ToString(fieldDefinition.Name), false,
                                    ex));
                                hasError = true;
                            }
                        }
                        SolFunctionDefinition functionDefinition = definition as SolFunctionDefinition;
                        if (functionDefinition != null) {
                            try {
                                m_Assembly.m_GlobalFunctions.Add(functionDefinition.Name, functionDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(functionDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateGlobalFunction.ToString(functionDefinition.Name),
                                    false, ex));
                                hasError = true;
                            }
                        }
                    }
                }

                // And we've done it again! Time to finally move on to validating all of this.
                return !hasError;
            }

            /// <summary>
            ///     Builds all native libraries.
            /// </summary>
            /// <returns>true if everything worked as expected, false if an error occured.</returns>
            private bool TryBuildLibraries()
            {
                var globals = new PSList<Type>();
                // Build the raw definition hulls.
                foreach (SolLibrary library in m_Libraries) {
                    foreach (Assembly libraryAssembly in library.Assemblies) {
                        foreach (Type libraryType in libraryAssembly.GetTypes()) {
                            // Get descriptor
                            SolTypeDescriptorAttribute descriptor = libraryType.GetCustomAttribute<SolTypeDescriptorAttribute>(false);
                            if (descriptor != null && descriptor.LibraryName == library.Name) {
                                // Get name
                                string name = libraryType.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? libraryType.Name;
                                // Create definition object
                                SolClassDefinition definition = new SolClassDefinition(m_Assembly, SolSourceLocation.Native(), true) {
                                    Type = name,
                                    TypeMode = descriptor.TypeMode,
                                    DescribedType = descriptor.Describes,
                                    DescriptorType = libraryType
                                };
                                // Register in ...
                                //   ... descibed type lookup
                                try {
                                    m_Assembly.m_DescribedClasses.Add(definition.DescribedType, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClassDescribed.ToString(definition.DescribedType), false, ex));
                                    return false;
                                }
                                //   ... descriptor type lookup
                                try {
                                    m_Assembly.m_DescriptorClasses.Add(definition.DescriptorType, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClassDescriptor.ToString(definition.DescriptorType), false, ex));
                                    return false;
                                }
                                //   ... class name lookup
                                try {
                                    m_Assembly.m_ClassDefinitions.Add(name, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClass.ToString(name), false, ex));
                                    return false;
                                }
                            }
                            // Get global
                            SolGlobalAttribute global = libraryType.GetCustomAttribute<SolGlobalAttribute>();
                            if (global != null && global.Library == library.Name) {
                                // Globals will be searched later since the type hierarchy needs to be
                                // built in order to determine their return types.
                                // This step here ONLY buids the class hulls so that we have a list of all type names.
                                globals.Add(libraryType);
                            }
                        }
                    }
                }

                //          !! LIBRARIES HAVE NO MORE MEANING PAST THIS POINT !!
                // ===========================================================================

                // Figure out inheritance 
                // Need to be built in a different iteration since not all definitions may be
                // created while trying to access one in the previous.
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    // Set base class
                    Type describedBaseType = definition.DescribedType.BaseType;
                    SolClassDefinition baseClassDefinition;
                    if (describedBaseType != null && m_Assembly.m_DescribedClasses.TryGetValue(describedBaseType, out baseClassDefinition)) {
                        definition.BaseClassReference = new SolClassDefinitionReference(m_Assembly, baseClassDefinition);
                    }
                }
                // ===========================================================================

                // Time to build functions and fields!
                // They need to be built in a different iteration since the marshaller needs
                // to access the inheritance chain in order to determine the sol types.
                // Also we will turn a constructor into a function.
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    // Build native class functions
                    foreach (MethodInfo method in definition.DescriptorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (method.IsSpecialName) {
                            continue;
                        }
                        // Try to build it.
                        Result<SolFunctionDefinition> functionResult = BuildFunction(method);
                        if (!functionResult) {
                            // If it failed WITHOUT an exception it simply means that no script
                            // function can/should be generated.
                            if (functionResult.Exception == null) {
                                continue;
                            }
                            // If it fails WITH an exception it means that an actual error occured
                            // which will be reported back to the user.
                            // Additionally, assembly generation will be aborted.
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeFunction.ToString(method.FullName()),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition function = functionResult.Value;
                        function.DefinedIn = definition;
                        // Guard against duplicate function names.
                        if (definition.DeclaredFunctions.Any(d => d.Name == function.Name)) {
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_DuplicateClassFunction.ToString(definition.Type, function.Name),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        definition.AssignFunctionDirect(function);
                    }
                    // Build native class field. Same procedure as a function.
                    foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(definition.DescriptorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (field.IsSpecialName) {
                            continue;
                        }
                        Result<SolFieldDefinition> fieldResult = BuildField(field);
                        if (!fieldResult) {
                            if (fieldResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeField.ToString(field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        SolFieldDefinition fieldDef = fieldResult.Value;
                        fieldDef.DefinedIn = definition;
                        if (definition.DeclaredFields.Any(d => d.Name == fieldDef.Name)) {
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_DuplicateClassField.ToString(definition.Type, field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        definition.AssignFieldDirect(fieldDef);
                    }
                    bool hasConstructor = false;
                    // Find a class constructor ...
                    foreach (ConstructorInfo constructor in definition.DescriptorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        Result<SolFunctionDefinition> ctorResult = BuildConstructor(constructor);
                        if (!ctorResult) {
                            if (ctorResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeConstructor.ToString(constructor.FullName()),
                                false, ctorResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition ctorDef = ctorResult.Value;
                        ctorDef.DefinedIn = definition;
                        definition.AssignFunctionDirect(ctorDef);
                        hasConstructor = true;
                        break;
                    }
                    // ... or raise an error if none could be found.
                    // todo: allow classes without constructor. Not sure how, but it seems like something which -could- be useful
                    if (!hasConstructor) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(
                            SolSourceLocation.Native(), ErrorId.None,
                            Resources.Err_NoClassConstructor.ToString(definition.Type))
                        );
                        return false;
                    }
                }

                // ===========================================================================

                // Now that the entire class hierarchy has been built we can go ahead and build the globals
                foreach (Type globalType in globals) {
                    foreach (MethodInfo method in globalType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (method.IsSpecialName) {
                            continue;
                        }
                        Result<SolFunctionDefinition> functionResult = BuildFunction(method);
                        if (!functionResult) {
                            if (functionResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeFunction.ToString(method.FullName()),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition function = functionResult.Value;
                        m_Assembly.m_GlobalFunctions.Add(function.Name, function);
                    }
                    foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(globalType, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (field.IsSpecialName) {
                            continue;
                        }
                        Result<SolFieldDefinition> fieldResult = BuildField(field);
                        if (!fieldResult) {
                            if (fieldResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeField.ToString(field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        SolFieldDefinition fieldDef = fieldResult.Value;
                        m_Assembly.m_GlobalFields.Add(fieldDef.Name, fieldDef);
                    }
                }

                // ===========================================================================

                // Okay - We made it! 
                // The assembly is built. How will be epic adventure of assembly creation continue?
                // Find out right after this advertisement!
                // On a more serious note: Now that we have the native stuff let's parse the user scripts.
                return true;
            }

            /// <summary>
            ///     Tries to build a field definition from the given field/property wrapper.
            /// </summary>
            /// <param name="field">The field to build the ... other field from.</param>
            /// <returns>
            ///     a - Nothing if the creation failed gracefully. Execution can be continued.<br />b - An exception if a creation
            ///     failed
            ///     critically. Throw/Log an error.<br />c - The field if the creation succeeded. The field itself will be a global
            ///     function. Assign <see cref="SolFieldDefinition.DefinedIn" /> to make it a class field.
            /// </returns>
            private Result<SolFieldDefinition> BuildField(FieldOrPropertyInfo field)
            {
                SolLibraryVisibilityAttribute visibility = field.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (visibility != null && !visibility.Visible || visibility == null && !field.IsPublic) {
                    return Result<SolFieldDefinition>.Failure();
                }
                NativeFieldPostProcessor postProcessor = m_Options.GetPostProcessor(field);
                if (postProcessor.DoesFailCreation(field)) {
                    return Result<SolFieldDefinition>.Failure();
                }
                string name = postProcessor.GetName(field);
                SolAccessModifier access = postProcessor.GetAccessModifier(field);
                SolType? remappedType = postProcessor.GetFieldType(field);
                if (remappedType == null) {
                    try {
                        remappedType = SolMarshal.GetSolType(m_Assembly, field.DataType);
                    } catch (SolMarshallingException ex) {
                        return Result<SolFieldDefinition>.Failure(ex);
                    }
                }
                SolFieldDefinition solfield = new SolFieldDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = name,
                    AccessModifier = access,
                    Type = remappedType.Value,
                    Initializer = new SolFieldInitializerWrapper(field)
                };
                // todo: Native Annotations for Fields
                return Result<SolFieldDefinition>.Success(solfield);
            }

            private Result<SolFunctionDefinition> BuildConstructor(ConstructorInfo constructor)
            {
                // todo: flesh out ctors as well as functions
                // todo: annotations   
                SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (!(visibility?.Visible ?? constructor.IsPublic)) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                SolAccessModifier accessModifier = constructor.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? SolAccessModifier.Internal;
                SolParameterInfo.Native parameterInfo;
                try {
                    parameterInfo = InternalHelper.GetParameterInfo(m_Assembly, constructor.GetParameters());
                } catch (SolMarshallingException ex) {
                    return Result<SolFunctionDefinition>.Failure(ex);
                }
                SolFunctionDefinition solctor = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = SolMetaFunction.__new.Name,
                    AccessModifier = accessModifier,
                    Type = new SolType(SolNil.TYPE, true),
                    Chunk = new SolChunkWrapper(constructor),
                    ParameterInfo = parameterInfo
                };
                return Result<SolFunctionDefinition>.Success(solctor);
            }

            /// <summary>
            ///     Tries to build a function definition from the given method info.
            /// </summary>
            /// <param name="method">The method to build the function from.</param>
            /// <returns>
            ///     a - Nothing if the creation failed gracefully. Execution can be continued.<br />b - An exception if a creation
            ///     failed
            ///     critically. Throw/Log an error.<br />c - The function if the creation succeeded. The function itself will be a
            ///     global
            ///     function. Assign <see cref="SolFunctionDefinition.DefinedIn" /> to make it a class function.
            /// </returns>
            private Result<SolFunctionDefinition> BuildFunction(MethodInfo method)
            {
                SolLibraryVisibilityAttribute visibility = method.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (visibility != null && !visibility.Visible || visibility == null && !method.IsPublic) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                NativeMethodPostProcessor postProcessor = m_Options.GetPostProcessor(method);
                if (postProcessor.DoesFailCreation(method)) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                string name = postProcessor.GetName(method);
                SolAccessModifier access = postProcessor.GetAccessModifier(method);
                SolMemberModifier memberModifier;
                if (method.IsAbstract) {
                    // Abstract methods are abstract fields. No additional checks required since they cannot override.
                    memberModifier = SolMemberModifier.Abstract;
                } else if (!method.IsOverride() || !m_Assembly.m_DescriptorClasses.ContainsKey(method.GetBaseDefinition().DeclaringType.NotNull())) {
                    // If a method is not overriding anything it is a normal function in sol script.
                    // If a method is overriding something but the overridden method's declaring class is not known to SolScript we will ignore
                    // the override keyword.
                    // todo "new" keyword on native methods might fail.
                    memberModifier = SolMemberModifier.Default;
                } else {
                    // If a method overrides something and the overridden method's class is exposed to SolScript the method if an override function.
                    memberModifier = SolMemberModifier.Override;
                }
                SolType? remappedReturn = postProcessor.GetReturn(method);
                if (remappedReturn == null) {
                    try {
                        remappedReturn = SolMarshal.GetSolType(m_Assembly, method.ReturnType);
                    } catch (SolMarshallingException ex) {
                        return Result<SolFunctionDefinition>.Failure(ex);
                    }
                }
                SolParameterInfo.Native parmeterInfo;
                try {
                    parmeterInfo = InternalHelper.GetParameterInfo(m_Assembly, method.GetParameters());
                } catch (SolMarshallingException ex) {
                    return Result<SolFunctionDefinition>.Failure(ex);
                }
                SolFunctionDefinition function = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = name,
                    AccessModifier = access,
                    Type = remappedReturn.Value,
                    Chunk = new SolChunkWrapper(method),
                    MemberModifier = memberModifier,
                    ParameterInfo = parmeterInfo
                };
                // todo: Native Annotations for Methods
                return Result<SolFunctionDefinition>.Success(function);
            }
        }

        #endregion
    }
}