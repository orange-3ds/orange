# Orange - DevkitPro Library Manager

Orange is a .NET 9.0 C# DevkitPro Library Manager for Nintendo 3DS development. It manages libraries, builds CIA files, creates 3DS applications and libraries, and streams to 3DS consoles via 3dslink.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Setup
- **CRITICAL**: .NET 9.0 SDK is required. Install with:
  ```bash
  curl -L -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 9.0 --install-dir /tmp/dotnet
  export PATH="/tmp/dotnet:$PATH"
  dotnet --version  # Should show 9.0.x
  ```

### Build and Test Commands
- **Bootstrap and build the repository:**
  ```bash
  cd /path/to/orange
  dotnet restore                    # Takes ~10 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
  dotnet build --no-restore        # Takes ~10 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
  ```
- **Run tests:**
  ```bash
  dotnet test --no-build --verbosity normal    # Takes ~3 seconds, runs 56 tests. NEVER CANCEL. Set timeout to 30+ seconds.
  ```

### Running Orange Application
- **Get help:**
  ```bash
  dotnet run --project orange --no-build -- --help
  ```
- **Get version:**
  ```bash
  dotnet run --project orange --no-build -- --version    # Shows v1.1.0
  ```

### Common Orange Operations
- **Create new 3DS application project:**
  ```bash
  mkdir my-3ds-app && cd my-3ds-app
  dotnet run --project /path/to/orange/orange --no-build -- init app
  ```
- **Create new 3DS library project:**
  ```bash
  mkdir my-3ds-lib && cd my-3ds-lib
  dotnet run --project /path/to/orange/orange --no-build -- init library
  ```
- **Build 3DS project (requires DevkitPro tools):**
  ```bash
  # In a directory with app.cfg or library.cfg
  dotnet run --project /path/to/orange/orange --no-build -- build
  ```
- **Build CIA file (requires makerom, bannertool, ffmpeg in PATH):**
  ```bash
  dotnet run --project /path/to/orange/orange --no-build -- build cia
  ```

## Validation

### Manual Testing Requirements
- **ALWAYS test core functionality after making changes:**
  - Run `dotnet run --project orange --no-build -- --help` and verify help output
  - Run `dotnet run --project orange --no-build -- --version` and verify version shows
  - Test `init app` in empty directory and verify project files are created
  - Test `init library` in empty directory and verify library files are created
  - Test invalid commands show proper error messages
- **ALWAYS run the full test suite:**
  ```bash
  dotnet test --no-build --verbosity normal
  ```
- **Build validation always passes without errors.**

### Expected Build Warnings
- Build produces 5 warnings related to Windows-specific code in `Installer/installer.cs` (CA1416 warnings about WindowsPrincipal/WindowsIdentity usage)
- These warnings are expected and do not indicate build failure

## Project Structure

### Key Projects
- **`orange/`** - Main console application (executable)
- **`orangelib/`** - Core library functionality for 3DS operations
- **`configlibnet/`** - Configuration management library  
- **`Installer/`** - Installer executable
- **`Tests/`** - Unit tests using xUnit (56 tests)

### Important Files
- **`orange.sln`** - Visual Studio solution file
- **`.github/workflows/dotnet.yml`** - CI/CD pipeline (builds on Ubuntu with .NET 9.x)
- **`orange/Orange.cs`** - Main application entry point with command handling
- **`orangelib/OrangeLib.cs`** - Core library utilities and 3DS operations

### External Dependencies
- **For CIA building:** `makerom`, `bannertool`, `ffmpeg` must be in PATH
- **For streaming:** `3dslink` must be in PATH
- **For building 3DS projects:** DevkitPro toolchain required
- **Internet access required for:** Library downloads (`sync`, `add` commands)

## Common Tasks

### Building from Scratch
```bash
# Complete build workflow
cd /path/to/orange
dotnet restore                    # ~10 sec
dotnet build --no-restore        # ~10 sec  
dotnet test --no-build            # ~3 sec, 56 tests
```

### Testing Changes
```bash
# After making code changes, always run:
dotnet build --no-restore
dotnet test --no-build
# Test core functionality manually
dotnet run --project orange --no-build -- --help
```

### Project Commands Reference
```bash
# In Orange repository root:
dotnet run --project orange --no-build -- COMMAND

# Available commands:
# init <app|library>    - Create new 3DS project
# sync                  - Download dependencies (requires internet)
# build [cia]          - Build project (requires DevkitPro)
# add <library_name>    - Add library dependency (requires internet)  
# stream <ip> [options] - Stream to 3DS (requires 3dslink)
```

### Repository Layout
```
orange/
├── .github/workflows/dotnet.yml    # CI/CD pipeline
├── orange/                         # Main console app
│   ├── Orange.cs                   # Main entry point
│   └── orange.csproj              # .NET 9.0 executable project
├── orangelib/                      # Core library
│   ├── OrangeLib.cs               # Main library code
│   ├── Cia.cs                     # CIA building functionality
│   ├── Audio.cs                   # Audio processing
│   └── orangelib.csproj           # .NET 9.0 library project
├── configlibnet/                   # Configuration library
├── Installer/                      # Installer executable  
├── Tests/                          # Unit tests (xUnit)
└── orange.sln                      # VS solution file
```

## Critical Reminders
- **NEVER CANCEL** any dotnet commands - they complete quickly (~10 seconds max)
- **ALWAYS** use `--no-build` flag when running Orange to avoid rebuilding
- **Internet access required** for `sync` and `add` commands
- **DevkitPro toolchain required** for actual 3DS building
- **External tools required** for full functionality: makerom, bannertool, ffmpeg, 3dslink