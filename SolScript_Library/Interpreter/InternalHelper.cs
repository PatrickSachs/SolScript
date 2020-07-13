using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;

namespace SolScript.Interpreter {
    internal static class InternalHelper {
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string JoinToString<T>(string separator, IEnumerable<T> array) {
            return string.Join(separator, array);
        }

        [ContractAnnotation("null=>null")]
        internal static string UnEscape(this string @this) {
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

        [CanBeNull, MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        
        [DebuggerStepThrough]
        internal static Terminators BuildTerminators(bool incReturn, bool incBreak, bool incContinue) {
            Terminators terminators = Terminators.None;
            if (incReturn) terminators |= Terminators.Return;
            if (incBreak) terminators |= Terminators.Break;
            if (incContinue) terminators |= Terminators.Continue;
            return terminators;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidReturn(Terminators terminators) {
            return (terminators & Terminators.Return) == Terminators.Return;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidBreak(Terminators terminators) {
            return (terminators & Terminators.Break) == Terminators.Break;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidContinue(Terminators terminators) {
            return (terminators & Terminators.Continue) == Terminators.Continue;
        }

        [DebuggerStepThrough]
        internal static AccessModifiers BuildModifiers(bool isLocal, bool isInternal, bool isAbstract) {
            AccessModifiers modifiers = AccessModifiers.None;
            if (isLocal) modifiers |= AccessModifiers.Local;
            if (isInternal) modifiers |= AccessModifiers.Internal;
            if (isAbstract) modifiers |= AccessModifiers.Abstract;
            return modifiers;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsLocal(AccessModifiers modifiers) {
            return (modifiers & AccessModifiers.Local) == AccessModifiers.Local;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsInternal(AccessModifiers modifiers) {
            return (modifiers & AccessModifiers.Internal) == AccessModifiers.Internal;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAbstract(AccessModifiers modifiers) {
            return (modifiers & AccessModifiers.Abstract) == AccessModifiers.Abstract;
        }
    }
}