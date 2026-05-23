using Microsoft.Extensions.DependencyInjection;

namespace OpenClawManager.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddOpenClawManagerServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGatewayService, GatewayServiceAdapter>();
        services.AddSingleton<IResourceMonitor, ResourceMonitorAdapter>();
        services.AddSingleton<IProcessDetector, ProcessDetectorAdapter>();
        services.AddSingleton<ICleanupService, CleanupServiceAdapter>();
        services.AddSingleton<ITokenService, TokenServiceAdapter>();

        return services;
    }
}
