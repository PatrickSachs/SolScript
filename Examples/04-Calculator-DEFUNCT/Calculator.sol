class Main
    local function __new()
        local input: string! = String.to_lower(IO.sol_in("Choose calculate mode [plus, mult]:"))
        local op = nil
        if input == "plus" then
            op = new CalcPlus()
        elseif input == "mult" then
            op = new CalcMult()
        else
            error("Invalid mode!")
        end
         
        input = String.to_lower(IO.sol_in("Enter a number:"))
        local parsed: number? = String.parse_number(input)
        assert(parsed != nil, "Invalid input!")
        local counter : number! = 0
        for local i: number! = 1, i < parsed, i = i++ do
        
        end
        while parsed > 0 do
            counter = op.process(counter, parsed)
            parsed = parsed - 1
        end
        IO.sol_outln("Done! The number is: " .. counter .. ".")
    end
end 
