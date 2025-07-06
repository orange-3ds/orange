using OrangeLib;
using OrangeInfoLib;
namespace Orange
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                Info.Help();
                ProjectInfo projectInfo = new ProjectInfo();
                var info = projectInfo.LoadCfg("orange.cfg");
                Console.WriteLine($"App Title: {info.AppTitle}");
                Console.WriteLine($"App Description: {info.AppDescription}");
                Console.WriteLine($"App Author: {info.AppAuthor}");
                Console.WriteLine($"Dependencies: {info.Dependencies}");
                Compile.GccCompile("orange.c");
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