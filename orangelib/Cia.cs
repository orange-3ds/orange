using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OrangeLib
{
    /// <summary>
    /// Orchestrates the entire process of building a 3DS CIA file from source assets.
    /// This class replaces the old 'Cia' class.
    /// </summary>
    public class CiaBuilder(string title, string description, string author, string largeIconPath, string smallIconPath, string bannerPath, string bannerAudioPath)
    {
        private readonly string _buildDir = ".orange";

        /// <summary>
        /// Generates all assets and builds the final CIA file.
        /// </summary>
        /// <returns>True if the build was successful, otherwise false.</returns>
        public async Task<bool> GenerateCia()
        {
            // --- 1. Pre-build Checks and Setup ---
            string elfPath = $"{title}.elf";
            if (!File.Exists(elfPath))
            {
                Console.WriteLine($"Error: ELF file not found at '{elfPath}'. Please build the project first.");
                return false;
            }

            // Ensure the build directory exists.
            Directory.CreateDirectory(_buildDir);

            try
            {
                // --- 2. Asset Generation ---
                Console.WriteLine("--- Generating 3DS Assets ---");

                // Generate Banner
                byte[] bannerContents = await Bannertool.Net.BannerBuilder.CreateBannerAsync(bannerPath, bannerAudioPath);
                string bannerOutputPath = Path.Combine(_buildDir, "banner.bnr");
                await File.WriteAllBytesAsync(bannerOutputPath, bannerContents);
                Console.WriteLine($" > Banner successfully created at: {bannerOutputPath}");

                // Generate SMDH (Icon)
                byte[] iconContents = await Bannertool.Net.SmdhBuilder.CreateSmdhAsync(largeIconPath, smallIconPath, title, description, author);
                string smdhOutputPath = Path.Combine(_buildDir, "smdh.bin");
                await File.WriteAllBytesAsync(smdhOutputPath, iconContents);
                Console.WriteLine($" > SMDH icon successfully created at: {smdhOutputPath}");

                // --- 3. CIA Compilation ---
                Console.WriteLine("\n--- Compiling CIA ---");

                // Generate the RSF file once.
                string rsfContent = RsfHelper.GenerateRsf(title);
                string rsfPath = Path.Combine(_buildDir, "makerom.rsf");
                await File.WriteAllTextAsync(rsfPath, rsfContent);

                // Create the CXI file (the main application body).
                string cxiPath = Path.Combine(_buildDir, "output.cxi");
                if (!MakeromRunner.CreateCxi(rsfPath, elfPath, smdhOutputPath, bannerOutputPath, cxiPath))
                {
                    // The runner already printed the error, so we just return.
                    return false;
                }

                // Create the final CIA file from the CXI.
                string ciaOutputPath = $"{title}.cia";
                if (!MakeromRunner.CreateCia(cxiPath, ciaOutputPath))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during the build process: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// A robust wrapper for executing makerom commands.
    /// It captures and displays output, making debugging much easier.
    /// This replaces the need for an external dependency like CollinExecute.
    /// </summary>
    static class MakeromRunner
    {
        public static bool CreateCxi(string rsfPath, string elfPath, string iconPath, string bannerPath, string cxiOutputPath)
        {
            Console.WriteLine(" > Running makerom to create CXI...");
            // Arguments are properly formatted to handle potential spaces in paths.
            string args = $"-f cxi -o \"{cxiOutputPath}\" -rsf \"{rsfPath}\" -target t -elf \"{elfPath}\" -icon \"{iconPath}\" -banner \"{bannerPath}\"";
            return Execute(args);
        }

        public static bool CreateCia(string cxiPath, string ciaOutputPath)
        {
            Console.WriteLine(" > Running makerom to create CIA...");
            string args = $"-f cia -o \"{ciaOutputPath}\" -content \"{cxiPath}\":0:0";
            return Execute(args);
        }

        private static bool Execute(string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "makerom", // Assumes 'makerom' is in the system's PATH
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    process.Start();

                    // Read output and error streams to avoid deadlocks and display them.
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    // Print the output for debugging purposes
                    if (!string.IsNullOrWhiteSpace(output))
                        Console.WriteLine(output);

                    if (process.ExitCode != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nError: makerom exited with code {process.ExitCode}.");
                        if (!string.IsNullOrWhiteSpace(error))
                            Console.WriteLine(error);
                        Console.ResetColor();
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError: Could not execute 'makerom'. Ensure it is in your system's PATH. Details: {ex.Message}");
                    Console.ResetColor();
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Helper class for generating the makerom RSF configuration file.
    /// </summary>
    static class RsfHelper
    {
        // Use a single static Random instance to avoid generating the same
        // numbers if called in quick succession.
        private static readonly Random _random = new Random();

        public static string GenerateRandomProductCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var code = new char[4];
            for (int i = 0; i < 4; i++)
                code[i] = chars[_random.Next(chars.Length)];
            return $"CTR-P-{new string(code)}";
        }

        public static string GenerateRsf(string title)
        {
            string productCode = GenerateRandomProductCode();
            // Using a verbatim string literal @"" makes the content easier to read and maintain.
            return @$"
BasicInfo:
  Title                   : {title}
  ProductCode             : {productCode}
  Logo                    : Nintendo

RomFs:
  RootPath                : romfs

TitleInfo:
  Category                : Application
  UniqueId                : 0xff3ff

Option:
  UseOnSD                 : true
  FreeProductCode         : true
  EnableCompress          : true
  
AccessControlInfo:
  CoreVersion                   : 2
  DescVersion                   : 2
  ReleaseKernelMajor            : ""02""
  ReleaseKernelMinor            : ""33"" 
  UseExtSaveData                : false
  FileSystemAccess:
   - DirectSdmc
  MemoryType                    : Application
  SystemMode                    : 64MB
  IdealProcessor                : 0
  AffinityMask                  : 1
  Priority                      : 16
  HandleTableSize               : 0x200
  DisableDebug                  : false
  CanWriteSharedPage            : true
  CanUseNonAlphabetAndNumber    : true
  PermitMainFunctionArgument    : true
  CanShareDeviceMemory          : true
  RunnableOnSleep               : false
  SpecialMemoryArrange          : true
  SystemModeExt                 : 124MB
  CpuSpeed                      : 804MHz
  EnableL2Cache                 : true
  CanAccessCore2                : true

SystemCallAccess: 
    ControlMemory: 1
    QueryMemory: 2
    ExitProcess: 3
    GetProcessIdealProcessor: 6
    CreateThread: 8
    ExitThread: 9
    SleepThread: 10
    GetThreadPriority: 11
    SetThreadPriority: 12
    GetThreadIdealProcessor: 15
    GetCurrentProcessorNumber: 17
    CreateMutex: 19
    ReleaseMutex: 20
    CreateSemaphore: 21
    ReleaseSemaphore: 22
    CreateEvent: 23
    SignalEvent: 24
    ClearEvent: 25
    CreateTimer: 26
    SetTimer: 27
    CancelTimer: 28
    ClearTimer: 29
    CreateMemoryBlock: 30
    MapMemoryBlock: 31
    UnmapMemoryBlock: 32
    CreateAddressArbiter: 33
    ArbitrateAddress: 34
    CloseHandle: 35
    WaitSynchronizationN: 37
    GetSystemTick: 40
    GetHandleInfo: 41
    GetSystemInfo: 42
    GetProcessInfo: 43
    GetThreadInfo: 44
    ConnectToPort: 45
    SendSyncRequest: 50
    GetProcessId: 53
    GetThreadId: 55
    GetResourceLimit: 56
    Break: 60
    OutputDebugString: 61

ServiceAccessControl:
   - cfg:u
   - fs:USER
   - gsp::Gpu
   - hid:USER
   - ndm:u
   - pxi:dev
   - APT:U
   - csnd:SND

SystemControlInfo:
  SaveDataSize: 0K
  StackSize: 0x40000
";
        }
    }
}