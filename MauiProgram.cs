using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using PhotoJobApp.Services;

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

#if IOS
        // Register iOS-specific Google Sign-In service
        builder.Services.AddSingleton<PhotoJobApp.Platforms.iOS.GoogleSignInService>();
        builder.Services.AddSingleton<IGoogleSignInService>(sp => sp.GetRequiredService<PhotoJobApp.Platforms.iOS.GoogleSignInService>());
#endif
        
        builder.Services.AddSingleton<FirebaseAuthService>();
        
        // Register services that need user context
        builder.Services.AddTransient<PhotoJobService>();
        builder.Services.AddTransient<JobTypeService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
} 