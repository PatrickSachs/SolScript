class Main
    local function __new()
        local my_var = new SuperAwesomeClass()
        my_var.class0()
        my_var.class1()
        my_var.class2()
    end
end

class sealed SuperAwesomeClass mixin BaseClass1, BaseClass2
    function class0()
        print("0")
        class1()
        class2()
    end
end

class abstract BaseClass1
    function class1()
        print("1")
    end
end

class abstract BaseClass2
    function class2()
        print("2")
    end
end


