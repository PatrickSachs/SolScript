/*
 * This example shows user input, string handling and simple conditional logic.
 */
function main()
    var my_name: string! = IO.read("Please enter your name.")
    var my_name_lower: string! = String.to_lower(my_name)
    if my_name_lower == "bob" || my_name_lower == "alice" then
        IO.writeln("Hello, " .. my_name .. ".")
    else
        IO.writeln("You are not Bob or Alice! I don't like you.")
    end
end
