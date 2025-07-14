using OrangeLib.Info;

namespace Orange
{
    public static class Commands
    {
        static public void add(string[] args)
        {
            // Validate argument count
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: orange add <package path>");
                return;
            }

            string packagePath = args[1];

            // Check If file exists
            if (!File.Exists(packagePath))
            {
                Console.WriteLine("Error: Package File does not exist.");
                return;
            }
            else
            {
                try
                {
                    // load package cfg
                    PackageInfo packageloader = new PackageInfo();
                    packageloader.LoadCfg("package.cfg");
                    OrangeLib.Package.InstallPackage(packagePath);
                    // Add dependency to config
                    packageloader.AddDependencyToCfg(packagePath, "package.cfg");
                    Console.WriteLine("Sucessfully added the dependency.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error installing package: {ex.Message}");
                    return;
                }
                Console.WriteLine($"Package {packagePath} installed successfully.");
            }
        }
    }
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
            }
            else if (args.Length > 0 && args[0] == "add")
            {
                Commands.add(args);
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