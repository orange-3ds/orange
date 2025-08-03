using OrangeLib;
using OrangeLib.Info;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Orange
{
    static class Program
    {
        private const string V = @"Orange - DevkitPro Library Manager

Usage: orange [command] [options]

Commands:
    init <app|library>    Create a new 3DS app or library project from templates
    sync                  Download and install all dependencies listed in config
    build [cia]           Build the current project (app or library)
                          Optional 'cia' argument builds CIA file after successful build
    add <library_name>    Download and add a library dependency to the project
    stream <ip> [options] Stream built 3DSX file to 3DS console via homebrew launcher
                          Options: -r, --retries <num>  Number of connection retry attempts (default: 1)

Global Options:
    --help, -h           Show this help message
    --version, -v        Show version information

Examples:
    orange init app                     Create a new 3DS application project
    orange init library                 Create a new 3DS library project
    orange sync                         Download all dependencies from current project config
    orange build                        Build the current project to ELF and 3DSX
    orange build cia                    Build the current project and create CIA file
    orange add mylib                    Add 'mylib' library as a dependency
    orange stream 192.168.1.100         Stream to 3DS at IP 192.168.1.100
    orange stream 192.168.1.100 -r 3    Stream with 3 retry attempts

Notes:
    - Ensure your 3DS is running homebrew launcher when using 'stream'
    - CIA building requires makerom, bannertool, and ffmpeg to be in PATH
    - Use 'orange init' in an empty directory to create a new project";
        static readonly string _help = V;
        private const string Version = "v1.1.0"; // Incremented version for refactor

        // Main entry point is now async to allow for top-level await.
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLowerInvariant();
            try
            {
                // Use a cleaner switch statement for command routing.
                switch (command)
                {
                    case "init":
                        await HandleInitCommandAsync(args);
                        break;
                    case "sync":
                        await HandleSyncCommandAsync(args);
                        break;
                    case "build":
                        await HandleBuildCommandAsync(args);
                        break;
                    case "add":
                        await HandleAddCommandAsync(args);
                        break;
                    case "stream":
                        HandleStreamCommand(args);
                        break;
                    case "--version":
                    case "-v":
                        Console.WriteLine($"Orange Version: {Version}");
                        break;
                    case "--help":
                    case "-h":
                        ShowHelp();
                        break;
                    default:
                        LogError($"Unknown command '{command}'. Use --help to view available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"An unexpected error occurred: {ex.Message}");
                // For debugging, you might want to uncomment the line below:
                // Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task HandleBuildCommandAsync(string[] args)
        {
            // 1. Sync dependencies first.
            LogInfo("--- Syncing Dependencies ---");
            await HandleSyncCommandAsync(args);

            // 2. Determine project type and load config.
            var libraryInfo = new libraryInfo();
            string configPath = File.Exists("library.cfg") ? "library.cfg" : "app.cfg";

            if (!File.Exists(configPath))
            {
                LogError("No app.cfg or library.cfg found. Please run 'orange init' first.");
                return;
            }

            Information info = libraryInfo.LoadCfg(configPath);

            // 3. Build the library or application.
            if (configPath == "library.cfg")
            {
                LogInfo("\n--- Building Library ---");
                Library.CreateLibrary(info); // Assuming this is a synchronous method
                LogSuccess("Successfully built library!");
                return;
            }
            
            LogInfo("\n--- Building Application ---");
            await ProcessRunner.RunAsync("make", "clean"); // Clean previous build artifacts.
            
            var (success, _, error) = await ProcessRunner.RunAsync("make", "");
            if (!success)
            {
                LogError($"Build Failed. Error output:\n{error}");
                return;
            }
            LogSuccess("Successfully built application ELF!");

            // 4. Optionally build the CIA.
            if (args.Length > 1 && args[1].ToLowerInvariant() == "cia")
            {
                LogInfo("\n--- Building CIA ---");
                // The CiaBuilder will handle banner/icon creation internally.
                var ciaBuilder = new CiaBuilder(
                    info.Title,
                    info.Description,
                    info.Author,
                    "assets/icon.png",
                    "assets/banner.png",
                    "assets/banner_audio.wav" // Pass the ORIGINAL audio file.
                );
                
                // Use the fully async method from the refactored OrangeLib
                bool ciaSuccess = await ciaBuilder.GenerateCia();
                if (ciaSuccess)
                {
                    LogSuccess($"Successfully built {info.Title}.cia!");
                }
                else
                {
                    LogError("Failed to build CIA file. Check the output from makerom above.");
                }
            }
        }

        private static async Task HandleSyncCommandAsync(string[] args)
        {
            OrangeLib.Net.Internet.SetWebPath("https://orange.collinsoftware.dev/");
            var libraryInfo = new libraryInfo();
            string configPath = File.Exists("library.cfg") ? "library.cfg" : "app.cfg";

            if (!File.Exists(configPath))
            {
                LogError("No app.cfg or library.cfg found. Please run 'orange init' first.");
                return;
            }

            Information info = libraryInfo.LoadCfg(configPath);
            var dependencies = info.Dependencies?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            if (!dependencies.Any())
            {
                LogInfo("No dependencies to sync.");
                return;
            }

            foreach (var dep in dependencies)
            {
                await OrangeLib.Net.Internet.GetLibrary(dep);
                Console.WriteLine($" > Synced {dep}");
            }
        }
        
        private static async Task HandleAddCommandAsync(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: orange add <library_name>");
                return;
            }
            string libraryName = args[1];

            try
            {
                string configPath = File.Exists("library.cfg") ? "library.cfg" : "app.cfg";
                if (!File.Exists(configPath))
                {
                    LogError("No app.cfg or library.cfg found. Please run 'orange init' first.");
                    return;
                }

                var libraryLoader = new libraryInfo();
                libraryLoader.LoadCfg(configPath);

                LogInfo($"Downloading library '{libraryName}'...");
                await OrangeLib.Net.Internet.GetLibrary(libraryName);
                
                libraryLoader.AddDependencyToCfg(libraryName, configPath);
                LogSuccess($"Successfully added '{libraryName}' as a dependency to {configPath}.");
            }
            catch (Exception ex)
            {
                LogError($"Error adding library: {ex.Message}");
            }
        }

        private static void HandleStreamCommand(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: orange stream <3DS_IP_ADDRESS> [--retries <num>]");
                return;
            }
            string ip = args[1];
            int retries = 1;

            for (int i = 2; i < args.Length; i++)
            {
                if ((args[i] == "-r" || args[i] == "--retries") && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedRetries))
                {
                    retries = parsedRetries;
                    break;
                }
            }
            
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.3dsx");
            if (!files.Any())
            {
                LogError("No .3dsx file found in the current directory. Please run 'orange build' first.");
                return;
            }
            if (files.Length > 1)
            {
                LogError("Multiple .3dsx files found. Please specify which one to stream or clean your directory.");
                return;
            }

            bool success = OrangeLib.Streaming.Stream3dsxTo3ds(ip, retries, files[0]);
            if (success)
            {
                LogSuccess($"Successfully streamed to 3DS at {ip}.");
            }
            else
            {
                LogError($"Failed to stream to 3DS at {ip} after {retries} attempt(s).");
            }
        }

        private static async Task HandleInitCommandAsync(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: orange init <app|library>");
                return;
            }

            string type = args[1].ToLowerInvariant();
            string templateUrl, rootFolder;

            switch (type)
            {
                case "library":
                    templateUrl = "https://github.com/orange-3ds/3ds-library-template/archive/refs/heads/main.zip";
                    rootFolder = "3ds-library-template-main/";
                    break;
                case "app":
                    templateUrl = "https://github.com/orange-3ds/3ds-app-template/archive/refs/heads/main.zip";
                    rootFolder = "3ds-app-template-main/";
                    break;
                default:
                    LogError("Unknown type. Use 'app' or 'library'.");
                    return;
            }

            string zipPath = "3ds-template.zip";
            LogInfo($"Downloading template for '{type}'...");
            await OrangeLib.Utils.DownloadFileAsync(templateUrl, zipPath);
            
            LogInfo("Extracting template...");
            ExtractTemplateZip(zipPath, rootFolder);

            File.Delete(zipPath);
            LogSuccess("Project initialized successfully! Run 'orange build' to get started.");
        }

        private static void ExtractTemplateZip(string zipPath, string rootFolder)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            string intendedDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(rootFolder) || string.IsNullOrEmpty(entry.Name)) continue;
                
                string destinationPath = Path.GetFullPath(Path.Combine(intendedDirectory, entry.FullName.Substring(rootFolder.Length)));
                
                // Security check to prevent Zip Slip vulnerability
                if (!destinationPath.StartsWith(intendedDirectory, StringComparison.Ordinal))
                {
                    throw new IOException($"Entry is attempting to extract outside of the target directory: {entry.FullName}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, true);
            }
        }
        
        // --- Helper Methods for Console Output ---
        private static void ShowHelp() => Console.WriteLine(_help);
        private static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        private static void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        private static void LogInfo(string message) => Console.WriteLine(message);
    }
    
    /// <summary>
    /// A robust, self-contained runner for external processes like 'make'.
    /// </summary>
    public static class ProcessRunner
    {
        public static async Task<(bool success, string output, string error)> RunAsync(string command, string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();

                // Asynchronously read the output streams.
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                string output = await outputTask;
                string error = await errorTask;

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Failed to execute '{command}'. Ensure it is in your system's PATH. Details: {ex.Message}");
            }
        }
    }
}