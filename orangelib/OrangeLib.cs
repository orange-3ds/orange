using Newtonsoft.Json;
using OrangeLib.Info;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace OrangeLib
{
    public static class Streaming
    {
        public static bool Stream3dsxTo3ds(string ip, int retries, string path)
        {
            int i = 0;
            while (i < retries)
            {
                try
                {
                    bool success = Utils.ExecuteShellCommand($"3dslink {path} -a {ip}");
                    if (success)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Log or handle the exception if needed
                }
                i++;
            }
            return false;
        }
    }
    public static class Utils
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        // Only for specific use.
        public static bool ExecuteShellCommand(string command)
        {
            try
            {
                var process = new Process();
                if (IsWindows())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c {command}";
                }
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"{command}\"";
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static async Task DownloadFileAsync(string fileUrl, string localFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    byte[] fileBytes = await httpClient.GetByteArrayAsync(fileUrl);
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
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
    static public class Library
    {
        public static void CreateLibrary(Information libraryinfo)
        {
            if (File.Exists("library.json"))
            {
                File.Delete("library.json");
            }
            if (File.Exists("library.zip"))
            {
                File.Delete("library.zip");
            }
            // Before deleting, ensure all files and subdirectories are not read-only
            if (Directory.Exists("library"))
            {
                try
                {
                    RemoveReadOnlyAttributesRecursively("library");
                    Directory.Delete("library", true);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to delete 'library' directory: {ex.Message}");
                    return;
                }
            }
            if (File.Exists("Makefile"))
            {
                bool successclean = Utils.ExecuteShellCommand("make clean");
                Directory.CreateDirectory("build");
                if (File.Exists("library.cfg"))
                {
                    try
                    {
                        File.Copy("library.cfg", "build/library.cfg");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to copy 'library.cfg': {ex.Message}");
                        return;
                    }
                }
                else
                {
                    Console.Error.WriteLine("'library.cfg' does not exist. Aborting operation.");
                    return;
                }
                bool successmake = Utils.ExecuteShellCommand("make");
                if (!successclean || !successmake)
                {
                    Console.Error.WriteLine("Build Failed.");
                    return;
                }
            }
            // prepare library directory
            if (!Directory.Exists("library"))
            {
                Directory.CreateDirectory("library");
            }
            // Serialize json from libraryinfo
            string json = JsonConvert.SerializeObject(libraryinfo);
            using (StreamWriter outputFile = new StreamWriter("library/library.json"))
            {
                outputFile.WriteLine(json);
            }
            // Copy output to build directory
            if (Directory.Exists("lib") && Directory.Exists("include"))
            {
                Utils.CopyFilesRecursively("lib", "library/lib");
                Utils.CopyFilesRecursively("include", "library/include");
            }
            Utils.CreateZip("library", "library.zip");
            // Delete the library directory after zipping
            try
            {
                RemoveReadOnlyAttributesRecursively("library");
                Directory.Delete("library", true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete 'library' directory: {ex.Message}");
            }
        }

        // Helper to remove read-only attributes recursively
        private static void RemoveReadOnlyAttributesRecursively(string directory)
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }
            foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(dir);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(dir, attributes & ~FileAttributes.ReadOnly);
                }
            }
            // Remove read-only from the root directory itself
            var rootAttributes = File.GetAttributes(directory);
            if ((rootAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(directory, rootAttributes & ~FileAttributes.ReadOnly);
            }
        }

        public static void InstallLibrary(string libraryZip)
        {
            string dirBeforeTemp = Directory.GetCurrentDirectory();

            // Extract the library zip to a temporary directory
            string tempDir = Path.Combine(Path.GetTempPath(), "OrangeLib_library");
            Directory.CreateDirectory(tempDir);
            try
            {
                // unzip to temp directory
                ZipFile.ExtractToDirectory(libraryZip, tempDir);
                // copy lib folder to DirBeforeTemp, overwrite it
                Utils.CopyDirectoryRecursively(Path.Combine(tempDir, "lib"), Path.Combine(dirBeforeTemp, "lib"));
                // copy include folder to DirBeforeTemp, overwrite it
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