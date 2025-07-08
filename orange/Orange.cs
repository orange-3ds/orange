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
                Utils.Messages.Help();
                PackageInfo packageloader = new PackageInfo();
                // The following code is for testing
                var package = packageloader.LoadCfg("package.cfg");
                Console.WriteLine($"Package Title: {package.Title}");
                Console.WriteLine($"Package Description: {package.Description}");
                Console.WriteLine($"Package Author: {package.Author}");
                Console.WriteLine($"Package Readme Contents: {package.ReadmeContents}");
            }
            else if (args[0] == "--version" || args[0] == "-v")
            {
                Utils.Messages.Version();
            }
            else
            {
                Console.WriteLine("Unknown command. Use --help to view help information");
            }
        }
    }
}