using System;
using OrangeLib;

namespace Orange
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                Info.Help();
                Console.WriteLine("Usage: orange [command] [options]");
                Utils.RunCommandStreamOutput("echo Hey!");
            }   
            else if (args[0] == "--version" || args[0] == "-v")
            {
                Info.Version();
            }
            else
            {
                Console.WriteLine("Unknown command. Use --help to view help information");
            }
        }
    }
}