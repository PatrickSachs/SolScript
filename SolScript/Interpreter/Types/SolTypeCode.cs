namespace SolScript.Interpreter.Types
{
    internal enum SolTypeCode : byte
    {
        Nil = 0,
        Bool = 1,
        Number = 2,
        String = 3,
        Table = 4,
        Function = 5,
        Class = 6
    }
}