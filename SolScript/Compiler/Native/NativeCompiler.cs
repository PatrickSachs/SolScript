using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Microsoft.CSharp;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;

namespace SolScript.Compiler.Native
{
    /// <summary>
    /// This class actually compiles native code.
    /// </summary>
    public class NativeCompiler
    {
        public class Context
        {
            public string AssemblyName;
        }

        /*public Assembly CreateAssemblyForClasses(SolAssembly assembly, string fullName)
        {
            AssemblyBuilder builder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(fullName), AssemblyBuilderAccess.Run);
            foreach (SolClassDefinition sCls in assembly.Classes) {
                
            }
        }

        public void CreateNativeClassForSolClass(SolClassDefinition definition)
        {
            // We are overriding the descriptor since we want to keep its signature.
            // And that's fine since the descriptor base methods will inject the values correctly anyway.
            Type descriptorType = definition.DescriptorType;
            if (descriptorType == null) {
                // Well, we could. But we're not going to. It serves no purpose. The reason why we create new
                // override classes for native classes is that we will be able to use the overridden members.
                throw new InvalidOperationException("Cannot create a native class for a SolScript class without descriptor.");
            }
            AssemblyName typeNam = new AssemblyName(descriptorType.FullName + "<solcls_" + definition.Type + ">");
            TypeBuilder typeBuilder = new TypeBuilder();
        }
        */

        public static void CreateNativeClassForSolClass(IEnumerable<SolClassDefinition> definitions, Context ctx)
        {
            var compileUnit = new CodeCompileUnit();
            var csc = new CSharpCodeProvider(new Dictionary<string, string>() {{"CompilerVersion", "v4.0"}});
            var parameters = new CompilerParameters(AppDomain.CurrentDomain.GetAssemblies().Select(s => s.Location).ToArray(), ctx.AssemblyName, false);
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = false;
            parameters.OutputAssembly = "solscript.gen.dll";
            PSDictionary<string, CodeNamespace> namespaces = new PSDictionary<string, CodeNamespace>();
            PSDictionary<string, SolClassDefinition> fullNameToDef = new PSDictionary<string, SolClassDefinition>();
            foreach (SolClassDefinition definition in definitions) {
                var inheritance = definition.GetInheritance().ToList();
                // We are overriding the descriptor since we want to keep its signature.
                // And that's fine since the descriptor base methods will inject the values correctly anyway.
                if (inheritance.Count(i => i.DescriptorType != null) == 0) {
                    // Well, we could. But we're not going to. It serves no purpose. The reason why we create new
                    // override classes for native classes is that we will be able to use the overridden members.
                    throw new InvalidOperationException("Cannot create a native class for " + definition.Type + " due to a missing or ambigous descriptor.");
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
                var classType = new CodeTypeDeclaration(definition.Type);
                ns.Types.Add(classType);
                classType.Attributes = MemberAttributes.Public;
                classType.BaseTypes.Add(descriptorDef.DescriptorType);

                fullNameToDef.Add(ns.Name + "." + classType.Name, definition);
                // INativeClassSelf
                {
                    classType.BaseTypes.Add(typeof(INativeClassSelf));

                    var selfField = new CodeMemberField(typeof(SolClass), nameof(INativeClassSelf.Self) + "__backingfield") {
                        Attributes = MemberAttributes.Private
                    };

                    var selfProp = new CodeMemberProperty {
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
                PSHashSet<string> handledFuncNames = new PSHashSet<string>();
                foreach (SolClassDefinition inhCls in inheritance) {
                    foreach (SolFunctionDefinition function in inhCls.DeclaredFunctions) {
                        if (handledFuncNames.Contains(function.Name)
                            || function.MemberModifier != SolMemberModifier.Override
                            || function.AccessModifier == SolAccessModifier.Local) {
                            continue;
                        }
                        handledFuncNames.Add(function.Name);
                        SolDebug.WriteLine("Chunk func " + function);
                        // Okay, we found a function. Now find the lowest one it overrides.
                        foreach (var inhClsRev in definition.GetInheritanceReversed()) {
                            SolFunctionDefinition overriddenDef;
                            if (inhClsRev.TryGetFunction(function.Name, true, out overriddenDef)) {
                                SolDebug.WriteLine("   Overrides " + overriddenDef);
                                // It was overridden in this class.
                                if (overriddenDef == function) {
                                    throw new InvalidOperationException("An override function cannot override itself. - Invalid internal state.");
                                }
                                // We did override a native method.
                                if (overriddenDef.Chunk.ChunkType != SolChunkWrapper.Type.NativeMethod) {
                                    continue;
                                }
                                MethodInfo overriddenMethod = overriddenDef.Chunk.GetNativeMethod();
                                var method = new CodeMemberMethod();
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
            using (TextWriter writer = File.CreateText(ctx.AssemblyName + ".native.cs")) {
                csc.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions() {});
            }
            var results = csc.CompileAssemblyFromDom(parameters, compileUnit);
            results.Errors.Cast<CompilerError>().ToList().ForEach(error => Debug.WriteLine(error.ToString()));
            foreach (Type type in results.CompiledAssembly.GetTypes()) {
                Debug.WriteLine("final assembly contains: " + type.FullName);
                fullNameToDef[type.FullName].DescriptorType = type;
            }
        }

        public static T MethodBody<T>(
            SolClass instance,
            string defName,
            SolAccessModifier defAccess,
            string funcName,
            object[] args)
        {
            SolValue raw = MethodBodyNoConvert(instance, defName, defAccess, funcName, args);
            return raw.ConvertTo<T>();
        }

        public static SolValue MethodBodyNoConvert(
            SolClass instance,
            string defName,
            SolAccessModifier defAccess,
            string funcName,
            object[] args)
        {
            SolValue[] solArgs = SolMarshal.MarshalFromNative(instance.Assembly, args);
            SolClass.Inheritance inh = instance.FindInheritance(defName);
            IVariables vars = inh.GetVariables(defAccess, SolVariableMode.Declarations);
            SolValue funcRaw = vars.Get(funcName);
            SolFunction func = (SolFunction) funcRaw;
            return func.Call(new SolExecutionContext(instance.Assembly, "Native calling " + func), solArgs);
        }
    }
}