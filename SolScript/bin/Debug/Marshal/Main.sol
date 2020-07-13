class Main
    @getter @setter
    local str : string! = "kekqueen the first"

    local function __new()
        --[[print("boo!")
        local fn : FunctionInfo = Reflect.get_function("TestClass", "test_func")
        local tc = new TestClass()
        fn.call(tc)
        
        local fn2 = new FunctionInfo("TestClass", "test_func")
        fn.call(tc)
        
        local ff = Reflect.get_field(tc, "counter")
        print("counter type: " .. type(tc.counter))
        print(ff.get(tc))
        ff.set(tc, 666)
        print(tc.counter)]]
        
        local var : string! = my_func() ?? "boo!"
        local rval : string? = my_func()
        local var2 : string! = if rval != nil then return rval else return "boo!" end
       
        print(get_str())
        set_str("keklord the second")
        print(get_str())
    end
    
    local function my_func() : string?
        return nil
    end
end 

class annotation getter
    local function do_get() : any?
        return _a_target_class[_a_target_field]
    end

    local function __new(name : string?)
        Emit.insert_function(_a_target_class, name ?? ("get_" .. _a_target_field), do_get, false)
    end
end 

class annotation setter
    local function do_set(value : any?) : any?
        _a_target_class[_a_target_field] = value
        return _a_target_class[_a_target_field]
    end

    local function __new(name : string?)
        Emit.insert_function(_a_target_class, name ?? ("set_" .. _a_target_field), do_set, false)
    end
end 