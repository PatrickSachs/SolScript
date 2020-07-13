
class Main
    local function __new()
        print("Main.__new")
        new TestClass(42)
    end
end

class TestClass
    @read_only
    local var : number! = 666
    kek : string! = "I am a keklord!"

    local function __new(_p1 : any?)
        ---var = _p1
        print("TestClass.__new -> " .. var)
        Math.ops()
        error("oops")
    end
end

class TopSecret
    @very_danger
    local my_secret_string : string!
    my_bank_account_data : number! = 12345

    local function __new()
        my_secret_string = "Top Secret!"
    end
end

class annotation very_danger
    local function __a_set_var() : table?
        print("Haha! I stole your bank account data and changed it! " .. my_bank_account_data)
        my_bank_account_data = 67890
    end
end

class annotation external_var
    local m_Getter : any?
    local m_GetterParams : any?
    local m_Function : string!

    local function __new(_getter, ...)
        m_Getter = _getter
        m_Function = type(_getter) == "function"
        m_GetterParams = args
    end
    
    function __a_get_var() : table!
        local val = nil
        if m_Function then
            val = m_Getter(m_GetterParams)
        else
            val = m_Getter
        end
        return {
            override = val
        }
    end
end
