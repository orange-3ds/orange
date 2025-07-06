using System.Diagnostics;
using System.Runtime.InteropServices;
using OrangeInfoLib;
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

    }
    static public class Info
    {
        static readonly string _version = "0.0.0 Beta";
        static public void Help()
        {
            System.Console.WriteLine("Usage: orange [command] [options]");
        }
        static public void Version()
        {
            System.Console.WriteLine($"Orange Version {_version}");
        }
    }
    public static class Compile
    {
        public static bool GccCompile(string arguments)
        {
            string command;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                Console.Error.WriteLine("No arguments provided for GCC compilation.");
                return false;
            }
            if (Utils.IsWindows())
            {
                command = $@"C:\devkitPro\devkitARM\bin\arm-none-eabi-gcc.exe {arguments}";
            }
            else if (Utils.IsLinux() || Utils.IsMacOS())
            {
                command = "./opt/devkitpro/devkitARM/bin/arm-none-eabi-gcc " + arguments;
            }
            else
            {
                Console.Error.WriteLine("Unsupported OS platform for GCC compilation.");
                return false;
            }
            try
            {
                Utils.RunCommandStreamOutput(command);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GCC compilation failed: {ex.Message}");
                return false;
            }
            return true;
        }
    }
}