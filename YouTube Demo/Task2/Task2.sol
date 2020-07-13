class Main
    local function __new()
        local my_name : string! = IO.sol_in("Please enter your name.")
        local my_name_lower : string! = String.to_lower(my_name)
        if my_name_lower == "bob" or my_name_lower == "alice" then
            IO.sol_outln("Hello, " .. my_name .. ".")
        else
            IO.sol_outln("You are not Bob or Alice! I don't like you.")
        end
    end
end
