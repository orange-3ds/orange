using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal; // For Windows admin check
using System.Text.Json;
using System.Threading.Tasks;
using OrangeLib;
using CollinExecute;

namespace Installer
{
    static class Program
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        
        
        private const string Version = "1.0.0";

        static void Main(string[] args)
        {
            if (!IsRunningAsAdministratorOrRoot())
            {
                Console.Error.WriteLine("Error: Installer must be run as administrator/root to install to system directories.");
                if (IsWindows())
                {
                    Console.Error.WriteLine("Please right-click and select 'Run as administrator'.");
                }
                else if (IsLinux())
                {
                    Console.Error.WriteLine("Please run this installer with 'sudo'.");
                }
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"Orange Library Manager Installer v{Version}");
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

            // Parse version argument
            string? targetVersion = null;
            if (args.Length >= 2 && (args[0] == "--version" || args[0] == "-v"))
            {
                targetVersion = args[1];
                if (string.IsNullOrWhiteSpace(targetVersion))
                {
                    Console.Error.WriteLine("Error: Version argument cannot be empty.");
                    Environment.Exit(1);
                    return;
                }
                // Ensure version starts with 'v' for consistency with GitHub releases
                if (!targetVersion.StartsWith("v"))
                {
                    targetVersion = "v" + targetVersion;
                }
            }

            InstallOrangeAsync(targetVersion).Wait();
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: Installer [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h        Show this help message");
            Console.WriteLine("  --uninstall, -u   Uninstall Orange");
            Console.WriteLine("  --version, -v     Specify version to install (e.g., v1.0.0)");
            Console.WriteLine();
            Console.WriteLine("Default behavior (no options): Install latest Orange version");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Installer                    # Install latest version");
            Console.WriteLine("  Installer --version v1.0.2   # Install specific version");
        }

        static async Task InstallOrangeAsync(string? targetVersion = null)
        {
            try
            {
                if (targetVersion != null)
                {
                    Console.WriteLine($"Starting Orange installation for version {targetVersion}...");
                }
                else
                {
                    Console.WriteLine("Starting Orange installation (latest version)...");
                }
                
                // Download Orange binary from GitHub releases
                string orangeExePath = await DownloadOrangeBinaryAsync(targetVersion);
                if (string.IsNullOrEmpty(orangeExePath))
                {
                    await Console.Error.WriteLineAsync("Error: Failed to download Orange binary.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"Downloaded Orange binary: {orangeExePath}");

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

                // Make executable on Unix systems
                if (!IsWindows())
                {
                    MakeExecutable(targetExePath);
                }

                // Add to PATH
                AddToPath(installDir);

                // Clean up downloaded file
                try
                {
                    File.Delete(orangeExePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                Console.WriteLine("✓ Orange has been installed successfully!");
                
                if (!IsWindows())
                {
                    Console.WriteLine();
                    Console.WriteLine("Note: You may need to restart your terminal or run 'source ~/.bashrc' (Linux) for the PATH changes to take effect.");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Installation failed: {ex.Message}");
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

        static async Task<string> DownloadOrangeBinaryAsync(string? targetVersion = null)
        {
            try
            {
                if (targetVersion != null)
                {
                    Console.WriteLine($"Fetching release information for version {targetVersion} from GitHub...");
                }
                else
                {
                    Console.WriteLine("Fetching latest release information from GitHub...");
                }
                
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Orange-Installer/1.0");
                    
                    // Get release info - either latest or specific version
                    string apiUrl = targetVersion != null
                        ? $"https://api.github.com/repos/orange-3ds/orange/releases/tags/{targetVersion}"
                        : "https://api.github.com/repos/orange-3ds/orange/releases/latest";
                    
                    string responseJson;
                    try
                    {
                        responseJson = await httpClient.GetStringAsync(apiUrl);
                    }
                    catch (HttpRequestException ex)
                    {
                        if (targetVersion != null)
                        {
                            throw new Exception($"Version '{targetVersion}' not found. Please check that this version exists in the releases.");
                        }
                        else
                        {
                            throw new Exception($"Failed to fetch latest release information: {ex.Message}");
                        }
                    }
                    
                    var releaseInfo = System.Text.Json.Nodes.JsonNode.Parse(responseJson);
                    var assets = releaseInfo?["assets"]?.AsArray();
                    if (assets == null)
                    {
                        throw new Exception("No assets found in release");
                    }
                    
                    // Determine the correct binary name for the platform
                    string binaryName = GetPlatformBinaryName();
                    Console.WriteLine($"Looking for binary: {binaryName}");
                    
                    // Find the correct asset
                    string downloadUrl = "";
                    foreach (var asset in assets)
                    {
                        var name = asset?["name"]?.ToString() ?? "";
                        var url = asset?["browser_download_url"]?.ToString() ?? "";
                        if (name.Equals(binaryName, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = url;
                            break;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        throw new Exception($"Binary '{binaryName}' not found in release assets");
                    }
                    
                    Console.WriteLine($"Downloading from: {downloadUrl}");
                    
                    // Download the binary
                    byte[] binaryData = await httpClient.GetByteArrayAsync(downloadUrl);
                    
                    // Save to temporary file
                    string tempFileName;
                    if (IsWindows())
                        tempFileName = binaryName;
                    else
                        tempFileName = "orange";
                    string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                    await File.WriteAllBytesAsync(tempPath, binaryData);
                    Console.WriteLine($"Downloaded binary to: {tempPath}");
                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to download Orange binary: {ex.Message}");
                return string.Empty;
            }
        }
        
        static string GetPlatformBinaryName()
        {
            if (IsWindows())
            {
                return "orange.exe";
            }
            else if (IsLinux())
            {
                return "orange-linux";
            }
            else
            {
                return "orange";
            }
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

        static void MakeExecutable(string filePath)
        {
            // Make file executable on Unix systems using CollinExecute when available
            string chmodCommand = $"chmod +x \"{filePath}\"";
            
    
            
            bool success = CollinExecute.Shell.SystemCommand(chmodCommand);
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
                // Use PowerShell to add to user PATH with CollinExecute when available
                string command = $"powershell -Command \"$env:PATH += ';{directory}'; [Environment]::SetEnvironmentVariable('PATH', $env:PATH, 'User')\"";
  
                
                bool success = CollinExecute.Shell.SystemCommand(command);
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
                // Use PowerShell to remove from user PATH with CollinExecute when available
                string command = $"powershell -Command \"$path = [Environment]::GetEnvironmentVariable('PATH', 'User'); $newPath = $path -replace [regex]::Escape(';{directory}'), ''; [Environment]::SetEnvironmentVariable('PATH', $newPath, 'User')\"";
                
                
                bool success = CollinExecute.Shell.SystemCommand(command);
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

        static bool IsRunningAsAdministratorOrRoot()
        {
            if (IsWindows())
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            else
            {
                // On Unix, UID 0 is root
                return GetUid() == 0;
            }
        }

        static int GetUid()
        {
            if (IsWindows()) return -1;
            try
            {
                return GetUnixUid();
            }
            catch
            {
                return -1;
            }
        }

        private static IntPtr libcHandle = IntPtr.Zero;
        private delegate uint GetUidDelegate();
        private static GetUidDelegate getuid;

        static Program()
        {
            // Attempt to load libc dynamically
            string[] libcNames = { "libc.so.6", "libc" };
            foreach (var name in libcNames)
            {
                if (NativeLibrary.TryLoad(name, out libcHandle))
                {
                    IntPtr getuidPtr = NativeLibrary.GetExport(libcHandle, "getuid");
                    getuid = Marshal.GetDelegateForFunctionPointer<GetUidDelegate>(getuidPtr);
                    break;
                }
            }

            if (libcHandle == IntPtr.Zero || getuid == null)
            {
                throw new InvalidOperationException("Failed to load libc or locate getuid function.");
            }
        }
        private static int GetUnixUid() => (int)getuid();
    }
}