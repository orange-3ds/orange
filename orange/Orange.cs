using OrangeLib;
using OrangeLib.Info;

namespace Orange
{
    static class Program
    {
        static readonly string _version = "v0.0.0 Beta";
        static readonly string _help = @"Usage: orange [command] [options]
Commands:
    - upload
    - init (app/package)
    - sync
    - build
    - add (package path) ";
        static void Main(string[] args)
        {

            if (args.Length == 0 || args[0] == "--help")
            {
                // Write _help text
                Console.WriteLine(_help);
                var packageloader = new PackageInfo();
                // The following code is for testing
                var package = packageloader.LoadCfg("package.cfg");
                Console.WriteLine($"Package Title: {package.Title}");
                Console.WriteLine($"Package Description: {package.Description}");
                Console.WriteLine($"Package Author: {package.Author}");
                Console.WriteLine($"Package Readme Contents: {package.ReadmeContents}");
                OrangeLib.Package.InstallPackage("package.zip");

            }
            else if (args[0] == "--version" || args[0] == "-v")
            {
                Console.WriteLine($"Orange Version: {_version}");
            }
            else
            {
                Console.WriteLine("Unknown command. Use --help to view help information");
            }
        }
    }
}