// This example shows number parsing (as well as handling nilable types), control loops and some light math.
function main()
    var input: string! = IO.read("Enter a number:")
    var parsed: number? = String.parse_number(input)
    assert(parsed != nil, "Invalid input!")
    var counter: number! = 0
    while parsed > 0 do
        if parsed % 3 == 0 || parsed % 5 == 0 then
            counter = counter + parsed
        end
        parsed = parsed - 1
    end
    IO.writeln("The sum of all numbers dividable by 3 or 5 in " .. parsed .. " is " .. counter .. ".")
end
