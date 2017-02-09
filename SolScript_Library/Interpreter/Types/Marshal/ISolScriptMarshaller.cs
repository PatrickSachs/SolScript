namespace SolScript.Interpreter.Types.Marshal
{
    public interface ISolScriptMarshaller
    {
        int Priority { get; }
        bool DoesHandle(SolAssembly assembly, SolType type);
        object Marshal(SolAssembly assembly, SolValue value, SolType type);
    }
}