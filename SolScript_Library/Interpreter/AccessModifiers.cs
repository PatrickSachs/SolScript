using System;

namespace SolScript.Interpreter {
    [Flags]
    public enum AccessModifiers {
        None = 0,
        Local = 1,
        Internal = 2,
        Abstract = 4
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