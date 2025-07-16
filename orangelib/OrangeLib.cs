using Newtonsoft.Json;
using OrangeLib.Info;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
namespace OrangeLib
{
    public static class Utils
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static async Task DownloadFileAsync(string fileUrl, string localFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    byte[] fileBytes = await httpClient.GetByteArrayAsync(fileUrl);
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                    Console.WriteLine($"File downloaded to {localFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
            }
        }
        public static string RunCommandSafe(string command)
        {
            try
            {
                string shell, shellArgs;

                if (IsWindows())
                {
                    shell = "cmd.exe";
                    shellArgs = $"/c \"{command}\"";
                }
                else if (IsLinux() || IsMacOS())
                {
                    shell = "/bin/bash";
                    shellArgs = $"-c \"{command}\"";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported OS platform.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                    Console.Error.WriteLine($"[stderr]: {error}");

                return output.Trim(); // Clean up trailing newlines
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[exception]: {ex.Message}");
                return string.Empty;
            }
        }
        public static bool RunCommandStreamOutputChecked(string command)
        {
            try
            {
                string shell, shellArgs;

                if (IsWindows())
                {
                    shell = "cmd.exe";
                    shellArgs = $"/c \"{command}\"";
                }
                else if (IsLinux() || IsMacOS())
                {
                    shell = "/bin/bash";
                    shellArgs = $"-c \"{command}\"";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported OS platform.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.Error.WriteLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[exception]: {ex.Message}");
                return false;
            }
        }

        public static void RunCommandStreamOutput(string command)
        {
            try
            {
                string shell, shellArgs;

                if (IsWindows())
                {
                    shell = "cmd.exe";
                    shellArgs = $"/c \"{command}\"";
                }
                else if (IsLinux() || IsMacOS())
                {
                    shell = "/bin/bash";
                    shellArgs = $"-c \"{command}\"";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported OS platform.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.Error.WriteLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[exception]: {ex.Message}");
            }
        }
        public static void CreateZip(string SourceDir, string ZipName)
        {
            ZipFile.CreateFromDirectory(SourceDir, ZipName);
        }
        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativeDirPath = Path.GetRelativePath(sourcePath, dirPath);
                Directory.CreateDirectory(Path.Combine(targetPath, relativeDirPath));
            }

            foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string relativeFilePath = Path.GetRelativePath(sourcePath, filePath);
                string targetFilePath = Path.Combine(targetPath, relativeFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);
                File.Copy(filePath, targetFilePath, true);
            }
        }
        public static void CopyFile(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
            File.Copy(sourceFile, targetFile);
        }
        public static void CopyDirectoryRecursively(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string newDirPath = dirPath.Replace(sourceDir, targetDir);
                Directory.CreateDirectory(newDirPath);
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string newFilePath = filePath.Replace(sourceDir, targetDir);
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath)!); // ensure directory exists
                File.Copy(filePath, newFilePath, true); // overwrite if exists
            }
        }
    }
    // TODO: Write logic
    static public class Package
    {
        public static void CreatePackage(Information packageinfo)
        {
            if (File.Exists("package.json"))
            {
                File.Delete("package.json");
            }
            if (File.Exists("package.zip"))
            {
                File.Delete("package.zip");
            }
            if (Directory.Exists("package"))
            {
                Directory.Delete("package", true);
            }
            if (File.Exists("Makefile"))
            {
                bool successclean = Utils.RunCommandStreamOutputChecked("make clean");
                bool successmake = Utils.RunCommandStreamOutputChecked("make");
                if (!successclean || !successmake)
                {
                    Console.Error.WriteLine("Build Failed.");
                    return;
                }
            }
            // prepare package directory
            if (!Directory.Exists("package"))
            {
                Directory.CreateDirectory("package");
            }
            // Serialize json from packageinfo
            string json = JsonConvert.SerializeObject(packageinfo);
            using (StreamWriter outputFile = new StreamWriter("package/package.json"))
            {
                outputFile.WriteLine(json);
            }
            // Copy output to build directory
            if (Directory.Exists("lib") && Directory.Exists("include"))
            {
                Utils.CopyFilesRecursively("lib", "package/lib");
                Utils.CopyFilesRecursively("include", "package/include");
            }
            Utils.CreateZip("package", "package.zip");
        }
        public static void InstallPackage(string packageZip)
        {
            string dirBeforeTemp = Directory.GetCurrentDirectory();

            // Extract the package zip to a temporary directory
            string tempDir = Path.Combine(Path.GetTempPath(), "OrangeLibPackage");
            Directory.CreateDirectory(tempDir);
            try
            {
                // unzip to temp directory
                ZipFile.ExtractToDirectory(packageZip, tempDir);
                // copy lib folder to DirBeforeTemp, overright it
                Utils.CopyDirectoryRecursively(Path.Combine(tempDir, "lib"), Path.Combine(dirBeforeTemp, "lib"));
                // copy include folder to DirBeforeTemp, overright it
                Utils.CopyDirectoryRecursively(Path.Combine(tempDir, "include"), Path.Combine(dirBeforeTemp, "include"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[exception]: {ex.Message}");
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}