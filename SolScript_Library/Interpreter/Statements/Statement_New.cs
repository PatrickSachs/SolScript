using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public class Statement_New : SolStatement
    {
        public Statement_New([NotNull] SolAssembly assembly, SolSourceLocation location, string typeName, params SolExpression[] arguments) : base(assembly, location)
        {
            Arguments = arguments;
            TypeName = typeName;
        }

        public readonly SolExpression[] Arguments;

        public readonly string TypeName;

        #region Overrides

        /// <exception cref="SolRuntimeException">An error occured while creating the class instance.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            var arguments = new SolValue[Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                arguments[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            SolClass instance;
            try {
                instance = Assembly.TypeRegistry.CreateInstance(TypeName, ClassCreationOptions.Default, arguments);
            } catch (SolTypeRegistryException ex) {
                throw new SolRuntimeException(context, $"An error occured while creating a class instance of type \"{TypeName}\".", ex);
            }
            return instance;
        }

        protected override string ToString_Impl()
        {
            return
                $"new {TypeName}({InternalHelper.JoinToString(",", Arguments)})";
        }

        #endregion
    }
}