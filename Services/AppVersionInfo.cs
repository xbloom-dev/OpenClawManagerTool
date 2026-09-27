using System.Diagnostics;
using System.Reflection;

namespace OpenClawManager.Services;

public static class AppVersionInfo
{
    public static string Number => ResolveVersionNumber();
    public static string Display => $"v{Number}";

    private static string ResolveVersionNumber()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informational = entryAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational.Split('+')[0].Trim();

        var location = entryAssembly.Location;
        if (!string.IsNullOrWhiteSpace(location))
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(location).FileVersion;
            if (!string.IsNullOrWhiteSpace(fileVersion))
                return fileVersion;
        }

        var assemblyVersion = entryAssembly.GetName().Version?.ToString(3);
        return string.IsNullOrWhiteSpace(assemblyVersion) ? "0.0.0" : assemblyVersion;
    }
}
