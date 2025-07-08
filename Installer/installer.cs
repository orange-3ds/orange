using System.Runtime.InteropServices;

namespace Installer
{
    static class Program
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}