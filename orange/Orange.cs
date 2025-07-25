﻿using OrangeLib;
using OrangeLib.Info;
using System.IO.Compression;
using CollinExecute;

namespace Orange
{


    static class Program
    {
        private const string V = @"Usage: orange [command] [options]
Commands:
    - init [app/library]
    - sync
    - build
    - add [library name]
    - stream [3DS Ip address] (-r --retries Number of times to retry the connction)";
        static readonly string _version = "v1.0.2";
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
                Console.WriteLine("Ha! you found a removed command. go to the github to upload a library...");
            }
            else if (args[0] == "stream")
            {
                Stream(args);
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
                Console.WriteLine("Usage: orange add <library>");
                return;
            }

            string libraryPath = args[1];

            // Check If file exists
            if (File.Exists(libraryPath))
            {
                Console.WriteLine("Error: library file local installing is not supported yet.");
                return;
            }
            else
            {
                try
                {
                    // load library cfg
                    libraryInfo libraryloader = new libraryInfo();
                    if (File.Exists("library.cfg"))
                    {
                        libraryloader.LoadCfg("library.cfg");
                    }
                    else
                    {
                        libraryloader.LoadCfg("app.cfg");
                    }

                    OrangeLib.Net.Internet.GetLibrary(libraryPath).GetAwaiter().GetResult();
                    // Add dependency to config
                    if (File.Exists("library.cfg"))
                    {
                        libraryloader.AddDependencyToCfg(libraryPath, "library.cfg");
                    }
                    else
                    {
                        libraryloader.AddDependencyToCfg(libraryPath, "app.cfg");
                    }

                    Console.WriteLine("Successfully added the dependency.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error installing library: {ex.Message}");
                    return;
                }
                Console.WriteLine($"library {libraryPath} installed successfully.");
            }
        }
        static public void Build(string[] args)
        {
            var libraryinfo = new libraryInfo();
            if (File.Exists("library.cfg"))
            {
                Information info = libraryinfo.LoadCfg("library.cfg");
                Library.CreateLibrary(info);
                Console.WriteLine("Successfully built library!");
                return;
            }
            else
            {
                if (!File.Exists("app.cfg"))
                {
                    Console.Error.WriteLine("No app.cfg or library.cfg found. Please run 'orange init (app/library)' to create one.");
                    return;
                }
                Information info = libraryinfo.LoadCfg("app.cfg");
                CollinExecute.Shell.SystemCommand("make clean");
                Directory.CreateDirectory("build");
                File.Copy("app.cfg", "build/app.cfg");
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



        }
        static public void Sync(string[] args)
        {
            // Ensure production URL is used for library downloads
            OrangeLib.Net.Internet.SetWebPath("https://orange.collinsoftware.dev/");
            var libraryinfo = new libraryInfo();
            Information info = libraryinfo.LoadCfg("library.cfg");
            var dependencies = info.Dependencies?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var dep in dependencies)
            {
                OrangeLib.Net.Internet.GetLibrary(dep).GetAwaiter().GetResult();
                Console.WriteLine($"Installed {dep}");
            }
        }
        static public void Stream(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: orange stream [3DS Ip address] (-r --retries Number of times to retry the connction)");
                return;
            }
            string ip = args[1];
            int retries = 1;
            // Parse optional retries argument
            for (int i = 2; i < args.Length; i++)
            {
                if ((args[i] == "-r" || args[i] == "--retries") && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedRetries))
                {
                    retries = parsedRetries;
                    break;
                }
            }
            bool success = OrangeLib.Streaming.Stream3dsxTo3ds(ip, retries);
            if (success)
            {
                Console.WriteLine($"Successfully streamed to 3DS at {ip}.");
            }
            else
            {
                Console.WriteLine($"Failed to stream to 3DS at {ip} after {retries} attempt(s).");
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
                Console.WriteLine("Usage: orange init <app/library>");
                return;
            }
            
            string type = args[1];
            string templateUrl = String.Empty;
            string rootFolder = String.Empty;

            if (type == "library")
            {
                templateUrl = "https://github.com/orange-3ds/3ds-library-template/archive/refs/heads/main.zip";
                rootFolder = "3ds-library-template-main/";
            }
            else if (type == "app")
            {
                templateUrl = "https://github.com/orange-3ds/3ds-app-template/archive/refs/heads/main.zip";
                rootFolder = "3ds-app-template-main/";
            }
            else
            {
                Console.WriteLine("Unknown type. Use 'app' or 'library'.");
                return;
            }

            OrangeLib.Utils.DownloadFileAsync(templateUrl, "3ds-template.zip").Wait();
            ExtractTemplateZip("3ds-template.zip", rootFolder);
            if (File.Exists("3ds-template.zip"))
            {
                File.Delete("3ds-template.zip");
            }
            Console.WriteLine("Extracted template project! Run orange build to build it!");
        }

        private static void ExtractTemplateZip(string zipPath, string rootFolder)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith(rootFolder) && !string.IsNullOrEmpty(entry.Name))
                    {
                        string intendedDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());
                        string destinationPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), entry.FullName.Substring(rootFolder.Length)));
                        
                        if (!destinationPath.StartsWith(intendedDirectory, StringComparison.Ordinal))
                        {
                            throw new IOException($"Entry is outside of the target directory: {entry.FullName}");
                        }
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, true);
                    }
                }
            }
        }
    }
}
