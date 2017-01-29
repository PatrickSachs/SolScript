using System;

namespace SolScript.Interpreter
{
    public enum AccessModifier
    {
        None = 0,
        Local = 1,
        Internal = 2
    }

    [Flags]
    public enum FunctionalityModifiers
    {
        None = 0,
        Abstract = 1,
        Override = 2
    }
}

/*
abstract class MyClass extends AnotherClass
    local function __new()
        print("Hello Hello!")
    end

    abstract internal function internal_func()
        error("implement me! :(")
    end
end
 */