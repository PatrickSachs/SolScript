using System.Collections.Generic;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Compiler
{
    /// <summary>
    ///     The compiler is used to compile an assembly into quicker to read binary data. The compiled data can then be read by
    ///     SolScript without the need for having to interpret it into a syntax tree first.
    /// </summary>
    public class SolCompiler
    {
        /// <summary>
        ///     Creates a new compiler instance.
        /// </summary>
        /// <param name="assembly">The assembly this compiler is for.</param>
        public SolCompiler(SolAssembly assembly)
        {
            Assembly = assembly;
        }

        /// <summary>
        ///     The assembly this compiler is for.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     Validates a class information to ensure that no duplicate functions or unimplemented abstract functions exist.
        /// </summary>
        /// <param name="definition">The class definition to validate.</param>
        /// <exception cref="SolCompilerException">The class definition is not valid.</exception>
        public void ValidateClass(SolClassDefinition definition)
        {
            Stack<SolClassDefinition> inheritanceChain = definition.GetInheritanceReversed();
            var names = new System.Collections.Generic.Dictionary<string, SolFunctionDefinition>();
            var abstracts = new System.Collections.Generic.Dictionary<string, SolFunctionDefinition>();
            while (inheritanceChain.Count != 0) {
                SolClassDefinition current = inheritanceChain.Pop();
                var thisNames = new HashSet<string>();
                foreach (SolFunctionDefinition function in current.Functions) {
                    ValidateFunction(function);
                    // Every function name may only exist once for each inheritance level.
                    if (thisNames.Contains(function.Name)) {
                        // todo: support function overloading(? desired ?). (possibly declare functions in a higher "unreachable" context and rename them matching to their param names?)
                        throw new SolCompilerException("The function \"" + FuncStr(function) + "\" exists twice within its class. Function overloading is currently not supported.");
                    }
                    thisNames.Add(function.Name);
                    // If another function with the same name already exists we can only
                    // continue if this one overrides the other one.
                    if (names.ContainsKey(function.Name)) {
                        if (function.MemberModifier != SolMemberModifier.Override) {
                            throw new SolCompilerException("The function \"" + FuncStr(function) + "\" hides a member declared at a lower level but does not have the override member modifier.");
                        }
                    }
                    // Ensure overriding is done right.
                    if (function.MemberModifier == SolMemberModifier.Abstract) {
                        abstracts.Add(function.Name, function);
                    } else if (function.MemberModifier == SolMemberModifier.Override) {
                        // A function with the override modifier must have a function in the same
                        // access context to actually override.
                        SolFunctionDefinition overrides;
                        if (!names.TryGetValue(function.Name, out overrides)) {
                            throw new SolCompilerException("The function \"" + FuncStr(function) + "\" tried to override a function that does not exist.");
                        }
                        if (function.AccessModifier != overrides.AccessModifier) {
                            throw new SolCompilerException("The function \"" + FuncStr(function) + "\" tried to override a " + overrides.AccessModifier + " function, but was " +
                                                           function.AccessModifier + " itself. Only functions with the same access modifier can override another.");
                        }
                        abstracts.Remove(function.Name);
                    }
                    // Local names are not relevant for the flat namespace.
                    // Setting the name at the end since we need to access the old function when checking override access.
                    if (function.AccessModifier != SolAccessModifier.Local) {
                        names[function.Name] = function;
                    }
                }
            }
            // Non abstract class need to implement all abstract functions.
            if (definition.TypeMode != SolTypeMode.Abstract && abstracts.Count != 0) {
                throw new SolCompilerException("The non-abstract class \"" + definition.Type + "\" has " + abstracts.Count +
                                               " unimplemented abstract function(s). Non-abstract classes need to implement all abstract functions. Function(s): " +
                                               InternalHelper.JoinToString(", ", abstracts.Values, FuncStr));
            }
        }

        /// <summary>
        ///     Validates a function. Keep in mind that even after calling this function some information may still be incorrect.
        ///     (e.g. this function does not check if an override function overrides an actual function. This check is not in the
        ///     class validation.)
        /// </summary>
        /// <param name="definition">The function definition.</param>
        /// <exception cref="SolCompilerException">The function definition is not valid.</exception>
        public void ValidateFunction(SolFunctionDefinition definition)
        {
            // todo: expand this method to fully validate everything. relocate code from the class check. see summary for more detail.
            if (definition.AccessModifier == SolAccessModifier.Local) {
                // Local function
                if (definition.MemberModifier != SolMemberModifier.None) {
                    throw new SolCompilerException("The function \"" + FuncStr(definition) + "\" has local access and thus cannot have the " + definition.MemberModifier + " member modifier.");
                }
            } else {
                // Internal / Public function
                if (definition.DefinedIn == null) {
                    // Global functions
                    if (definition.MemberModifier != SolMemberModifier.None) {
                        throw new SolCompilerException("The function \"" + FuncStr(definition) + "\" is a global function and thus cannot have the " + definition.MemberModifier + " member modifier.");
                    }
                } else {
                    if (definition.MemberModifier == SolMemberModifier.Abstract && definition.DefinedIn.TypeMode != SolTypeMode.Abstract) {
                        throw new SolCompilerException("The function \"" + FuncStr(definition) +
                                                       "\" is abstract, but was declared in a non abstract class. Only abstract classes can contain abstract functions.");
                    }
                }
            }
        }

        /// <summary>
        ///     <see cref="SolClassDefinition.Type" />.<see cref="SolFunctionDefinition.Name" />
        /// </summary>
        private static string FuncStr(SolFunctionDefinition definition)
        {
            if (definition.DefinedIn != null) {
                return definition.DefinedIn.Type + "." + definition.Name;
            }
            return definition.Name;
        }
    }
}