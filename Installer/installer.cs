using System;
using System.IO;
using System.Runtime.InteropServices;
using OrangeLib;

namespace Installer
{
    static class Program
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        
        private const string OrangeName = "orange";
        private const string Version = "0.1.0";

        static void Main(string[] args)
        {
            Console.WriteLine($"Orange Package Manager Installer v{Version}");
            Console.WriteLine("==========================================");
            
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                ShowHelp();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--uninstall" || args[0] == "-u"))
            {
                UninstallOrange();
                return;
            }

            InstallOrange();
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: Installer [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h        Show this help message");
            Console.WriteLine("  --uninstall, -u   Uninstall Orange");
            Console.WriteLine();
            Console.WriteLine("Default behavior (no options): Install Orange");
        }

        static void InstallOrange()
        {
            try
            {
                Console.WriteLine("Starting Orange installation...");
                
                // Get current executable directory
                string? currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(currentDir))
                {
                    Console.Error.WriteLine("Error: Could not determine current directory.");
                    Environment.Exit(1);
                    return;
                }

                // Find the Orange executable
                string orangeExePath = FindOrangeExecutable(currentDir);
                if (string.IsNullOrEmpty(orangeExePath))
                {
                    Console.Error.WriteLine("Error: Orange executable not found. Please ensure orange.exe (Windows) or orange binary is available.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"Found Orange executable: {orangeExePath}");

                // Get installation directory
                string installDir = GetInstallDirectory();
                Console.WriteLine($"Installing to: {installDir}");

                // Create installation directory if it doesn't exist
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                    Console.WriteLine("Created installation directory.");
                }

                // Copy Orange executable to installation directory
                string targetExePath = Path.Combine(installDir, Path.GetFileName(orangeExePath));
                File.Copy(orangeExePath, targetExePath, true);
                Console.WriteLine("Copied Orange executable.");

                // Copy dependencies if they exist
                CopyDependencies(currentDir, installDir);

                // Make executable on Unix systems
                if (!IsWindows())
                {
                    MakeExecutable(targetExePath);
                }

                // Add to PATH
                AddToPath(installDir);

                Console.WriteLine();
                Console.WriteLine("✓ Orange has been installed successfully!");
                Console.WriteLine($"✓ Installed to: {installDir}");
                Console.WriteLine("✓ Added to system PATH");
                Console.WriteLine();
                Console.WriteLine("You can now use 'orange' command from anywhere in your terminal.");
                Console.WriteLine("Try: orange --help");
                
                if (!IsWindows())
                {
                    Console.WriteLine();
                    Console.WriteLine("Note: You may need to restart your terminal or run 'source ~/.bashrc' (Linux) or 'source ~/.zshrc' (macOS) for the PATH changes to take effect.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Installation failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void UninstallOrange()
        {
            try
            {
                Console.WriteLine("Starting Orange uninstallation...");
                
                string installDir = GetInstallDirectory();
                
                if (Directory.Exists(installDir))
                {
                    // Remove Orange files
                    string orangeExePath = Path.Combine(installDir, IsWindows() ? "orange.exe" : "orange");
                    if (File.Exists(orangeExePath))
                    {
                        File.Delete(orangeExePath);
                        Console.WriteLine("Removed Orange executable.");
                    }

                    // Remove dependencies
                    RemoveDependencies(installDir);

                    // Try to remove directory if empty
                    try
                    {
                        if (Directory.GetFiles(installDir).Length == 0 && Directory.GetDirectories(installDir).Length == 0)
                        {
                            Directory.Delete(installDir);
                            Console.WriteLine("Removed installation directory.");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Installation directory not empty, leaving it in place.");
                    }
                }

                // Remove from PATH
                RemoveFromPath(installDir);

                Console.WriteLine();
                Console.WriteLine("✓ Orange has been uninstalled successfully!");
                
                if (!IsWindows())
                {
                    Console.WriteLine("Note: You may need to restart your terminal for the PATH changes to take effect.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Uninstallation failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static string FindOrangeExecutable(string currentDir)
        {
            string[] possiblePaths = {
                Path.Combine(currentDir, "..", "orange", "bin", "Debug", "net8.0", IsWindows() ? "orange.exe" : "orange"),
                Path.Combine(currentDir, "..", "orange", "bin", "Release", "net8.0", IsWindows() ? "orange.exe" : "orange"),
                Path.Combine(currentDir, IsWindows() ? "orange.exe" : "orange"),
                Path.Combine(currentDir, "..", IsWindows() ? "orange.exe" : "orange")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        static string GetInstallDirectory()
        {
            if (IsWindows())
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                return Path.Combine(programFiles, "Orange");
            }
            else
            {
                return "/usr/local/bin";
            }
        }

        static void CopyDependencies(string sourceDir, string targetDir)
        {
            // Copy .dll files on Windows, .so files on Linux, .dylib files on macOS
            string[] extensions = IsWindows() ? new[] { "*.dll" } : 
                                 IsLinux() ? new[] { "*.so", "*.so.*" } : 
                                 new[] { "*.dylib" };

            foreach (string extension in extensions)
            {
                string[] files = Directory.GetFiles(sourceDir, extension);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileName.StartsWith("orange")) // Don't copy the main executable again
                    {
                        string targetFile = Path.Combine(targetDir, fileName);
                        File.Copy(file, targetFile, true);
                        Console.WriteLine($"Copied dependency: {fileName}");
                    }
                }
            }

            // Copy related directories if they exist
            string[] dirsToCheck = { "runtimes", "refs" };
            foreach (string dirName in dirsToCheck)
            {
                string sourceSubDir = Path.Combine(sourceDir, dirName);
                if (Directory.Exists(sourceSubDir))
                {
                    string targetSubDir = Path.Combine(targetDir, dirName);
                    Utils.CopyDirectoryRecursively(sourceSubDir, targetSubDir);
                    Console.WriteLine($"Copied directory: {dirName}");
                }
            }
        }

        static void RemoveDependencies(string installDir)
        {
            // Remove .dll files on Windows, .so files on Linux, .dylib files on macOS
            string[] extensions = IsWindows() ? new[] { "*.dll" } : 
                                 IsLinux() ? new[] { "*.so", "*.so.*" } : 
                                 new[] { "*.dylib" };

            foreach (string extension in extensions)
            {
                string[] files = Directory.GetFiles(installDir, extension);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }

            // Remove related directories
            string[] dirsToRemove = { "runtimes", "refs" };
            foreach (string dirName in dirsToRemove)
            {
                string dirPath = Path.Combine(installDir, dirName);
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
            }
        }

        static void MakeExecutable(string filePath)
        {
            // Use chmod to make file executable on Unix systems
            string chmodCommand = $"chmod +x \"{filePath}\"";
            bool success = Utils.ExecuteShellCommand(chmodCommand);
            if (success)
            {
                Console.WriteLine("Made Orange executable.");
            }
            else
            {
                Console.WriteLine("Warning: Could not make Orange executable. You may need to run 'chmod +x' manually.");
            }
        }

        static void AddToPath(string directory)
        {
            if (IsWindows())
            {
                AddToWindowsPath(directory);
            }
            else
            {
                AddToUnixPath(directory);
            }
        }

        static void AddToWindowsPath(string directory)
        {
            try
            {
                // Use PowerShell to add to user PATH
                string command = $"powershell -Command \"$env:PATH += ';{directory}'; [Environment]::SetEnvironmentVariable('PATH', $env:PATH, 'User')\"";
                bool success = Utils.ExecuteShellCommand(command);
                if (success)
                {
                    Console.WriteLine("Added to Windows PATH.");
                }
                else
                {
                    Console.WriteLine("Warning: Could not automatically add to PATH. Please add manually.");
                    Console.WriteLine($"Add this directory to your PATH: {directory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not add to PATH: {ex.Message}");
                Console.WriteLine($"Please manually add this directory to your PATH: {directory}");
            }
        }

        static void AddToUnixPath(string directory)
        {
            try
            {
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                // Determine shell config file
                string[] shellConfigs = { ".zshrc", ".bashrc", ".bash_profile", ".profile" };
                string configFile = "";
                
                foreach (string config in shellConfigs)
                {
                    string configPath = Path.Combine(homeDir, config);
                    if (File.Exists(configPath))
                    {
                        configFile = configPath;
                        break;
                    }
                }

                // If no config file exists, create .bashrc
                if (string.IsNullOrEmpty(configFile))
                {
                    configFile = Path.Combine(homeDir, ".bashrc");
                }

                // Add PATH export to shell config
                string pathLine = $"export PATH=\"$PATH:{directory}\"";
                
                // Check if already added
                if (File.Exists(configFile))
                {
                    string content = File.ReadAllText(configFile);
                    if (content.Contains(pathLine) || content.Contains(directory))
                    {
                        Console.WriteLine("PATH already contains Orange directory.");
                        return;
                    }
                }

                // Append to config file
                using (StreamWriter writer = File.AppendText(configFile))
                {
                    writer.WriteLine();
                    writer.WriteLine("# Added by Orange installer");
                    writer.WriteLine(pathLine);
                }
                
                Console.WriteLine($"Added to PATH via {Path.GetFileName(configFile)}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not add to PATH: {ex.Message}");
                Console.WriteLine($"Please manually add this directory to your PATH: {directory}");
            }
        }

        static void RemoveFromPath(string directory)
        {
            if (IsWindows())
            {
                RemoveFromWindowsPath(directory);
            }
            else
            {
                RemoveFromUnixPath(directory);
            }
        }

        static void RemoveFromWindowsPath(string directory)
        {
            try
            {
                // Use PowerShell to remove from user PATH
                string command = $"powershell -Command \"$path = [Environment]::GetEnvironmentVariable('PATH', 'User'); $newPath = $path -replace [regex]::Escape(';{directory}'), ''; [Environment]::SetEnvironmentVariable('PATH', $newPath, 'User')\"";
                bool success = Utils.ExecuteShellCommand(command);
                if (success)
                {
                    Console.WriteLine("Removed from Windows PATH.");
                }
                else
                {
                    Console.WriteLine("Warning: Could not automatically remove from PATH. Please remove manually.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not remove from PATH: {ex.Message}");
            }
        }

        static void RemoveFromUnixPath(string directory)
        {
            try
            {
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] shellConfigs = { ".zshrc", ".bashrc", ".bash_profile", ".profile" };

                foreach (string config in shellConfigs)
                {
                    string configPath = Path.Combine(homeDir, config);
                    if (File.Exists(configPath))
                    {
                        string content = File.ReadAllText(configPath);
                        string pathLine = $"export PATH=\"$PATH:{directory}\"";
                        
                        if (content.Contains(pathLine))
                        {
                            // Remove the line and the comment
                            content = content.Replace("# Added by Orange installer\n", "");
                            content = content.Replace(pathLine + "\n", "");
                            content = content.Replace(pathLine, "");
                            
                            File.WriteAllText(configPath, content);
                            Console.WriteLine($"Removed from PATH in {Path.GetFileName(configPath)}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not remove from PATH: {ex.Message}");
            }
        }
    }
}