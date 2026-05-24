using Microsoft.Extensions.DependencyInjection;
using OpenClawManager.Views;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddOpenClawManagerServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppEnvironment, AppEnvironment>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGatewayService, GatewayService>();
        services.AddSingleton<IResourceMonitor, ResourceMonitor>();
        services.AddSingleton<IProcessDetector, ProcessDetector>();
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<ITokenService, TokenServiceAdapter>();

        services.AddTransient<AboutViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CleaningViewModel>();

        services.AddTransient<AboutWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<CleaningWindow>();

        return services;
    }
}
