# PhotoJobApp Setup Guide

## Prerequisites

### For Windows Development
1. **Visual Studio 2022** (Community Edition is free)
   - Download from: https://visualstudio.microsoft.com/downloads/
   - Install with these workloads:
     - .NET Multi-platform App UI development
     - Mobile development with .NET
     - Universal Windows Platform development

2. **.NET 9.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   - Verify installation: `dotnet --version` (should show 9.0.x)

3. **Windows App SDK**
   - Usually installed with Visual Studio 2022
   - If not, download from: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/

### For iPhone Deployment
1. **Apple Developer Account** (Required for device deployment)
   - Free account: Can test on simulator and deploy to your own devices
   - Paid account ($99/year): Can distribute to App Store and other devices
   - Sign up at: https://developer.apple.com/

2. **Xcode** (Required for iOS development)
   - Download from Mac App Store (requires macOS)
   - **Alternative**: Use Visual Studio's built-in iOS development tools on Windows

3. **iOS Simulator** (for testing)
   - Comes with Visual Studio 2022
   - Can test iOS apps on Windows without a Mac

## Setup Steps

### Step 1: Clone/Download Project
1. Copy the project from your USB to your new computer
2. Open the project folder in Visual Studio 2022
3. Or use command line: `dotnet restore` in the project directory

### Step 2: Install Dependencies
```bash
# Install .NET MAUI workload
dotnet workload install maui

# Restore NuGet packages
dotnet restore
```

### Step 3: Configure Firebase (Optional - for cloud sync)
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or use existing one
3. Enable Realtime Database
4. Update `Services/FirebaseConfig.cs` with your project details:
   ```csharp
   public const string ProjectId = "your-firebase-project-id";
   public const string ApiKey = "your-firebase-api-key";
   ```

### Step 4: Test Windows Build
```bash
# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0

# Run on Windows
dotnet run -f net9.0-windows10.0.19041.0
```

### Step 5: Test iOS Build
```bash
# Build for iOS (simulator)
dotnet build -f net9.0-ios

# Run on iOS simulator
dotnet run -f net9.0-ios
```

## iPhone Deployment

### Option 1: Using Visual Studio 2022 (Recommended)

1. **Connect iPhone to Windows PC**
   - Use USB cable
   - Trust the computer on your iPhone

2. **Configure iOS Development**
   - In Visual Studio, go to Tools > Options > Xamarin > iOS Settings
   - Configure your Apple Developer account
   - Set up iOS build host (if using Mac)

3. **Deploy to iPhone**
   - Select "iPhone" as target device
   - Build and deploy: `dotnet run -f net9.0-ios`

### Option 2: Using Xcode (requires Mac)

1. **Open project in Xcode**
   - Navigate to `Platforms/iOS/` folder
   - Open the `.xcodeproj` file

2. **Configure signing**
   - Select your team in project settings
   - Configure bundle identifier if needed

3. **Deploy to device**
   - Connect iPhone via USB
   - Select your device as target
   - Build and run

### Option 3: Using iOS Simulator (Windows)

1. **Install iOS Simulator**
   - Comes with Visual Studio 2022
   - Or download separately

2. **Run on simulator**
   ```bash
   dotnet run -f net9.0-ios
   ```

## Troubleshooting

### Common Issues

1. **iOS Code Signing Errors**
   - **Error**: "No valid iOS code signing keys found in keychain"
   - **Solution**: The project is now configured with automatic code signing
   - **Files Updated**: 
     - `PhotoJobApp.csproj` - Updated iOS signing configuration
     - `Platforms/iOS/Entitlements.plist` - Created entitlements file
     - `Platforms/iOS/Info.plist` - Added required permissions

2. **Build Errors**
   - Ensure all workloads are installed: `dotnet workload install maui`
   - Clean and rebuild: `dotnet clean && dotnet build`

3. **iOS Deployment Issues**
   - Check Apple Developer account status
   - Verify device is trusted
   - Check bundle identifier matches

4. **Firebase Issues**
   - Verify project ID and API key
   - Check internet connection
   - Review Firebase Console settings

### Performance Optimization

1. **Enable Release Mode**
   ```bash
   dotnet build -c Release -f net9.0-ios
   ```

2. **Optimize for Device**
   - Use device-specific builds
   - Enable AOT compilation for better performance

## Next Steps

1. **Test the app** on Windows and iOS simulator
2. **Configure Firebase** for cloud sync (optional)
3. **Customize the app** for your specific needs
4. **Deploy to physical devices** for testing
5. **Prepare for App Store** (if distributing publicly)

## Support

- **Documentation**: https://docs.microsoft.com/en-us/dotnet/maui/
- **Community**: https://github.com/dotnet/maui/discussions
- **Issues**: https://github.com/dotnet/maui/issues

## Notes

- The app uses SQLite for local storage
- Firebase integration is optional for cloud sync
- Camera and location permissions are configured for iOS
- The app supports both portrait and landscape orientations
- iOS code signing is configured for automatic provisioning
