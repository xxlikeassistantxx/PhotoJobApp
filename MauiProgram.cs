using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using PhotoJobApp.Services;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace PhotoJobApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Load appsettings.json configuration
        // Configuration is optional - the app will use GoogleService-Info.plist as primary source
        IConfiguration? configuration = null;
        try
        {
            // Try to load appsettings.json from the app bundle or working directory
            var configurationBuilder = new ConfigurationBuilder();
            
            // Add appsettings.json as optional - if it exists, it will be loaded
            // The app will fall back to GoogleService-Info.plist if configuration is not available
            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            
            configuration = configurationBuilder.Build();
            if (configuration.AsEnumerable().Any())
            {
                builder.Configuration.AddConfiguration(configuration);
                System.Diagnostics.Debug.WriteLine("Successfully loaded appsettings.json configuration");
                Console.WriteLine("Successfully loaded appsettings.json configuration");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("appsettings.json not found or empty - will use GoogleService-Info.plist defaults");
                Console.WriteLine("appsettings.json not found or empty - will use GoogleService-Info.plist defaults");
            }
        }
        catch (Exception ex)
        {
            // Configuration is optional - app will use defaults from GoogleService-Info.plist
            System.Diagnostics.Debug.WriteLine($"Warning: Could not load appsettings.json (using defaults): {ex.Message}");
            Console.WriteLine($"Warning: Could not load appsettings.json (using defaults): {ex.Message}");
        }

#if IOS
        // Register iOS-specific Google Sign-In service
        builder.Services.AddSingleton<PhotoJobApp.Platforms.iOS.GoogleSignInService>();
        builder.Services.AddSingleton<IGoogleSignInService>(sp => sp.GetRequiredService<PhotoJobApp.Platforms.iOS.GoogleSignInService>());
        
        // Register FirebaseAuthService with configuration and GoogleSignInService
        builder.Services.AddSingleton<FirebaseAuthService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var googleSignInService = sp.GetRequiredService<IGoogleSignInService>();
            return new FirebaseAuthService(config, googleSignInService);
        });
#else
        // Register FirebaseAuthService with configuration only (non-iOS)
        builder.Services.AddSingleton<FirebaseAuthService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new FirebaseAuthService(config);
        });
#endif
        // Register IConfiguration for dependency injection
        // Use builder.Configuration if configuration is null (fallback)
        IConfiguration finalConfiguration;
        if (configuration != null)
        {
            builder.Services.AddSingleton<IConfiguration>(configuration);
            finalConfiguration = configuration;
        }
        else
        {
            // Create an empty configuration if appsettings.json couldn't be loaded
            var emptyConfig = new ConfigurationBuilder().Build();
            builder.Services.AddSingleton<IConfiguration>(emptyConfig);
            finalConfiguration = emptyConfig;
        }
        
        // Initialize FirebaseConfig with configuration
        FirebaseConfig.Initialize(finalConfiguration);
        
        // Register services that need user context
        builder.Services.AddTransient<PhotoJobService>();
        builder.Services.AddTransient<JobTypeService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
} 