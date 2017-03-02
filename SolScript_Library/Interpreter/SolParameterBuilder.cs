using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolParameterBuilder" /> is an assembly independent representation of a <see cref="SolParameter" />.
    /// </summary>
    public class SolParameterBuilder
    {
        /// <summary>
        ///     Creates a new parameter builder.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The type builder.</param>
        public SolParameterBuilder(string name, SolTypeBuilder type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        ///     The parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The type builder.
        /// </summary>
        public SolTypeBuilder Type { get; }

        /// <summary>
        ///     Gets an actual parameter for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The parameter.</returns>
        /// <exception cref="SolMarshallingException">Cannot find a <see cref="SolType" /> for this assembly.</exception>
        public SolParameter Get(SolAssembly assembly)
        {
            return new SolParameter(Name, Type.Get(assembly));
        }
    }
}