using OrangeLib;
using OrangeLib.Info;

namespace Orange
{


    static class Program
    {
        private const string V = @"Usage: orange [command] [options]
Commands:
    - init (app/package)
    - sync
    - build
    - add (package path) ";
        static readonly string _version = "v1.0.0";
        static readonly string _help = V;
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                ShowHelp();
            }
            else if (args[0] == "add")
            {
                Add(args);
            }
            else if (args[0] == "build")
            {
                Build(args);
            }
            else if (args[0] == "upload")
            {
                Console.WriteLine("Ha! you found a removed command. go to the github to upload a package...");
            }
            else if (args[0] == "init")
            {
                HandleInitCommand(args);
            }
            else if (args[0] == "sync")
            {
                Sync(args);
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
            var packageinfo = new PackageInfo();
            if (File.Exists("package.cfg"))
            {
                Information info = packageinfo.LoadCfg("package.cfg");
                Package.CreatePackage(info);
                Console.WriteLine("Successfully built package!");
                return;
            }
            else
            {
                Information info = packageinfo.LoadCfg("app.cfg");
                CollinExecute.Shell.SystemCommand("make clean");
                bool success = CollinExecute.Shell.SystemCommand("make");
                if (!success)
                {
                    Console.Error.WriteLine("Build Failed.");
                    return;
                }
                else
                {
                    Console.WriteLine("Successfully built app!");
                }
            }

            Console.WriteLine("Package build completed successfully.");


        }
        static public void Sync(string[] args)
        {
            // Ensure production URL is used for package downloads
            OrangeLib.Net.Internet.SetWebPath("https://orange.collinsoftware.dev/");
            var packageinfo = new PackageInfo();
            Information info = packageinfo.LoadCfg("package.cfg");
            var dependencies = info.Dependencies?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var dep in dependencies)
            {
                OrangeLib.Net.Internet.GetPackage(dep).GetAwaiter().GetResult();
                Console.WriteLine($"Installed {dep}");
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
                var packageconfigtext = @"[info]
Title: 3dslib
Description: 3ds library template.
Author: Zachary Jones
README: README.md

[dependencies]

";
                File.WriteAllLines("package.cfg", packageconfigtext);
            }
            else
            {
                Console.WriteLine("Unknown type. Use 'app' or 'package'.");
            }
        }
    }
}
