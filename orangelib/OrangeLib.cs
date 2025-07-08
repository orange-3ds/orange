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
    }
    // TODO: Write logic
    static public class Package
    {
        public static void CreatePackage(Information packageinfo)
        {
            // INDEV
        }
    }
}