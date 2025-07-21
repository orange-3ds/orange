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
            // Before deleting, ensure all files and subdirectories are not read-only
            if (Directory.Exists("package"))
            {
                try
                {
                    RemoveReadOnlyAttributesRecursively("package");
                    Directory.Delete("package", true);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to delete 'package' directory: {ex.Message}");
                    return;
                }
            }
            if (File.Exists("Makefile"))
            {
                bool successclean = CollinExecute.Shell.SystemCommand("make clean");
                bool successmake = CollinExecute.Shell.SystemCommand("make");
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