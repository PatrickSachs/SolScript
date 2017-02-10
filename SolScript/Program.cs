using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Library.Classes;
using SolScript.Interpreter.Types;
using SolScript.Reader;

namespace SolScript
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Hello:

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" ================================================");
            Console.Write(" ===== ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("SolScript - Command Line Application");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" =====");
            Console.WriteLine(" ================================================");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Welcome to the SolScript command line interpreter.");

            Input:

            Console.WriteLine("The following commands are available:");
            Console.WriteLine("   1: Open the SolScript documentation & specification in your default web-browser.");
            Console.WriteLine("   2: Compile a directory into a SolAssembly.");
            Console.WriteLine("   3: Directly interpret the files in a directory.");
            Console.WriteLine("   4: Interpret an already compiled assembly.");
            Console.WriteLine("Please type the number of the option your wish to choose.");
            Console.Write(" > ");
            string input = Console.ReadLine();
            int option;
            if (!int.TryParse(input, out option)) {
                goto Input;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" ================================================");
            Console.ForegroundColor = ConsoleColor.White;
            switch (option) {
                case 1: {
                    Console.WriteLine("Step 1: Write Stuff");
                    Console.WriteLine("Step 2: Use SolScript");
                    Console.WriteLine("Step 3: Be happy \\(^^)/");
                    Console.WriteLine(
                        "On a more serious note: I haven't written it yet, choose something else. I simply added the option to make the menu look less-poor when presenting it in school. Why am I even writing this?");

                    Console.WriteLine("\n === Ooops ... Press any key to return to the main menu.");
                    Console.ReadKey(true);
                    goto Hello;
                }
                case 2: {
                    ChooseDir:
                    Console.WriteLine("Please enter the input directory (Absolute or relative path to this executable)");
                    Console.Write(" > ");
                    string dirRaw = Console.ReadLine();
                    if (!Directory.Exists(dirRaw)) {
                        Console.WriteLine("This directory does not exist. :( Please try again.");
                        goto ChooseDir;
                    }
                    ChooseFile:
                    Console.WriteLine(
                        "Please enter the file name (without extension) of the assembly. The assembly will be placed in the same directory as this executable.");
                    Console.Write(" > ");
                    string fileRaw = (Console.ReadLine() ?? string.Empty) + ".sol_a1";
                    if (!IsValidFilename(fileRaw)) {
                        Console.WriteLine(
                            "Come on! You are doing this on purpose right? This file name isn't even valid!");
                        goto ChooseFile;
                    }
                    Console.WriteLine("Compiling " + new DirectoryInfo(dirRaw).FullName + " into " +
                                      new FileInfo(fileRaw).FullName + " ...");
                    SolAssemblyReader.ToAssembly(dirRaw, fileRaw);
                    Console.WriteLine("\n === Done ... Press any key to return to the main menu.");
                    Console.ReadKey(true);
                    goto Hello;
                }
                case 3: {
                    ChooseDir:
                    Console.WriteLine(
                        "Please enter the source file directory (Absolute or relative path to this executable)");
                    Console.Write(" > ");
                    string dirRaw = Console.ReadLine();
                    if (!Directory.Exists(dirRaw)) {
                        Console.WriteLine("This directory does not exist. :( Please try again.");
                        goto ChooseDir;
                    }
                    SolAssembly script = SolAssembly.FromDirectory(dirRaw);
                    script.IncludeLibrary(SolLibrary.StandardLibrary).IncludeLibrary(new SolLibrary("test", typeof(MarshalTest).Assembly)).Create();
                    try {
                        script.TypeRegistry.CreateInstance("Main", ClassCreationOptions.Default(), new SolString("Hello from the command line :)"), new SolNumber(42));
                    } catch (SolTypeRegistryException ex) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("   A runtime error occured!");
                        Console.WriteLine(" ================================================");
                        Console.ForegroundColor = ConsoleColor.White;
                            StringBuilder builder = new StringBuilder();
                            SolScriptException.UnwindExceptionStack(ex, builder);
                        Console.WriteLine(builder.ToString());
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" ================================================");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    //try {
                    //Console.WriteLine("Main Class: "+script.Run());
                    /*} catch (SolScriptInterpreterException exception) {
                        Console.WriteLine("An exception occured: " + exception.Message);
                        throw;
                    }*/
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n === Script execution finished ... Press any key to return to the main menu.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.ReadKey(true);
                    goto Hello;
                }
                case 4: {
                    ChooseFile:
                    Console.WriteLine("Please enter the source assembly (Absolute or relative path to this executable)");
                    Console.Write(" > ");
                    string fileRaw = (Console.ReadLine() ?? string.Empty) + ".sol_a1";
                    if (!File.Exists(fileRaw)) {
                        Console.WriteLine("This file does not exist. :( Please try again. (Make sure to NOT specify the file ending, as the file ending must be .sol_a1)");
                        goto ChooseFile;
                    }
                    SolAssembly script = SolAssembly.FromFile(fileRaw).IncludeLibrary(SolLibrary.StandardLibrary).Create();
                    script.TypeRegistry.CreateInstance("Main", ClassCreationOptions.Default(), new SolString("Hello from the command line :)"), new SolNumber(42));
                    Console.WriteLine("\n === Script execution finished ... Press any key to return to the main menu.");
                    Console.ReadKey(true);
                    goto Hello;
                }
                default: {
                    Console.WriteLine("Okay. You were given one simple task. To enter a number between 1 and 4. Does " +
                                      option +
                                      " seem like being in between 1 and 4? No? No. Correct. So go back, and choose a VALID option. Thank you.");
                    Console.WriteLine("\n === Failed ... Press any key to return to the main menu.");
                    Console.ReadKey(true);
                    goto Hello;
                }
            }
        }

        public static bool IsValidFilename(string testName)
        {
            string regexString = "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]";
            Regex containsABadCharacter = new Regex(regexString);

            if (containsABadCharacter.IsMatch(testName)) {
                return false;
            }
            return true;
        }
    }
}