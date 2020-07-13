class Main
    local function __new()
        local input : string! = IO.sol_in("Please enter a sting to convert to pig-latin:")
        local pig = to_pig_latin(input)
        IO.sol_outln(pig)
        IO.sol_outln("Normal:")
        IO.sol_outln(from_pig_latin(pig))
    end
    
    function from_pig_latin(_input : string!) : string!
        local input_words : table! = String.split(_input, " ")
        local latin = ""
        for word in input_words do
            if (#latin) != 0 then
                latin = latin .. " "
            end
            local word_len = #word.value
            local latin_word : string! = ""
            assert(word_len >= 3, word.value .. " is not a pig-latin word!")
            if word_len == 3 then
                latin_word = String.take(word.value, 1)
            else          
                local relevant = String.substring(word.value, 1, (#word.value) - 4)
                local first_old = String.take(word.value, 1)
                local first_new = String.substring(word.value, (#relevant) + 1, 1)
                if String.is_upper(first_old) then
                    first_old = String.to_lower(first_old)
                    first_new = String.to_upper(first_new)
                end
                latin_word = first_new .. first_old .. relevant
            end
            latin = latin .. latin_word
        end
        return latin
    end
    ---A quick brown fox and a naked snake jump over my table
    function to_pig_latin(_input : string!) : string!
        local input_words : table! = String.split(_input, " ")
        local piglatin : string! = ""
        for word in input_words do
            if (#piglatin) != 0 then
                piglatin = piglatin .. " "
            end
            local pig_word : string! = ""
            local word_len = 0 + #word.value - 0
            if word_len == 1 then
                pig_word = word.value .. "ay"
            else
                local first = String.take(word.value, 1)
                if String.is_upper(first) then
                    pig_word = 
                        String.to_upper(String.char_at(word.value, 1)) 
                        .. String.substring(word.value, 2, word_len - 2) 
                        .. String.to_lower(first) 
                        .. "ay"
                else
                    pig_word = 
                        String.substring(word.value, 1, word_len - 1) 
                        .. first 
                        .. "ay"
                end
            end
            piglatin = piglatin .. pig_word
        end
        return piglatin
    end
end
