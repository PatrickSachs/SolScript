using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;
using JetBrains.Annotations;

namespace SolScript.Interpreter {
    internal static class InternalHelper {
        [ContractAnnotation("null=>null")]
        public static string UnEscape(this string @this) {
            if (string.IsNullOrEmpty(@this)) {
                return @this;
            }
            StringBuilder retVal = new StringBuilder(@this.Length);
            for (int ix = 0; ix < @this.Length;) {
                int jx = @this.IndexOf('\\', ix);
                if (jx < 0 || jx == @this.Length - 1) jx = @this.Length;
                retVal.Append(@this, ix, jx - ix);
                if (jx >= @this.Length) break;
                switch (@this[jx + 1]) {
                    case 'n':
                        retVal.Append('\n');
                        break; // Line feed
                    case 'r':
                        retVal.Append('\r');
                        break; // Carriage return
                    case 't':
                        retVal.Append('\t');
                        break; // Tab
                    case '\\':
                        retVal.Append('\\');
                        break; // Don't escape
                    default: // Unrecognized, copy as-is
                        retVal.Append('\\').Append(@this[jx + 1]);
                        break;
                }
                ix = jx + 2;
            }
            return retVal.ToString();
        }


        [CanBeNull]
        internal static ParseTreeNode FindChildByName(this ParseTreeNodeList @this, string name) {
            return @this.Find(p => p.Term.Name == name);
        }

        [NotNull, DebuggerStepThrough]
        internal static T NotNull<T>([CanBeNull] this T @this, string message = "Unexpected null value!") {
            if (@this == null) {
                throw new NullReferenceException(message);
            }
            return @this;
        }

        internal static MethodInfo GetMethodBfAll(this Type @this, string name) {
            return @this.GetMethod(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }

        /*[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
        internal static SolString S(this string @this) {
            return new SolString(@this);
        }*/
    }
}