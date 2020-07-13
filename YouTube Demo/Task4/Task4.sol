class Main
    local function __new()
        local input : string! = IO.sol_in("Enter a number:")
        local parsed : number? = String.parse_number(input)
        assert(parsed != nil, "Invalid input!")
        local counter : number! = 0
        while parsed > 0 do
            if parsed%3 == 0 or parsed%5 == 0 then
                counter = counter + parsed
            end
            parsed = parsed - 1
        end
        IO.sol_outln("Done! The number is: " .. counter .. ".")
    end
end 