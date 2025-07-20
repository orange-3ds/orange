using OrangeLib.Info;

namespace Orange
{
    public static class Commands
    {
        static public void Add(string[] args)
        {
            // Validate argument count
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: orange add <package>");
                return;
            }

            string packagePath = args[1];

            // Check If file exists
            if (File.Exists(packagePath))
            {
                Console.WriteLine("Error: package file local installing is not supported yet.");
                return;
            }
            else
            {
                try
                {
                    // load package cfg
                    PackageInfo packageloader = new PackageInfo();
                    packageloader.LoadCfg("package.cfg");
                    OrangeLib.Net.Internet.GetPackage(packagePath).GetAwaiter().GetResult();
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
        static public void Build(string[] args)
        {
            // TODO Make build command
            Console.WriteLine("Build command is not yet implemented.");
        }
        static public void Sync(string[] args)
        {
            // TODO Make Sync command
            Console.WriteLine("Sync command is not yet implemented.");
        }
        static class Program
        {
            private const string V = @"Usage: orange [command] [options]
Commands:
    - init (app/package)
    - sync
    - build
    - add (package path) ";
            static readonly string _version = "v0.0.0 Beta";
            static readonly string _help = V;
            static void Main(string[] args)
            {
                if (args.Length == 0 || args[0] == "--help")
                {
                    ShowHelp();
                }
                else if (args[0] == "add")
                {
                    Commands.Add(args);
                }
                else if (args[0] == "build")
                {
                    Commands.Build(args);
                }
                else if (args[0] == "upload")
                {
                    Console.WriteLine("Upload command is not yet implemented.");
                }
                else if (args[0] == "init")
                {
                    HandleInitCommand(args);
                }
                else if (args[0] == "sync")
                {
                    Console.WriteLine("Sync command is not yet implemented.");
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

            private static void ShowHelp()
            {
                Console.WriteLine(_help);
            }

            private static void HandleInitCommand(string[] args)
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: orange init <app/package>");
                    return;
                }
                string type = args[1];
                if (type == "app")
                {
                    Console.WriteLine("App initialization is not yet implemented.");
                }
                else if (type == "package")
                {
                    Console.WriteLine("Package initialization is not yet implemented.");
                }
                else
                {
                    Console.WriteLine("Unknown type. Use 'app' or 'package'.");
                }
            }
        }
    }