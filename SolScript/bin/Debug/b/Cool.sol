class Main
    local function __new()
        print("Main.__new")
        for local i = 0, i < 10, i = i + 1 do
            if (i == 4) then
                continue
            end
            print(i)
            if (i == 6) then
                break
            end
        end
    end
end

