using System.IO;

namespace OpenClawManager.Services;

public sealed class AppEnvironment : IAppEnvironment
{
    public string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawManager",
        "settings.json");

    public string WebView2DataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenClawManager",
        "WebView2");

    public string AppBaseDirectory { get; } = AppContext.BaseDirectory;
}
