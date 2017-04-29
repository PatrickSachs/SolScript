using SolScript.Interpreter.Library;
using SolScript.Libraries.std;

namespace SolScript.Interpreter.Types.Classes
{
    [SolTypeDescriptor(std.NAME, SolTypeMode.Sealed, typeof(TypeTypeDescriptor)), SolLibraryName(TYPE)]
    public class TypeTypeDescriptor : ANativeTypeDescriptor
    {
        public const string TYPE = "NativeType";
    }
}