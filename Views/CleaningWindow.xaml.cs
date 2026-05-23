using Microsoft.Extensions.DependencyInjection;
using OpenClawManager.Views;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddOpenClawManagerServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGatewayService, GatewayService>();
        services.AddSingleton<IResourceMonitor, ResourceMonitorAdapter>();
        services.AddSingleton<IProcessDetector, ProcessDetectorAdapter>();
        services.AddSingleton<ICleanupService, CleanupServiceAdapter>();
        services.AddSingleton<ITokenService, TokenServiceAdapter>();
        
        // Registrace ViewModelů
        services.AddTransient<AboutViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CleaningViewModel>();
        
        // Registrace oken
        services.AddTransient<AboutWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<CleaningWindow>();

        return services;
    }
}