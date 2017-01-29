class Main local function __new() my_function() print("===============================") var stuff : Thing! = new Thing() stuff.say_hello("Generic YouTuber User #3243").say_hello("my friend")
		print("===============================")
		var poor_string : string! = "hello i am a string. please dont kill me i have a family and {0} kids."
		print(String.to_upper(poor_string))
		print(String.format(poor_string, "24"))
		print(String.skip(poor_string, 6)) print(String.join(" || ", poor_string, false, true)) print(String.parse_number("24324"))
		print("===============================")
		var ghost : table! = {}
		ghost.abc = "def"
		Table.append(ghost, "val 1")
		Table.append(ghost, "val 2")
		Table.append(ghost, "val 3")
		Table.append(ghost, "val 4")
		for valuepair in ghost do print(valuepair) end
		print("===============================")
		IO.sol_outln("I dont have an irritating debug info stuff thingy next to me. yay.")
		var input : string! = IO.sol_in("Whats your name?")
		print("i dont like the name " ..  input)
		print(new Thing().get_int())
	end
end

function __main(args : table!)
	for value in args do
		print(value)
	end
end

function my_function()
	print("Hello World")
	var my_var : MyClass! = new SubClass()
	my_var.my_class_function()
	print("===============================")
	var num1 = 43 + 42 / 82 ^ 54 * 851 + 7%8
	var num2 = #"I am a string"
	var num3 = #{
		[0]=1,
		[1]=1324,
		[2]=134
	}
	print(num1)
	print(num2)
	print(num3)
	
	var not_bad : bool! = bad() ?? false
	print(not_bad)
	//var num4 = #my_var
end

function bad() : bool?
	return nil
end

class MyClass
	internal my_table : table! = {
		a_key = 42,
		another_key = function() print("Boo!") end,
		[function() end] = "foo bar"
	}

	//local function __new()
	//	my_table = {
	//		a_key = 42,
	//		another_key = function() print("Boo!") end,
	//		[function() end] = "foo bar"
	//	}
	//end
	
	local function __get_n() : any?
		return 35
	end

	function my_class_function()
		print("Hello, I am class number " .. my_table.a_key .. "!")
		my_table.another_key()
	end
	
	local function a_local()
		print("Someone messed up pretty badly.")
	end
end

class SubClass extends MyClass
	function my_class_function()
		print("Hello, I am SUBCLASS number " .. (my_table.a_key * 7) .. "!")
		my_table.another_key()
		//a_local()
	end
end
