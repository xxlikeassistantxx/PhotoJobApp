# Hot Restart EXE File Not Deployed Issue

## The Problem

When deploying to iOS via **Hot Restart** (from Windows to iPhone), the app crashes on launch with:

```
System.IO.FileNotFoundException: 'Could not load file or assembly 
'/var/mobile/Containers/Data/Application/.../Documents/PhotoJobApp.content/PhotoJobApp.exe' 
or one of its dependencies.'
```

## Root Cause

**Hot Restart** has a known limitation where `.exe` files are **not included** in the deployment package that gets copied to the device. The Hot Restart deployment process filters out `.exe` files and only copies `.dll` files.

Even though:
- The EXE is created in the output directory (`bin\Debug\net9.0-ios\ios-arm64\PhotoJobApp.exe`)
- Multiple MSBuild targets copy it to Hot Restart content directories
- It's added as Content and MauiAsset items

Hot Restart's `_CopyFilesToHotRestartContentDir` target still filters it out during deployment.

## Workarounds

### Option 1: Use Full Deploy from Mac (Recommended)

For iOS development and testing, use a **full deploy from a Mac** instead of Hot Restart:

1. **From Visual Studio for Mac:**
   - Open the project
   - Select your iOS device or simulator
   - Build and Run (F5)

2. **From command line on Mac:**
   ```bash
   dotnet build -f net9.0-ios
   dotnet run -f net9.0-ios
   ```

3. **From Xcode:**
   - Build the project in Xcode
   - Deploy to device or simulator

### Option 2: Test on Android Instead

For development and testing, use **Android** which works fine from Windows:

- Build and deploy to Android device or emulator from Visual Studio on Windows
- No Hot Restart limitations

### Option 3: Wait for Microsoft Fix

This is a known issue in the .NET MAUI / Xamarin Hot Restart system. Microsoft may fix it in a future update. Monitor:
- [.NET MAUI GitHub Issues](https://github.com/dotnet/maui/issues)
- [Visual Studio Release Notes](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes)

## What We've Tried

The project includes multiple MSBuild targets that attempt to ensure the EXE is included:

1. **`EnsureMainAssemblyForHotRestart`** - Creates EXE in output directory after build
2. **`AddExeToHotRestartContent`** - Adds EXE as Content item
3. **`IncludeExeAsContentForHotRestart`** - Copies EXE to content directory
4. **`EnsureMainAssemblyInContentDir`** - Pre-copies EXE before `_CopyFilesToHotRestartContentDir`
5. **`ForceIncludeExeInDeployment`** - Force-copies EXE after `_CopyFilesToHotRestartContentDir`
6. **`EnsureMainAssemblyInHotRestartBundle`** - Copies EXE to bundle locations

None of these work because Hot Restart's deployment process filters out `.exe` files at a lower level.

## Current Status

- ✅ EXE is created in output directory
- ✅ EXE is copied to Hot Restart content directories
- ❌ EXE is **not** deployed to device (filtered out by Hot Restart)

## Recommendation

**For iOS development:** Use a Mac with Visual Studio for Mac or Xcode for full deployment.

**For quick testing:** Use Android from Windows, which doesn't have this limitation.

**For production:** Always use full deployment from a Mac, as Hot Restart is not supported for production builds.
