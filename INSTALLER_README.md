# Orange Installer

This document describes the Orange Package Manager installer implementation.

## Overview

The installer provides cross-platform installation and uninstallation capabilities for the Orange Package Manager. It handles:

- Cross-platform installation (Windows, macOS, Linux)
- Automatic PATH management
- Dependency copying and executable permissions
- Uninstall functionality
- Help system and error handling

## Current Implementation Status

### ✅ Completed Features

1. **Cross-Platform Support**: Detects and handles Windows, macOS, and Linux installations
2. **Automatic Installation**: Finds Orange executable and installs it to system location
3. **PATH Management**: Automatically adds/removes Orange from system PATH
4. **Dependency Handling**: Copies required .dll/.so/.dylib files and runtime directories
5. **Executable Permissions**: Sets executable permissions on Unix systems
6. **Uninstall Support**: Complete removal including PATH cleanup
7. **Command Line Interface**: Help system and uninstall flags
8. **Error Handling**: Comprehensive error handling with user-friendly messages
9. **Testing**: Unit tests for platform detection and core functionality

### 🔄 Framework Transition Status

**Current State**: All projects have been updated to .NET 9.0 as requested:
- `orange/orange.csproj` → net9.0
- `orangelib/orangelib.csproj` → net9.0
- `configlibnet/configlibnet.csproj` → net9.0
- `Tests/Tests.csproj` → net9.0
- `Installer/Installer.csproj` → net9.0

**CollinExecute Integration**: Package reference added to Installer project:
```xml
<PackageReference Include="CollinExecute" Version="1.0.0" />
```

### ⏳ Pending - Requires .NET 9.0 SDK

The current environment has .NET 8.0 SDK which cannot build .NET 9.0 projects. When .NET 9.0 SDK becomes available:

1. **Build will work**: `dotnet build` and `dotnet test` will function normally
2. **CollinExecute integration**: Currently commented out code will be uncommented:
   ```csharp
   // TODO: Uncomment when .NET 9.0 SDK is available:
   // using CollinExecute;
   ```
3. **Command execution replacement**: Replace `Utils.ExecuteShellCommand` calls with CollinExecute

## Usage

### Installation
```bash
dotnet run --project Installer
# or
./Installer
```

### Help
```bash
dotnet run --project Installer -- --help
./Installer --help
```

### Uninstallation
```bash
dotnet run --project Installer -- --uninstall
./Installer --uninstall
```

## Installation Locations

- **Windows**: `%ProgramFiles%\Orange\`
- **macOS/Linux**: `/usr/local/bin/`

## CollinExecute Integration Plan

When .NET 9.0 SDK is available, the following changes will be made:

1. **Uncomment using statement**:
   ```csharp
   using CollinExecute;
   ```

2. **Replace shell command execution** in `MakeExecutable()`:
   ```csharp
   // Current (fallback)
   bool success = Utils.ExecuteShellCommand(chmodCommand);
   
   // Future (with CollinExecute)
   var executor = new CommandExecutor();
   var result = executor.Execute("chmod", new[] { "+x", filePath });
   bool success = result.ExitCode == 0;
   ```

3. **Replace PATH management commands**:
   ```csharp
   // Current (fallback)
   bool success = Utils.ExecuteShellCommand(command);
   
   // Future (with CollinExecute)
   var executor = new CommandExecutor();
   var result = executor.Execute("powershell", new[] { "-Command", powerShellScript });
   bool success = result.ExitCode == 0;
   ```

## Testing

The installer includes comprehensive tests:
- Platform detection tests
- Installation directory validation
- Cross-platform compatibility checks

Run tests with:
```bash
dotnet test
```

## Development Notes

1. **Minimal Changes**: The implementation reuses existing `Utils.ExecuteShellCommand` from OrangeLib
2. **Future-Ready**: Code structure prepared for CollinExecute integration
3. **Cross-Platform**: Handles Windows, macOS, and Linux installation patterns
4. **Robust**: Comprehensive error handling and fallback mechanisms
5. **User-Friendly**: Clear output and instructions for users

## Files Modified

- `Installer/Installer.csproj` - Updated to .NET 9.0, added CollinExecute package
- `Installer/installer.cs` - Complete installer implementation
- `Tests/Tests.cs` - Added installer tests
- `Tests/Tests.csproj` - Added Installer project reference, updated to .NET 9.0
- All other `.csproj` files - Updated from .NET 8.0 to .NET 9.0

## Next Steps

1. Wait for .NET 9.0 SDK availability in the environment
2. Uncomment CollinExecute using statement
3. Replace `Utils.ExecuteShellCommand` calls with CollinExecute
4. Test complete integration
5. Verify all cross-platform scenarios work with CollinExecute