using System;
using System.Linq;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library {
    [SolLibraryGlobals("std")]
    public static class GlobalsModule {
        // ReSharper disable InconsistentNaming
        [UsedImplicitly]
        public static SolBoolean @true {
            get { return SolBoolean.True; }
            set { throw new SolScriptInterpreterException("Cannot change the value of 'true'."); }
        }

        [UsedImplicitly]
        public static SolBoolean @false {
            get { return SolBoolean.False; }
            set { throw new SolScriptInterpreterException("Cannot change the value of 'false'."); }
        }

        [UsedImplicitly]
        public static SolNil nil {
            get { return SolNil.Instance; }
            set { throw new SolScriptInterpreterException("Cannot change the value of 'nil'."); }
        }

        [UsedImplicitly]
        public static void error(SolExecutionContext context, string message) {
            throw new SolScriptInterpreterException(context.CurrentLocation + " : " + message);
        }

        [UsedImplicitly]
        public static void print(SolExecutionContext context, params SolValue[] values) {
            Console.WriteLine(context.CurrentLocation + " : " + string.Join(", ", (object[]) values));
        }

        [UsedImplicitly]
        public static string type(SolValue value) {
            return value.Type;
        }

        // ReSharper disable once UnusedParameter.Global
        [UsedImplicitly]
        public static bool assert(SolExecutionContext context, SolValue value, string message = "Assertion failed!") {
            if (value.Equals(SolNil.Instance) || value.Equals(SolBoolean.False)) {
                throw new SolScriptInterpreterException(context.CurrentLocation + " : " + message);
            }
            return true;
        }

        [UsedImplicitly]
        public static SolValue @default(SolExecutionContext context, string type, params SolValue[] ctorArgs) {
            char lastChar = type[type.Length - 1];
            if (lastChar == '?') {
                return SolNil.Instance;
            }
            if (lastChar == '!') {
                type = type.Substring(0, type.Length - 1);
            }
            switch (type) {
                case "nil": {
                    return SolNil.Instance;
                }
                case "bool": {
                    return SolBoolean.False;
                }
                case "number": {
                    return new SolNumber(0);
                }
                case "table": {
                    return new SolTable();
                }
                case "string": {
                    return SolString.Empty;
                }
                default: {
                    return context.Assembly.TypeRegistry.CreateInstance(context.Assembly, type, ctorArgs);
                }
            }
        }

        [UsedImplicitly]
        public static void dbg_dumpvm(SolExecutionContext context) {
            try {
                Console.WriteLine(context.Assembly.TypeRegistry.Types["Main"].Functions.First(f => f.Name == "_new").Creator1.ToString());
            } catch (Exception ex) {
                Console.WriteLine("A fatal error occured while trying to dump the VM ... Aborting and returning to script.\n\n" + ex.Message);
                throw;
            }
        }

        // ReSharper restore InconsistentNaming
    }
}