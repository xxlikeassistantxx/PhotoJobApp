# Complete System.Security.Cryptography.Pkcs Workaround for Hot Restart

## Overview
This document describes the comprehensive workaround implemented to ensure `System.Security.Cryptography.Pkcs` assembly is available to Hot Restart's signing tool, regardless of where it looks for the assembly.

## Problem
Hot Restart's signing tool fails with:
```
Could not load file or assembly 'System.Security.Cryptography.Pkcs, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies.
```

The signing tool runs as a separate process and has its own assembly resolution context, making it difficult to locate the assembly even when it's properly referenced.

## Solution Implemented

### 1. Package Reference Configuration
- Added `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="8.0.0" />`
- Ensured `PrivateAssets=""` and `ExcludeAssets=""` so the assembly is treated as a runtime dependency

### 2. Force Into Runtime Assets
The `ForcePkcsIntoRuntimeAssets` target ensures the assembly is explicitly added to:
- `RuntimeCopyLocalItems` - Ensures it's copied during runtime asset resolution
- `ReferenceCopyLocalPaths` - Ensures it's treated as a reference that should be copied locally
- Output directory - Copies it directly to the build output

This target runs **AfterTargets="ResolvePackageAssets"** to catch it early in the build process.

### 3. Multiple Copy Targets
Multiple targets copy the assembly to various locations at different stages:

#### Target: `CopyPkcsToHotRestartSigning`
- Runs: After `_GenerateHotRestartBuildSessionId`
- Copies to:
  - Hot Restart app bundle root: `%LOCALAPPDATA%\Temp\Xamarin\HotRestart\Signing\PhotoJobApp.app`
  - Hot Restart out directory: `%LOCALAPPDATA%\Temp\Xamarin\HotRestart\Signing\PhotoJobApp.app\out`
  - Hot Restart base directory: `%LOCALAPPDATA%\Temp\Xamarin\HotRestart`

#### Target: `CopyPkcsToAppBundle`
- Runs: After `_PrepareHotRestartApp`
- Ensures the assembly is in the app bundle root right before signing operations begin

#### Target: `CopyPkcsBeforeSigning`
- Runs: Before `CoreCompile`
- Early-stage copy to all potential directories as a safeguard

### 4. Fallback Strategy
Each target has fallback logic:
1. First tries to copy from build output: `$(OutputPath)System.Security.Cryptography.Pkcs.dll`
2. Falls back to NuGet package directory if output doesn't exist:
   - `net9.0` version (if available)
   - `net8.0` version (fallback)
   - `netstandard2.0` version (final fallback)

## How It Works

1. **During Package Resolution**: The assembly is forced into runtime assets, ensuring MSBuild knows it's needed.

2. **During Build**: The assembly is copied to the output directory as part of normal build process.

3. **Hot Restart Preparation**: When Hot Restart begins, multiple targets ensure the assembly is copied to:
   - The app bundle root (where signing tool likely starts its search)
   - The `out` subdirectory (where compiled assemblies are)
   - The base Hot Restart directory (fallback location)

4. **Multiple Timing Windows**: Copies happen at different build stages to catch all possible timing windows:
   - Before CoreCompile (early)
   - After _GenerateHotRestartBuildSessionId (mid)
   - After _PrepareHotRestartApp (late, right before signing)

## Testing the Solution

1. **Clean Build**:
   ```powershell
   dotnet clean
   dotnet restore
   dotnet build -f net9.0-ios
   ```

2. **Verify Assembly Presence**:
   Check that `System.Security.Cryptography.Pkcs.dll` exists in:
   - `bin\Debug\net9.0-ios\ios-arm64\`
   - Check build output messages for copy confirmations

3. **Deploy with Hot Restart**:
   - In Visual Studio, select your iPhone device
   - Build and deploy
   - The assembly should be automatically copied to all Hot Restart directories

## Build Output Messages

When the workaround is working, you should see messages like:
```
Force-added System.Security.Cryptography.Pkcs to runtime assets: C:\Users\...\.nuget\packages\system.security.cryptography.pkcs\8.0.0\lib\net8.0\System.Security.Cryptography.Pkcs.dll
Copied System.Security.Cryptography.Pkcs to Hot Restart app bundle: ...
Copied System.Security.Cryptography.Pkcs to Hot Restart out dir: ...
Final copy: System.Security.Cryptography.Pkcs to app bundle: ...
```

## Troubleshooting

If the error persists:

1. **Check Build Output**: Look for the copy messages above. If they're missing, the targets aren't running.

2. **Verify Package Location**: 
   ```powershell
   Test-Path "$env:USERPROFILE\.nuget\packages\system.security.cryptography.pkcs\8.0.0\lib\net8.0\System.Security.Cryptography.Pkcs.dll"
   ```

3. **Manual Copy (Last Resort)**:
   Run `CopyPkcsForHotRestart.ps1` before deploying:
   ```powershell
   .\CopyPkcsForHotRestart.ps1
   ```

4. **Check Hot Restart Directories**:
   After a failed build, check if the assembly exists in:
   - `%LOCALAPPDATA%\Temp\Xamarin\HotRestart\Signing\PhotoJobApp.app\`
   - If missing, the copy targets aren't executing at the right time

## Why This Works

The signing tool uses .NET's standard assembly probing, which searches:
1. Application base directory
2. Subdirectories
3. Framework directories
4. Directory of executing assembly

By copying the assembly to multiple locations in the Hot Restart app bundle structure at multiple build stages, we ensure it's found regardless of where the signing tool starts its search or when Hot Restart creates its directories.

## Notes

- This is a workaround, not a permanent fix
- The root cause is likely in Visual Studio's Hot Restart tooling not properly including NuGet dependencies in its assembly resolution context
- The solution is designed to be non-intrusive and only activates for iOS Hot Restart builds
- All targets conditionally run only when `$(TargetFramework)=='net9.0-ios'` and `$(IsHotRestartBuild)=='True'`

