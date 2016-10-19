using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class SolChunk : ICanTerminateParent {
        #region Delegates

        public delegate void ContextEditor(SolExecutionContext context);

        #endregion

        #region ContextMode enum

        public enum ContextMode {
            RunInParameter,
            RunInLocal
        }

        #endregion

        public SolChunk() {
            Id = ++lastId;
        }

        private static readonly SolExpression defaultReturn = new Expression_Nil(default(SourceLocation));

        private static int lastId = -1;
        public readonly int Id;

        private bool m_didTerminate;

        [CanBeNull] public SolExpression ReturnExpression;

        public SolStatement[] Statements;

        #region ICanTerminateParent Members

        public bool DidTerminateParent => ReturnExpression != null || m_didTerminate;

        #endregion

        public override int GetHashCode() {
            return 30 + Id;
        }

        public SolValue Execute(SolExecutionContext context, ContextMode mode = ContextMode.RunInLocal,
            [CanBeNull] ContextEditor editor = null) {
            //SolExecutionContext localContext = mode == ContextMode.RunInLocal ? SolExecutionContext.CreateFrom(context) : context;
            SolExecutionContext localContext = mode == ContextMode.RunInLocal ? new SolExecutionContext(context.Assembly) : context;
            if (mode == ContextMode.RunInLocal) {
                localContext.VariableContext.ParentContext = context.VariableContext;
            }
            //SolDebug.WriteLine("CHUNK ! " + editor);
            if (editor != null) {
                //SolDebug.WriteLine(" aand the editor is there ! " + editor);
                editor(localContext);
            }
            //editor?.Invoke(localContext);

            m_didTerminate = false;
            foreach (SolStatement statement in Statements) {
                //SolDebug.WriteLine("Calling " + statement);
                SolValue value = statement.Execute(localContext);
                if (statement.DidTerminateParent) {
                    m_didTerminate = true;
                    //SolDebug.WriteLine("  ... Manually Terminated");
                    return value;
                }
            }
            if (ReturnExpression != null) {
                //SolDebug.WriteLine("  ... Terminated by return");
                m_didTerminate = true;
                return ReturnExpression.Evaluate(localContext);
            }
            //SolDebug.WriteLine("  ... No termination");
            return defaultReturn.Evaluate(localContext);
        }

        public override string ToString() {
            return
                $"SolChunk(Statements=[{string.Join(",", (IEnumerable<SolStatement>) Statements)}], ReturnExpression={ReturnExpression})";
        }
    }
}