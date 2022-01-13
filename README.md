# SolScript

SolScript is an old scripting language developed by me around 2015-2016. This repository has been recovered from an old backup of mine as it was created during my school time before I used GitHub.

SolScript is object-oriented, optionally type-safe and interpreted. Its syntax is inspired by Lua (mainly), TypeScript, Python and Java (to a lesser extend). My initial idea behind SolScript was to act as a scripting language I could use in games developed by me that has strong .NET interop due to my focus on Unity at the time as well as natively support classes.

## Hello World

The following is a simple "Hello World!" program in SolScript:

```
1> class Main
2>    local function __new()
3>        var my_string: string! = "Hello World!"
4>        print(my_string)
5>    end
6> end
```

SolScript is a scripting language and therefore has no concept of an entry point. However, the built in CLI creates a new instance of the `Main` class in line 1 after parsing the code. The `__new` function serves as the constructor. (The CLI also allow to simply invoke the `main` function)

The `local` keyword in line 2 is the access modifier. `local` is the equivalent of `private` in most languages, `global` is `public` and `internal` corresponds to `protected`.

Line 3 features a variable declaration. Noted here is the type system of SolScript. The data type is suffixed by either a `?`, allowing the variable to be `nil` (SolScripts equivalent of a `NULL` pointer) or suffixed by a `!`, not allowing `nil` to be assigned. This feature by now has also been implemented by C#, but at the time was at least for me a new concept (C# only had support for nullable value types).

**Note:** Further examples can be found in the "Examples" directory. You can download a compiled version of the SolScript interpreter from the Releases page and run them yourselves and play around a bit with the language.

## Running SolScript

To try out SolScript either download a precompiled release or clone the repo and compile it yourself using `dotnet build` (Requires the old .NET SDK 4.6 - SolScript was build using .NET 4.0 and has not been ported to newer .NET versions).

SolScript has a simple CLI application that doesn't really support any command line options, only an interactive mode as I wasn't familiar with console applications at all when I developed the language.

Every menu has a hint text that explains what input is required.

```
================================================
===== SolScript - Command Line Application =====
================================================
Welcome to the SolScript command line interpreter.
The following commands are available:
  1: Open the SolScript documentation & specification in your default web-browser.
  2: Compile a directory into a SolAssembly.
  3: Directly interpret the files in a directory.
  4: Interpret an already compiled assembly.
Please type the number of the option your wish to choose.
>
```

Option 1, 2 and 4 are defunct. The time I stopped developing SolScript I just started laying the ground work of compilation and I realized that the parsing can get quite slow on larger codebases.

So **type 3** and answer the next few prompts:

```
Please type the number of the option your wish to choose.
> 3
================================================
Please enter the source file directory (Absolute or relative path to this executable)
> Examples/01-HelloWorld
Main class("0"/"class") or main function("1"/"function") as entry point?
> 1
main [Main.sol:4:11] : Hello World!
main() returned: nil
```

The input above assumes that SolScript.exe was placed in the root of the project and uses the Example code provided with the repo in the "Examples" directory.

When running code from the CLI you are required to provide the path and if the CLI should create a new instance of the `Main` class or call the  `main` function. You need to check the code of each example to see which one you need to select.

## Parsing

SolScript uses a custom built parser library, ["NodeParser"](https://github.com/PatrickSachs/NodeParser) (explanation video [here](https://www.youtube.com/watch?v=foufYNOaP64)) based on Irony to parse. It allows to declare a grammar with nodes in a EBNF-like syntax, which also support directly transforming the parsed data into the actual data structures required by the language at the same time, making quick iterations possible without breaking other parts of the language.

The main downside is that I never got around to implementing proper syntax errors, meaning any error during parsing is effectively a cryptic mess.

## Data types

SolScript supports different data types:

* `nil` - The absence of a value.
* `bool` - Boolean values.
* `number` - Numeric values (64 Bit floating point).
* `string` - Textual data. Immutable.
* `table` - Associative arrays but also serve as a normal array.
* `function` - Functions are first class types in SolScript allowing references to them and passing them around as values.
* `class` - Classes. Every class in turn creates its own type.
