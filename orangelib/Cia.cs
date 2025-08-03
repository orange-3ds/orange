using System.Diagnostics;

namespace OrangeLib
{
    /// <summary>
    /// Orchestrates the entire process of building a 3DS CIA file from source assets.
    /// This class replaces the old 'Cia' class.
    /// </summary>
    public class CiaBuilder(string title, string description, string author, string largeIconPath, string bannerPath, string bannerAudioPath)
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

                // Convert audio to the required format.
                string convertedAudioPath = Path.Combine(_buildDir, "converted_audio_banner.wav");
                await AudioConverter.ConvertWavTo3dsFormatAsync(bannerAudioPath, convertedAudioPath);

                // Generate Banner
                string bannerOutputPath = Path.Combine(_buildDir, "banner.bnr");
                // Use the 'convertedAudioPath' variable instead of the original 'bannerAudioPath'
                BannertoolHelper.CreateBanner(bannerPath, bannerOutputPath, convertedAudioPath);
                Console.WriteLine($" > Banner successfully created at: {bannerOutputPath}");

                // Generate SMDH (Icon)
                string smdhOutputPath = Path.Combine(_buildDir, "smdh.bin");
                if (!BannertoolHelper.CreateIcn(largeIconPath, smdhOutputPath, title, description, author))
                {
                    Console.WriteLine("Error: Failed to create SMDH icon.");
                    return false;
                }
                Console.WriteLine($" > SMDH icon successfully created at: {smdhOutputPath}");

                // Generate the RSF file once.
                string rsfContent = RsfHelper.GenerateRsf(title);
                string rsfPath = Path.Combine(_buildDir, "makerom.rsf");
                await File.WriteAllTextAsync(rsfPath, rsfContent);

                

                // --- 3. CIA Compilation ---
                Console.WriteLine("\n--- Compiling CIA ---");

                

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
    static class BannertoolHelper
    {
        private static bool Execute(string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "bannertool", // Assumes 'bannertool' is in the system's PATH
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
                        Console.WriteLine($"\nError: bannertool exited with code {process.ExitCode}.");
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
                    Console.WriteLine($"\nError: Could not execute 'bannertool'. Ensure it is in your system's PATH. Details: {ex.Message}");
                    Console.ResetColor();
                    return false;
                }
            }
        }
        public static bool CreateIcn(string iconpath, string outputPath, string title, string description, string author)
        {
            Console.WriteLine(" > Running bannertool to create icon...");
            // Properly quote arguments to handle spaces and allow multi-word titles/descriptions/authors.
            string args = $"makesmdh -s \"{title}\" -l \"{description}\" -p \"{author}\" -i \"{iconpath}\" -o \"{outputPath}\"";
            return Execute(args);
        }
        public static bool CreateBanner(string bannerpath, string outputPath, string audiopath)
        {
            Console.WriteLine(" > Running bannertool to create banner...");
            // Properly quote arguments to handle spaces in file paths.
            string args = $"makebanner -i \"{bannerpath}\" -a \"{audiopath}\" -o \"{outputPath}\"";
            return Execute(args);
        }
    }
}

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
        // Check if there is existing rsf file.
        if (File.Exists(".orange/makerom.rsf"))
        {
            Console.WriteLine(" > Using existing RSF file.");
            return File.ReadAllText(".orange/makerom.rsf");
        }
        string productCode = GenerateRandomProductCode();
        // Using a verbatim string literal @"" makes the content easier to read and maintain.
        return @$"
BasicInfo:
  Title                   : {title}
  ProductCode             : {productCode}
  Logo                    : Homebrew # Nintendo / Licensed / Distributed / iQue / iQueForSystem

RomFs:
  # Specifies the root path of the read only file system to include in the ROM.
  #RootPath                : romfs

TitleInfo:
  Category                : Application
  UniqueId                : 0xff3ff

Option:
  UseOnSD                 : true # true if App is to be installed to SD
  FreeProductCode         : true # Removes limitations on ProductCode
  MediaFootPadding        : false # If true CCI files are created with padding
  EnableCrypt             : false # Enables encryption for NCCH and CIA
  EnableCompress          : true # Compresses where applicable (currently only exefs:/.code)
  
AccessControlInfo:
  CoreVersion                   : 2

  # Exheader Format Version
  DescVersion                   : 2
  
  # Minimum Required Kernel Version (below is for 4.5.0)
  ReleaseKernelMajor            : ""02""
  ReleaseKernelMinor            : ""33"" 

  # ExtData
  UseExtSaveData                : false # enables ExtData       
  #ExtSaveDataId                : 0x300 # only set this when the ID is different to the UniqueId

  # FS:USER Archive Access Permissions
  # Uncomment as required
  FileSystemAccess:
   #- CategorySystemApplication
   #- CategoryHardwareCheck
   #- CategoryFileSystemTool
   #- Debug
   #- TwlCardBackup
   #- TwlNandData
   #- Boss
   - DirectSdmc
   #- Core
   #- CtrNandRo
   #- CtrNandRw
   #- CtrNandRoWrite
   #- CategorySystemSettings
   #- CardBoard
   #- ExportImportIvs
   #- DirectSdmcWrite
   #- SwitchCleanup
   #- SaveDataMove
   #- Shop
   #- Shell
   #- CategoryHomeMenu

  # Process Settings
  MemoryType                    : Application # Application/System/Base
  SystemMode                    : 64MB # 64MB(Default)/96MB/80MB/72MB/32MB
  IdealProcessor                : 0
  AffinityMask                  : 1
  Priority                      : 16
  MaxCpu                        : 0 # Let system decide
  HandleTableSize               : 0x200
  DisableDebug                  : false
  EnableForceDebug              : false
  CanWriteSharedPage            : true
  CanUsePrivilegedPriority      : false
  CanUseNonAlphabetAndNumber    : true
  PermitMainFunctionArgument    : true
  CanShareDeviceMemory          : true
  RunnableOnSleep               : false
  SpecialMemoryArrange          : true

  # New3DS Exclusive Process Settings
  SystemModeExt                 : 124MB # Legacy(Default)/124MB/178MB  Legacy:Use Old3DS SystemMode
  CpuSpeed                      : 804MHz # 268MHz(Default)/804MHz
  EnableL2Cache                 : true # false(default)/true
  CanAccessCore2                : true 

  # Virtual Address Mappings
  IORegisterMapping:
   - 1ff00000-1ff7ffff   # DSP memory
  MemoryMapping: 
   - 1f000000-1f5fffff:r # VRAM

  # Accessible SVCs, <Name>:<ID>
  SystemCallAccess: 
    ArbitrateAddress: 34
    Break: 60
    CancelTimer: 28
    ClearEvent: 25
    ClearTimer: 29
    CloseHandle: 35
    ConnectToPort: 45
    ControlMemory: 1
    CreateAddressArbiter: 33
    CreateEvent: 23
    CreateMemoryBlock: 30
    CreateMutex: 19
    CreateSemaphore: 21
    CreateThread: 8
    CreateTimer: 26
    DuplicateHandle: 39
    ExitProcess: 3
    ExitThread: 9
    GetCurrentProcessorNumber: 17
    GetHandleInfo: 41
    GetProcessId: 53
    GetProcessIdOfThread: 54
    GetProcessIdealProcessor: 6
    GetProcessInfo: 43
    GetResourceLimit: 56
    GetResourceLimitCurrentValues: 58
    GetResourceLimitLimitValues: 57
    GetSystemInfo: 42
    GetSystemTick: 40
    GetThreadContext: 59
    GetThreadId: 55
    GetThreadIdealProcessor: 15
    GetThreadInfo: 44
    GetThreadPriority: 11
    MapMemoryBlock: 31
    OutputDebugString: 61
    QueryMemory: 2
    ReleaseMutex: 20
    ReleaseSemaphore: 22
    SendSyncRequest1: 46
    SendSyncRequest2: 47
    SendSyncRequest3: 48
    SendSyncRequest4: 49
    SendSyncRequest: 50
    SetThreadPriority: 12
    SetTimer: 27
    SignalEvent: 24
    SleepThread: 10
    UnmapMemoryBlock: 32
    WaitSynchronization1: 36
    WaitSynchronizationN: 37
    Backdoor: 123

  # Service List
  # Maximum 34 services (32 if firmware is prior to 9.3.0)
  ServiceAccessControl:
   - cfg:u
   - fs:USER
   - gsp::Gpu
   - hid:USER
   - ndm:u
   - pxi:dev
   - APT:U
   - ac:u
   - act:u
   - am:net
   - boss:U
   - cam:u
   - cecd:u
   - csnd:SND
   - frd:u
   - http:C
   - ir:USER
   - ir:u
   - ir:rst
   - ldr:ro
   - mic:u
   - news:u
   - nfc:u
   - nim:aoc
   - nwm::UDS
   - ptm:u
   - qtm:u
   - soc:U
   - ssl:C
   - y2r:u


SystemControlInfo:
  SaveDataSize: 0K
  RemasterVersion: 0
  StackSize: 0x40000

  # Modules that run services listed above should be included below
  # Maximum 48 dependencies
  # If a module is listed that isn't present on the 3DS, the title will get stuck at the logo (3ds waves)
  # So act, nfc and qtm are commented for 4.x support. Uncomment if you need these.
  # <module name>:<module titleid>
  Dependency: 
    ac: 0x0004013000002402
    #act: 0x0004013000003802
    am: 0x0004013000001502
    boss: 0x0004013000003402
    camera: 0x0004013000001602
    cecd: 0x0004013000002602
    cfg: 0x0004013000001702
    codec: 0x0004013000001802
    csnd: 0x0004013000002702
    dlp: 0x0004013000002802
    dsp: 0x0004013000001a02
    friends: 0x0004013000003202
    gpio: 0x0004013000001b02
    gsp: 0x0004013000001c02
    hid: 0x0004013000001d02
    http: 0x0004013000002902
    i2c: 0x0004013000001e02
    ir: 0x0004013000003302
    mcu: 0x0004013000001f02
    mic: 0x0004013000002002
    ndm: 0x0004013000002b02
    news: 0x0004013000003502
    #nfc: 0x0004013000004002
    nim: 0x0004013000002c02
    nwm: 0x0004013000002d02
    pdn: 0x0004013000002102
    ps: 0x0004013000003102
    ptm: 0x0004013000002202
    #qtm: 0x0004013020004202
    ro: 0x0004013000003702
    socket: 0x0004013000002e02
    spi: 0x0004013000002302
    ssl: 0x0004013000002f02
";
    }
}
